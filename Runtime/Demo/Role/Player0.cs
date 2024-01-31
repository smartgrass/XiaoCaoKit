using Cinemachine.Utility;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.UI.GridLayoutGroup;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    public class Player0 : PlayerBase, IMsgReceiver
    {

        public PlayerData0 playerData = new PlayerData0();

        public PlayerShareData0 component = new PlayerShareData0();

        public override IData data => playerData;
        public override IShareData componentData => component;


        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(PlayerData0 playerData, bool isMainPlayer = false)
        {
            this.playerData = playerData;
            raceId = idRole.raceId;
            //映射
            roleData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().GetSetting(raceId);
            playerData.playerSetting = ConfigMgr.LoadSoConfig<PlayerSettingSo>().GetSetting(raceId);


            this.CreateGameObject();

            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.runtimeAnim;
            component.input = new PlayerInput(this);
            component.control = new PlayerControl(this);
            component.aiControl = new AIControl(this);
            component.atkTimers = new PlayerAtkTimer(this);
            component.movement = new PlayerMovement(this);
            Enable = true;

            if (isMainPlayer)
            {
                AddTag(RoleTagCommon.MainPlayer);
                GameDataCommon.Current.player0 = this;
            }

            RoleIn();
        }

        protected override void OnUpdate()
        {
            component.input.Update();
            component.aiControl.Update();
            component.control.Update();
            component.movement.Update();
            //考虑增加add模式
        }

        protected override void OnFixedUpdate()
        {
            component.aiControl.FixedUpdate();
            component.control.FixedUpdate();
            component.movement.FixedUpdate();
            component.input.FixedUpdate();

            //DataClear
            component.input.Used();
            roleData.roleState.Used();
        }

        protected override void OnDestroy()
        {
            RoleOut();
        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debug.Log($"--- Receive {type} fromId: {fromId}");
            if (type == EntityMsgType.StartSkill)
            {
                int msgInt = (int)msg;
                component.control.TryPlaySkill(msgInt);
            }

        }

    }

    public class PlayerInput : PlayerComponent, IUsed
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
        public void Used()
        {
            data.Reset();
        }
    }

    public class PlayerMovement : RoleMovement
    {
        public PlayerMovement(Player0 owner) : base(owner)
        {
            InputData = owner.playerData.inputData;
            owner.roleData.movement = this;
        }
        public PlayerInputData InputData;

        protected override Vector3 GetInputMoveDir()
        {
            if (RoleState.IsMoveLock)
            {
                return Vector3.zero;
            }
            Vector3 forward = camForward;
            forward.y = 0;
            Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 moveDir = (InputData.y * forward.normalized + InputData.x * hor).normalized;
            return moveDir;
        }
    }

    public class PlayerControl : RoleControl<Player0>
    {
        public PlayerControl(Player0 _owner) : base(_owner) { }
        public PlayerData0 Data_P => owner.playerData;
        public PlayerInputData InputData => owner.playerData.inputData;
        public PlayerAtkTimer AtkTimers => owner.component.atkTimers;

        public override void Update()
        {
            if (!owner.IsDie)
            {
                return;
            }
            owner.CheckBreakUpdate();
            if (InputData.skillInput != 0)
            {
                TryPlaySkill(InputData.skillInput);
            }

            if (InputData.inputs[InputKey.NorAck])
            {
                TryNorAck();
            }
        }

        public void TryNorAck()
        {
            if (!Data_R.IsFree || IsBusy())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());

            int nextNorAckIndex = AtkTimers.GetNextNorAckIndex();

            Data_P.curNorAckIndex = nextNorAckIndex;

            RcpPlaySkill(GetFullNorAckId(nextNorAckIndex));
        }



        //执行使用技能, 一般不直接使用
        public override void RcpPlaySkill(int skillId)
        {
            //TODO
            Debug.Log($" PlaySkill {skillId}");

            bool isOtherSkill = IsOtherSkill(skillId);

            base.RcpPlaySkill(skillId);
        }

        //平a 翻滚等跳跃等"特殊技能"
        public bool IsOtherSkill(int skillId)
        {
            return skillId < 0;
        }

    }
    //相当于System, 无数据

    public class PlayerAtkTimer : PlayerComponent
    {
        public PlayerAtkTimer(Player0 owner) : base(owner) { }
        public PlayerSetting playerSetting => Data_P.playerSetting;

        public Dictionary<int, SkillCdData> dic = new Dictionary<int, SkillCdData>();

        public float resetNorAckTimer;

        public int GetNextNorAckIndex()
        {
            if (Time.time < resetNorAckTimer)
            {
                int len = playerSetting.norAtkCount;// norAtkIds.Count;
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

    public class PlayerComponent : RoleComponent<Player0>
    {
        public PlayerComponent(Player0 _owner) : base(_owner) { }

        public PlayerData0 Data_P => owner.playerData;
    }


    #region Datas & Flag
    //玩家特有数据
    public class PlayerData0 : IData
    {
        public int curNorAckIndex;

        public PlayerInputData inputData = new PlayerInputData(); //方向,ack 1,2 ,skill,空格

        public PlayerSetting playerSetting;
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
        public AIControl aiControl;
        public PlayerMovement movement;
        public PlayerAtkTimer atkTimers;
    }


    public static class InputKey
    {
        public const int NorAck = 0;
        public const int LeftShift = 1;
        public const int Space = 2;
    }
    #endregion

}
