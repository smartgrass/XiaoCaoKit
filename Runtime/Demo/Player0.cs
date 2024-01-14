using Cinemachine.Utility;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    public class Player0 : PlayerBase, IMsgReceiver
    {
        //True:本地玩家控制;  False:网络同步控制, AI控制
        public bool IsControl;

        public PlayerData0 playerData = new PlayerData0();

        public PlayerShareData0 playerComponent = new PlayerShareData0();

        public override IData data => playerData;
        public override IShareData componentData => playerComponent;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(PlayerData0 playerData, bool isMainPlayer = false)
        {
            this.playerData = playerData;
            prefabID = playerData.prefabID;

            playerData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().moveSetting;
            playerData.playerSetting = ConfigMgr.LoadSoConfig<PlayerSettingSo>().playerSetting;


            this.CreateGameObject();

            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.runtimeAnim;
            playerComponent.input = new PlayerInput(this);
            playerComponent.control = new PlayerControl(this);
            playerComponent.movement = new PlayerMovement(this);
            playerComponent.atkTimers = new AtkTimers(this);
            Enable = true;

            if (isMainPlayer)
            {
                AddTag(RoleTagCommon.MainPlayer);
                GameDataCommon.Current.Player0 = this;
            }
        }

        protected override void OnUpdate()
        {
            playerComponent.input.Update();
            playerComponent.control.Update();
            playerComponent.movement.Update();
            //考虑增加add模式
        }

        protected override void OnFixedUpdate()
        {
            playerComponent.control.FixedUpdate();
            playerComponent.movement.FixedUpdate();
            playerComponent.input.FixedUpdate();
            playerComponent.input.Used();
        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debug.Log($"--- Receive {type} fromId: {fromId}");
            if (type == EntityMsgType.StartSkill)
            {
                int msgInt = (int)msg;
                playerComponent.control.TryPlaySkill(msgInt);
            }

        }

    }


    public class PlayerInputData
    {
        public float x;
        public float y;
        public bool[] inputs = new bool[8];
        public int skillInput;

        public KeyCode[] CheckKeyCode = new KeyCode[] { KeyCode.Alpha0, KeyCode.Alpha1 };

        public void Reset()
        {
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = false;
            skillInput = 0;
            x = 0;
            y = 0;
        }

        public void Copy(PlayerInputData data)
        {
            this.x = data.x;
            this.y = data.y;
            this.inputs = data.inputs;
            skillInput = data.skillInput;
        }

    }

    public static class InputKey
    {
        public const int NorAck = 0;
        public const int LeftShift = 1;
        public const int Space = 2;
    }

    public class PlayerInput : EntityComponent<Player0>
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => owner.playerData.inputData;
        public override void Update()
        {
            data.x = Input.GetAxisRaw("Horizontal");
            data.y = Input.GetAxisRaw("Vertical");
            if (Input.GetKeyDown(KeyCode.Mouse0))
                data.inputs[InputKey.NorAck] = true;
            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[InputKey.LeftShift] = true;
            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[InputKey.Space] = true;
            for (int i = 0; i < data.CheckKeyCode.Length; i++)
            {
                if (Input.GetKeyDown(data.CheckKeyCode[i]))
                {
                    data.skillInput = i; //TODO还需配置对应id,使用SO做默认配置吧
                }
            }
            owner.playerData.inputData.Copy(data);
        }
        public override void FixedUpdate()
        {

        }
        //使用过
        internal void Used()
        {
            data.Reset();
        }
    }

    public class PlayerMovement : EntityComponent<Player0>
    {
        public PlayerMovement(Player0 owner) : base(owner) { }

        private float velocity;

        public PlayerData0 Data => owner.playerData;
        public PlayerInputData InputData => owner.playerData.inputData;
        public RoleState RoleState => Data.roleState;
        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.tf;

        public MoveSetting setting => Data.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        public override void FixedUpdate()
        {
            if (Data.bodyState == EBodyState.Dead)
            {
                return;
            }
            MoveUpdate();
        }

        void MoveUpdate()
        {
            Vector3 moveDir = moveDir = Vector3.zero;
            bool isInput = false;
            if (!RoleState.isMoveLock && RoleState.moveLockTime <= 0)
            {
                Vector3 forward = camForward;
                forward.y = 0;
                Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
                moveDir = (InputData.y * forward.normalized + InputData.x * hor).normalized;
                isInput = (Mathf.Abs(InputData.x) + Mathf.Abs(InputData.y)) > 0;
            }

            FixUpdateMoveAnimMult(isInput);
            RotateByMoveDir(moveDir);
            //移动时用delta值的实时计算的, 如果做同步最好用绝对坐标
            Vector3 moveDelta = moveDir * Data.moveSetting.baseMoveSpeed * Data.roleState.MoveMultFinal * XCTime.fixedDeltaTime;
            cc.Move(moveDelta);
            owner.Anim.SetFloat(AnimNames.MoveSpeed, RoleState.animMoveSpeed);

            FixUpdateLockTime();
        }

        void RotateByMoveDir(Vector3 worldMoveDir)
        {
            if (worldMoveDir.IsNaN())
                return;

            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = tf.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, 0.5f);
            tf.rotation = rotation;
        }


        private void FixUpdateLockTime()
        {
            if (RoleState.moveLockTime > 0)
            {
                RoleState.moveLockTime -= XCTime.fixedDeltaTime;
            }
            if (RoleState.rotateLockTime > 0)
            {
                RoleState.rotateLockTime -= XCTime.fixedDeltaTime;
            }
        }

        //计算移动动画速度, 同时会影响移速倍率
        void FixUpdateMoveAnimMult(bool isInput)
        {
            float inputTotal = isInput ? 1 : 0;

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref velocity, setting.moveSmooth);

            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 1, 1.5f);
        }

    }

    public class PlayerControl : EntityComponent<Player0>
    {
        public PlayerControl(Player0 owner) : base(owner) { }

        public PlayerInputData InputData => owner.playerData.inputData;

        public PlayerData0 Data => owner.playerData;

        public RoleState RoleState => Data.roleState;

        public CharacterController cc => owner.idRole.cc;

        public AtkTimers atkTimers => owner.playerComponent.atkTimers;

        public List<XCTaskRunner> curTaskData = new List<XCTaskRunner>();

        public override void Update()
        {
            if (Data.bodyState == EBodyState.Dead)
            {

                return;
            }
            CheckBreak();
            if (InputData.skillInput != 0)
            {
                TryPlaySkill(InputData.skillInput);
            }

            if (InputData.inputs[InputKey.NorAck])
            {
                TryNorAck();
            }


        }
        public override void FixedUpdate()
        {
            UpdateGravity();

            if (Data.bodyState == EBodyState.Dead)
            {
                return;
            }
        }

        void CheckBreak()
        {
            if (RoleState.breakTime > 0)
            {
                RoleState.breakTime -= XCTime.deltaTime;
                Data.bodyState = EBodyState.Break;
            }
            else if (RoleState.breakTime <= 0 && Data.bodyState == EBodyState.Break)
            {
                Data.bodyState = EBodyState.Ready;
            }
        }

        //Move可以考虑拆分出 MoveComponent

        void UpdateGravity()
        {
            //暂时利用刚体吧
            //owner.playerShareData.rigidbody.useGravity
        }
        public bool IsBusy(int level = 0)
        {
            foreach (var item in curTaskData)
            {
                //&& item.Task.Info.skillId > 0 TODO
                if (item.Task.IsBusy )
                {
                    return true;
                }
            }
            return false;
        }

        public void TryPlaySkill(int skillId)
        {
            //条件判断, 耗蓝等等
            if (!Data.IsFree)
                return;
            //排除高优先级技能, 高优先级技能可以在别的技能使用过程中使用
            if (IsBusy() && !IsHighLevelSkill(skillId))
                return;

            RcpPlaySkill(skillId);
        }

        public void TryNorAck()
        {
            if (!Data.IsFree || IsBusy())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());
  
            int nextNorAckIndex = atkTimers.GetNextNorAckIndex();
            int norAckSkillId = Data.playerSetting.NorAtkIds[nextNorAckIndex];
            Data.curNorAckIndex = nextNorAckIndex;
            RcpPlaySkill(norAckSkillId);
        }

        private bool IsHighLevelSkill(int skillId)
        {
            //读表 读配置
            return false;
        }

        //执行使用技能, 一般不直接使用
        public void RcpPlaySkill(int skillId)
        {
            //TODO
            Debug.Log($" PlaySkill {skillId}");


            bool isOtherSkill = IsOtherSkill(skillId);

            Data.curSkillId = skillId;

            Transform playerTF = owner.gameObject.transform;

            TaskInfo taskInfo = new TaskInfo()
            {
                skillId = skillId,
                curGO = owner.gameObject,
                curTF = playerTF,
                playerTF = playerTF,
                castEuler = playerTF.eulerAngles,
                castPos = playerTF.position,
                playerAnimator = owner.Anim,
            };
            var task = XCTaskRunner.CreatNew(skillId, RoleType.Player, taskInfo);
            curTaskData.Add(task);
            task.onEndEvent.AddListener(OnTaskEnd);
        }

        void OnTaskEnd(XCTaskRunner runner)
        {
            curTaskData.Remove(runner);
        }

        //平a 翻滚等跳跃等设定
        public bool IsOtherSkill(int skillId)
        {
            return skillId < 0;
        }

    }
    //相当于System, 无数据

    public class AtkTimers : EntityComponent<Player0>
    {
        public AtkTimers(Player0 owner) : base(owner) { }
        public PlayerData0 data => owner.playerData;
        public PlayerSetting playerSetting => data.playerSetting;

        public Dictionary<int, SkillCdData> dic = new Dictionary<int, SkillCdData>();

        public float resetNorAckTimer;

        public int GetNextNorAckIndex()
        {
            if (Time.time < resetNorAckTimer)
            {
                int len = playerSetting.NorAtkIds.Count;
                return (data.curNorAckIndex + 1) % len;
            }
            return 0;
        }

        public void SetNorAckTime()
        {
            resetNorAckTimer = Time.time + playerSetting.resetNorAckTime;
        }


        public bool IsSkillReady(int skillId)
        {
            if (dic.ContainsKey(skillId))
            {
                return !dic[skillId].IsCd;
            }
            return true;
        }

        public void SetSkillEnterCD(int skillId)
        {
            if (dic.ContainsKey(skillId))
            {
                dic[skillId].EnterCD();
            }
        }


        public class SkillCdData
        {
            public int skillId;

            public float cdFinishTime;

            public float cd;
            public bool IsCd => Time.time < cdFinishTime;
            public void EnterCD()
            {
                cdFinishTime = Time.time + cd;
            }

            public float GetCurProccess()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time) / cd;
                }
                return 1;
            }
        }
    }

    public class EntityComponent<T> : IUpdate where T : Role
    {
        public T owner;

        public EntityComponent(T owner)
        {
            this.owner = owner;
        }

        public virtual void Update() { }

        public virtual void FixedUpdate() { }

    }

    public interface IUpdate
    {
        public void Update();

        public void FixedUpdate();
    }

    //建议只在这层 封装数据类
    public class PlayerData0 : IData
    {
        public int prefabID = 0;

        public EBodyState bodyState;

        public int curSkillId;
        public int curNorAckIndex;

        //优先级规则
        //0. 非死亡&非控制中 属于自由状态
        //1. 自由状态可以执行任何主动行为
        //2. 如果在自由期间 , 被击中,可能转至被不自由状态
        //3. 处于不自由时,可以使用特殊技能
        //4. 施法优先级高于普通, 普攻无法打断施法, 但施法可打断普攻 
        //5. 处于task过程中 无法普攻
        //6. 翻滚优先级高, 可以打断普攻 和 技能
        public bool IsFree => bodyState is not EBodyState.Break or EBodyState.Dead;


        public float moveSpeedFactor = 1;

        public RoleState roleState = new RoleState();

        public PlayerAttr playerAttr = new PlayerAttr();

        public PlayerInputData inputData = new PlayerInputData(); //方向,ack 1,2 ,skill,空格

        public MoveSetting moveSetting;

        public PlayerSetting playerSetting;

        public void TryChangeState()
        {

        }
    }

    public class RoleState
    {
        public bool isNorAck;
        public bool isMoveLock; //强制锁定移动, 如角色死亡
        public float moveLockTime; //普通锁定移动, 如技能状态
        public float rotateLockTime; //普通锁定旋转, 如技能状态
        public float breakTime;

        public float moveSpeedMult = 1;

        public float moveAnimMult = 1;

        public float animMoveSpeed = 0;

        public float MoveMultFinal => moveSpeedMult * moveAnimMult;

    }
    [Serializable]
    public class PlayerSetting
    {
        public int rollSkillId = -100;
        public List<int> NorAtkIds = new List<int>() { -1, -2, -3 };
        public float resetNorAckTime = 1.5f;
    }

    //基础数值
    [Serializable]
    public class MoveSetting
    {
        public float baseMoveSpeed = 4;

        public float moveSmooth = 0.05f;

        public float startRotateSpeed = 200;
        public float endRotateSpeed = 100;
    }

    public class PlayerAttr
    {
        public int lv;
        public int exp;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int maxExp;
        public int atk;
        public int def;



        public void SetByLevel(int lv)
        {
            maxHp = hp = 100 + 10 * lv;
            maxMp = mp = 100 + 10 * lv;
            maxExp = 100 + 100 * (lv % 10);
            atk = lv * 5;
            def = lv * 1;
        }
    }

    public class PlayerShareData0 : IShareData
    {
        public List<IUpdate> components = new List<IUpdate>();

        public PlayerInput input;
        public PlayerControl control;
        public PlayerMovement movement;
        public AtkTimers atkTimers;
    }


    public enum EBodyState
    {
        Ready,
        Break, //被打断的中,眩晕中
        Dead  //最高优先级
    }

    public enum EAckState
    {
        None,
        NorAck,
        Skill
    }
}
