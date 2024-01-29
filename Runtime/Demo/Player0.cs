using Cinemachine.Utility;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using TEngine;
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

            playerData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().moveSetting;
            playerData.playerSetting = ConfigMgr.LoadSoConfig<PlayerSettingSo>().GetPlayerSetting(raceId);


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

    public class PlayerInput : PlayerComponent
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => Data_P.inputData;
        public override void Update()
        {
            data.x = Input.GetAxis("Horizontal");
            data.y = Input.GetAxis("Vertical");
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

    public class PlayerMovement : PlayerComponent
    {
        public PlayerMovement(Player0 owner) : base(owner) { }

        private float tempVelocity;

        public PlayerInputData InputData => owner.playerData.inputData;

        public RoleState RoleState => Data_R.roleState;

        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.tf;

        public MoveSetting setting => Data_P.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        public override void FixedUpdate()
        {
            if (owner.roleData.bodyState == EBodyState.Dead)
            {
                return;
            }
            MoveUpdate();
        }

        void MoveUpdate()
        {
            //模拟修改思路, 根据moveDelta控制动画机->表现层

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
            Vector3 moveDelta = moveDir * Data_P.moveSetting.baseMoveSpeed * Data_R.roleState.MoveMultFinal * XCTime.fixedDeltaTime;

            //TODO 重力
            //v = v + gt
            float v = 0;
            v = v + Data_P.moveSetting.g * XCTime.fixedDeltaTime;
            moveDelta.y += v * XCTime.fixedDeltaTime;


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

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref tempVelocity, setting.moveSmooth);

            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 1, 1.5f);
        }

    }

    public class PlayerControl : PlayerComponent
    {
        public PlayerControl(Player0 owner) : base(owner) { }

        public PlayerInputData InputData => owner.playerData.inputData;

        public PlayerData0 Data => owner.playerData;
        public RoleData RoleData => owner.roleData;

        public RoleState RoleState => RoleData.roleState;

        public CharacterController cc => owner.idRole.cc;

        public AtkTimers atkTimers => owner.playerComponent.atkTimers;

        public List<XCTaskRunner> curTaskData = new List<XCTaskRunner>();

        public override void Update()
        {
            if (RoleData.bodyState == EBodyState.Dead)
            {

                return;
            }
            CheckBreakTime();
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

            if (RoleData.bodyState == EBodyState.Dead)
            {
                return;
            }
        }

        void CheckBreakTime()
        {
            if (RoleState.breakTime > 0)
            {
                RoleState.breakTime -= XCTime.deltaTime;
                RoleData.bodyState = EBodyState.Break;
            }
            else if (RoleState.breakTime <= 0 && RoleData.bodyState == EBodyState.Break)
            {
                RoleData.bodyState = EBodyState.Ready;
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
                if (item.Task.IsBusy)
                {
                    return true;
                }
            }
            return false;
        }

        public void TryPlaySkill(int skillId)
        {
            //条件判断, 耗蓝等等
            if (!RoleData.IsFree)
                return;
            //排除高优先级技能, 高优先级技能可以在别的技能使用过程中使用
            if (IsBusy() && !IsHighLevelSkill(skillId))
                return;

            RcpPlaySkill(skillId);
        }

        public void TryNorAck()
        {
            if (!RoleData.IsFree || IsBusy())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());

            int nextNorAckIndex = atkTimers.GetNextNorAckIndex();
            int norAckSkillId = Data.playerSetting.norAtkIds[nextNorAckIndex];
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

            Data_R.curSkillId = skillId;

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

    public class AtkTimers : PlayerComponent
    {
        public AtkTimers(Player0 owner) : base(owner) { }
        public PlayerSetting playerSetting => Data_P.playerSetting;

        public Dictionary<int, SkillCdData> dic = new Dictionary<int, SkillCdData>();

        public float resetNorAckTimer;

        public int GetNextNorAckIndex()
        {
            if (Time.time < resetNorAckTimer)
            {
                int len = playerSetting.norAtkIds.Count;
                return (Data_P.curNorAckIndex + 1) % len;
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

    public  class PlayerComponent : EntityComponent<Player0>
    {
        public PlayerComponent(Player0 owner) : base(owner){}

        public PlayerData0 Data_P => owner.playerData;
    }

    public class EntityComponent<T> : IUpdate where T : Role
    {
        public T owner;

        public RoleData Data_R => owner.roleData;

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
    //关于敌人数据,有很多字段与PlayerData0相似
    //如 EBodyState,breakState,roleState; playerAttr
    //建议少数提取,多数嗦哈
    public class PlayerData0 : IData
    {
        public int curNorAckIndex;

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

    public class BreakState
    {
        public float armor;  //虽说有小数, 实际用整数
        public float maxArmor;
        public float recoverWait_t;  //进入破防后,多久启动恢复
        public float recoverFinish_t = 0.5f; //恢复满需要时间
        public float recoverSpeedInner = 0; //被动恢复,没陷入破防时的恢复速度,boss用
        public float maxBreakTime = 0;  //最大连续受击时间,默认0为无
        public float recoverSpeed => maxArmor / recoverFinish_t; //每秒回复多少

        public BreakSubState state { get; set; }
        public bool isHover { get; set; }//是否滞空
        public bool isBreak => state != BreakSubState.None;  //是否破防

        public float enterBreakTime;
        public float breakTimer;

        public void OnHit(int hitArmor)
        {
            armor -= hitArmor;
            if (state == BreakSubState.None)
            {
                if (armor <= 0)
                {
                    state = BreakSubState.BreakStart;
                }
            }
        }

        public void OnUpdate(float deltaTime)
        {
            if (recoverSpeedInner > 0)
            {
                armor += deltaTime * recoverSpeedInner;
            }

            if (state == BreakSubState.BreakRecover)
            {
                armor += deltaTime * recoverSpeed;
            }
            else if (state == BreakSubState.BreakStart)
            {
                breakTimer += deltaTime;
                if (breakTimer > recoverWait_t)
                {
                    state = BreakSubState.BreakRecover;
                }
            }

            if (armor >= maxArmor)
            {
                armor = maxArmor;
                state = BreakSubState.None;
                Debug.Log($"--- recover finish");
            }
        }

        public enum BreakSubState
        {
            None,
            BreakStart,
            BreakRecover
        }
    }

    [Serializable]
    public class PlayerSetting
    {
        [Label("描述")]
        public string des = "";
        public int roleId = 0;
        public int rollSkillId = -1;
        public List<int> norAtkIds = new List<int>() { -101, -102, -103 };
        public List<int> skillList = new List<int>() { 101, 102, 103 };
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
        public float g = 9;
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
