using System;
using System.Collections.Generic;
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

        public void Init(PlayerData0 playerData)
        {
            this.playerData = playerData;
            prefabID = playerData.prefabID;

            playerData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().moveSetting;


            this.CreateGameObject();

            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.runtimeAnim;
            playerComponent.input = new PlayerInput(this);
            playerComponent.control = new PlayerControl(this);
            playerComponent.movement = new PlayerMovement(this);

            AddTag(RoleTagCommon.MainPlayer);
            Enable = true;
            Debug.Log($"--- Enable {Enable} {IsRuning} {hasAwake}");
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

    public class PlayerInput : EntityComponent<Player0>
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => owner.playerData.inputData;
        public override void Update()
        {
            data.x = Input.GetAxis("Horizontal");
            data.y = Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.Space))
                data.inputs[0] = true;
            if (Input.GetKey(KeyCode.LeftShift))
                data.inputs[1] = true;
            if (Input.GetKey(KeyCode.Mouse0))
                data.inputs[2] = true;

            for (int i = 0; i < data.CheckKeyCode.Length; i++)
            {
                if (Input.GetKey(data.CheckKeyCode[i]))
                {
                    data.skillInput = i; //TODO还需配置对应id,使用SO做默认配置吧
                }
            }
            owner.playerData.inputData.Copy(data);
        }
        public override void FixedUpdate()
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

        public MoveSetting setting => Data.moveSetting;

        public Vector3 camForward => CameraController.Forword;

        public override void FixedUpdate()
        {
            if (Data.actState == EActState.Dead)
            {
                return;
            }
            MoveUpdate();
        }

        void MoveUpdate()
        {
            if (!RoleState.isMoveLock && RoleState.moveLockTime <= 0)
            {
                Vector3 forward = camForward;
                forward.y = 0;
                Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 moveDir = (InputData.y * forward.normalized + InputData.x * hor).normalized;
                //速度这边调整
                float moveMult = Data.roleState.moveSpeedMult * Data.roleState.moveAnimMult;

                UpdateMoveAnim(true);

                cc.Move(moveDir * Data.moveSetting.moveSpeed * XCTime.fixedDeltaTime * moveMult);
            }
            else
            {
                UpdateMoveAnim(false);
            }


            if (RoleState.moveLockTime > 0)
            {
                RoleState.moveLockTime -= XCTime.fixedDeltaTime;
            }
        }

        void UpdateMoveAnim(bool isAdd)
        {
            float target = Mathf.Abs(InputData.x) + Mathf.Abs(InputData.y);

            if (!isAdd)
            {
                target = 0;
            }
            target = Mathf.Clamp(target, 0f, 1f);

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, target, ref velocity, setting.moveSmooth);

            owner.Anim.SetFloat(AnimNames.MoveSpeed, RoleState.animMoveSpeed);

            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 0, 1.5f);
        }

    }

    public class PlayerControl : EntityComponent<Player0>
    {
        public PlayerControl(Player0 owner) : base(owner) { }

        public PlayerInputData InputData => owner.playerData.inputData;

        public PlayerData0 Data => owner.playerData;

        public RoleState RoleState => Data.roleState;

        public CharacterController cc => owner.idRole.cc;


        public override void Update()
        {
            if (Data.actState == EActState.Dead)
            {

                return;
            }
            CheckBreak();
            if (Data.IsCanSkill && InputData.skillInput != 0)
            {
                TryPlaySkill(InputData.skillInput);
            }
        }
        public override void FixedUpdate()
        {
            UpdateGravity();

            if (Data.actState == EActState.Dead)
            {
                return;
            }
        }

        void CheckBreak()
        {
            if (RoleState.breakTime > 0)
            {
                RoleState.breakTime -= XCTime.deltaTime;
                Data.actState = EActState.Break;
            }
            else if (RoleState.breakTime <= 0 && Data.actState == EActState.Break)
            {
                Data.actState = EActState.Idle;
            }
        }

        //Move可以考虑拆分出 MoveComponent

        void UpdateGravity()
        {
            //暂时利用刚体吧
            //owner.playerShareData.rigidbody.useGravity
        }

        public void TryPlaySkill(int skillId)
        {
            //条件判断, 耗蓝等等
            if (!Data.IsCanSkill)
                return;
            //排除高优先级技能, 高优先级技能可以在别的技能使用过程中使用
            if (Data.IsAcking && !IsHighLevelSkill(skillId))
                return;

            RcpPlaySkill(skillId);
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
            Debug.Log($"yns PlaySkill {skillId}");
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
            };
            XCTaskRunner.CreatNew(skillId, RoleType.Player, taskInfo);

        }
    }
    //相当于System, 无数据
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

        public EActState actState;

        public int curSkillId;

        public bool IsCanSkill => actState is not EActState.Break or EActState.Dead;

        public bool IsAcking => actState is EActState.Acking;

        public MoveSetting moveSetting;

        public float moveSpeedFactor = 1;

        public RoleState roleState = new();

        public PlayerAttr playerAttr = new();

        public PlayerInputData inputData = new(); //方向,ack 1,2 ,skill,空格
    }

    public class RoleState
    {
        public bool isNorAck;
        public bool isMoveLock; //强制锁定移动
        public float moveLockTime; //普通锁定移动
        public float breakTime;

        public float moveSpeedMult = 1;

        public float moveAnimMult = 1;

        public float animMoveSpeed = 0;

    }

    //基础数值
    [Serializable]
    public class MoveSetting
    {
        public float moveSpeed = 4;
        public float angleSpeed = 40;


        public float moveSmooth = 0.05f;
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
        public List<IUpdate> components = new();

        public PlayerInput input;
        public PlayerControl control;
        public PlayerMovement movement;
    }


    public enum EActState
    {
        Idle,
        Acking, //处于攻击状态, 并且没被打断
        //Jump,
        //Roll,
        Break, //被打断的中,眩晕中
        Dead  //最高优先级
    }

}
