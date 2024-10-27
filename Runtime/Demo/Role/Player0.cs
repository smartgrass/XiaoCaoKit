using DG.Tweening;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    public class Player0 : PlayerBase, IMsgReceiver
    {
        public PlayerAttr PlayerAttr => roleData.playerAttr;

        public override void DataCreat()
        {
            playerData = new PlayerData0();
            roleData = playerData;
        }


        public PlayerData0 playerData;

        public PlayerShareData0 component => playerData.component;


        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(PlayerSaveData savaData, bool isMainPlayer = false)
        {
            CreateIdRole(savaData.prefabId);
            CreateRoleBody(idRole.bodyName);
            SetTeam(1);

            playerData.playerSetting = ConfigMgr.playerSettingSo.GetOrDefault(raceId, 0);
            roleData.playerAttr.Init(savaData.lv, ConfigMgr.commonSettingSo.playerSetting);

            component.input = new PlayerInput(this);
            component.control = new PlayerControl(this);
            roleData.roleControl = component.control;
            //component.aiControl = new AIControl(this);
            component.atkTimers = new PlayerAtkTimer(this);
            component.movement = new RoleMovement(this);
            roleData.movement = component.movement;

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
            component.control.Update();
            component.control.OnTaskUpdate();
            component.movement.Update();

            //考虑增加add模式
            ForDebug();
        }

        void ForDebug()
        {
            DebugGUI.Log("TimeScale", Time.timeScale.ToString("#.##"));
            DebugGUI.Log("Anim", Anim.speed);
            DebugGUI.Log("breakArmor", roleData.breakData.armor);
        }


        protected override void OnFixedUpdate()
        {
            component.control.FixedUpdate();
            component.movement.FixedUpdate();
            component.input.FixedUpdate();

            //DataClear
            component.input.Used();
            roleData.roleState.Used();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RoleOut();
        }


        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            base.ReceiveMsg(type, fromId, msg);

            if (type is EntityMsgType.PlayNextNorAck)
            {
                component.control.TryNorAck();
            }
        }

        public override void OnBreak()
        {
            component.control.OnBreak();
        }

    }

    public class PlayerInput : PlayerComponent, IUsed
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => Data_P.inputData;
        public RoleMovement movement => Data_P.movement;

        public override void Update()
        {
            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }

            data.x = Input.GetAxis("Horizontal");
            data.y = Input.GetAxis("Vertical");
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.J))
            {
                data.inputs[InputKey.NorAck] = true;
                ////屏幕安全边距
                //if (GetMouseProportionalCoordinates().y < 0.9f)
                //{

                //}

            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[InputKey.Space] = true;            
            
            if (Input.GetKeyDown(KeyCode.Tab))
                data.inputs[InputKey.Tab] = true;

            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    data.skillInput = i;
                }
            }

            for (int i = 0; i < data.CheckKeyCode2.Length; i++)
            {
                if (Input.GetKeyDown(data.CheckKeyCode2[i]))
                {
                    data.skillInput = i;
                }
            }

            owner.playerData.inputData.Copy(data);
        }
        public override void FixedUpdate()
        {
            movement.SetMoveDir(GetInputMoveDir());
        }

        public Vector2 GetMouseProportionalCoordinates()
        {
            Vector3 mousePosition = Input.mousePosition;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 将屏幕坐标转换为比例坐标  
            float proportionalX = mousePosition.x / screenWidth;
            float proportionalY = mousePosition.y / screenHeight;
            // 返回比例坐标  
            return new Vector2(proportionalX, proportionalY);

        }

        private Vector3 GetInputMoveDir()
        {
            Vector3 forward = movement.camForward;
            forward.y = 0;
            Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 moveDir = (data.y * forward.normalized + data.x * hor).normalized;
            return moveDir;
        }

        //使用过
        public void Used()
        {
            data.Reset();
        }
    }

    public class PlayerControl : RoleControl<Player0>
    {
        public PlayerControl(Player0 _owner) : base(_owner) { }
        public PlayerData0 Data_P => owner.playerData;
        public PlayerInputData InputData => owner.playerData.inputData;
        public PlayerAtkTimer AtkTimers => owner.component.atkTimers;
        public Tween JumpTween { get; set; }

        public override void Update()
        {
            if (owner.IsDie)
            {
                OnDeadUpdate();
                return;
            }
            owner.CheckBreakUpdate();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (InputData.inputs[InputKey.LeftShift])
            {
                TryRoll();
                return;
            }

            if (InputData.skillInput != 0)
            {
                TryPlaySkill(InputData.skillInput.ToString());
            }

            if (InputData.inputs[InputKey.NorAck])
            {
                TryNorAck();
            }

            if (InputData.inputs[InputKey.Tab])
            {
                if (BattleData.IsTimeStop)
                {
                    TimeStopMgr.Inst.RecoverTimeSpeed();
                }
                else
                {
                    TimeStopMgr.Inst.StopTimeSpeed(6);
                }
            }

            if (InputData.inputs[InputKey.Space])
            {
                TryJump();

            }
        }

        public void TryJump()
        {
            if (!Data_R.IsStateFree || IsBusy())
                return;
            if (JumpTween != null && JumpTween.IsPlaying())
            {
                return;
            }

            owner.Anim.Play(AnimNames.Jump);
            JumpTween = cc.DOHit(Data_P.playerSetting.JumpY, Vector3.zero, 0.5f);
            Data_R.movement.SetNoGravityT(Data_P.playerSetting.JumpNoGravityT);

            //if (FindEnemy(out Role findRole))
            //{
            //    Debug.Log($"--- Find Role {findRole.transform.name}");
            //    owner.idRole.StartCoroutine(IEJumpTo(findRole.transform.position));
            //} 
        }


        private System.Collections.IEnumerator IEJumpTo(Vector3 pos)
        {
            //1 跳起, & LookAt
            CameraMgr.Inst.tempLookAt.position = pos;
            CameraMgr.Inst.aimer.SetAim(CameraMgr.Inst.tempLookAt);
            CameraMgr.Inst.aimer.SetMainWeight(0.7f);


           //匀速运动 到终点

           //落地

           yield return null;
        }

        protected override void PreSkillStart(string skillId)
        {
            if (InputData.x != 0 || InputData.y != 0)
            {
                var dir = owner.component.movement.inputDir;
                this.Data_P.movement.RotateByMoveDir(dir, 1);
            }

            if (AnimNames.Roll != skillId)
            {
                DefaultAutoDirect();
            }
        }

        public void TryRoll()
        {
            //判断冷缩
            if (!AtkTimers.IsSkillReady(AnimNames.Roll))
            {
                Debug.Log($"rollId cd {AtkTimers.GetWaitTime(AnimNames.Roll)}");
                return;
            }

            AtkTimers.SetSkillEnterCD(AnimNames.Roll);

            //停止&打断当前动作
            if (IsBusy())
            {
                SetNoBusy();
            }

            RcpPlaySkill(AnimNames.Roll);
        }

        public void TryNorAck()
        {
            if (!Data_R.IsStateFree || IsBusy())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());

            int nextNorAckIndex = AtkTimers.GetNextNorAckIndex();

            Data_P.curNorAckIndex = nextNorAckIndex;

            RcpPlaySkill($"{AnimNames.NorAck}{nextNorAckIndex}");
        }

        public override void DefaultAutoDirect()
        {
            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, 3.5f, 30, out float maxScore, owner.team);
            if (findRole != null)
            {
                owner.transform.RotaToPos(findRole.transform.position, 0.4f);
                Debug.Log($"--- findRole RotaToPos {findRole.gameObject}");
            }
            else
            {
                Debug.Log($"--- findRole no");
            }

        }


        //执行使用技能, 一般不直接使用
        public override void RcpPlaySkill(string skillId)
        {
            base.RcpPlaySkill(skillId);
        }


    }
    //相当于System, 无数据

    public class PlayerAtkTimer : PlayerComponent
    {
        public PlayerAtkTimer(Player0 owner) : base(owner) { }
        public PlayerSetting playerSetting => Data_P.playerSetting;

        public Dictionary<string, SkillCdData> dic = new Dictionary<string, SkillCdData>();

        public float norAckTimer;

        public int GetNextNorAckIndex(bool setTime = true)
        {
            int ret = 0;
            if (Time.time < norAckTimer)
            {
                int len = playerSetting.norAtkCount;// norAtkIds.Count;
                ret = (Data_P.curNorAckIndex + 1) % len;
            }
            if (setTime)
            {
                SetNorAckTime();
            }
            return ret;
        }

        public void SetNorAckTime()
        {
            norAckTimer = Time.time + playerSetting.resetNorAckTime;
        }


        private void CheckDic(string skillIndex)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.cd = playerSetting.GetSkillSetting(skillIndex).cd;
                dic[skillIndex] = skillCdData;
            }
        }

        //public bool IsSkillReady(string skillId)
        //{
        //    int hash = skillId.GetHashCode();
        //}

        public bool IsSkillReady(string skillIndex)
        {
            CheckDic(skillIndex);
            return !dic[skillIndex].IsCd;
        }

        public void SetSkillEnterCD(string skillIndex)
        {
            CheckDic(skillIndex);
            dic[skillIndex].EnterCD();
        }

        public float GetProcess(string skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetCurProccess();
        }

        public float GetWaitTime(string skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetWaitTime();
        }


        public class SkillCdData
        {
            public float cd;

            public float cdFinishTime { get; set; }

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

            public float GetWaitTime()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time);
                }
                return 0;
            }
        }
    }

    public class PlayerComponent : RoleComponent<Player0>
    {
        public PlayerComponent(Player0 _owner) : base(_owner) { }

        public PlayerData0 Data_P => owner.playerData;
    }


    #region Datas & Flag

    public class PlayerSaveData
    {
        public static PlayerSaveData Current => GameData.playerSaveData;

        public int lv;

        public int raceId = 0;

        public string prefabId;

        //技能解锁状态
        public Dictionary<int, int> skillUnlockDic = new Dictionary<int, int>();

        public SkillBarData skillBarData = new SkillBarData();

        //ItemUI
        public Inventory inventory = new Inventory();
        //持有物
        public List<Item> holdItems = new List<Item>();

        //反序列化读取的数据, 可能会出现空的现象
        internal void CheckNull()
        {
            if (skillBarData == null || skillBarData.onSkill == null)
            {
                skillBarData = new SkillBarData();
            }
            if (inventory == null)
            {
                inventory = new Inventory();
            }
            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<int, int>();
            }
            if (string.IsNullOrEmpty( prefabId) )
            {
                prefabId = "P_0";
            }
        }

        public void AddSkillLevel(int skillId)
        {
            if (!skillUnlockDic.ContainsKey(skillId))
            {
                skillUnlockDic[skillId] = 1;
            }
            else
            {
                skillUnlockDic[skillId] = skillUnlockDic[skillId] + 1;
            }
        }

        public static void Sava()
        {
            SaveMgr.SaveData(PlayerSaveData.Current);
        }
    }

    //玩家特有数据
    public class PlayerData0 : RoleData
    {
        public PlayerShareData0 component = new PlayerShareData0();

        public int curNorAckIndex;

        public PlayerInputData inputData = new PlayerInputData(); //方向,ack 1,2 ,skill,空格

        public PlayerSetting playerSetting;
    }


    public class SkillBarData
    {
        public string[] onSkill = new string[GameSetting.SkillCountOnBar];
    }

    public class PlayerInputData
    {
        public float x;
        public float y;
        //InputKey
        public bool[] inputs = new bool[8];
        public int skillInput;

        public KeyCode[] CheckKeyCode = new KeyCode[] {
            KeyCode.Alpha0, KeyCode.Alpha1
        };


        public KeyCode[] CheckKeyCode2 = new KeyCode[] {
            KeyCode.K, KeyCode.L , KeyCode.U,KeyCode.I,KeyCode.O
        };

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
        public int maxExp;
        public int hp; //实时数值
        public int mp;

        public int MapHp => (int)maxHpAtr.CurrentValue;
        public int MapMp => (int)maxMpAtr.CurrentValue;
        public int Atk => (int)atkAtr.CurrentValue;
        public int Def => (int)defAtr.CurrentValue;
        public float Crit => critAtr.CurrentValue;


        public AttributeValue maxHpAtr = new AttributeValue();
        public AttributeValue maxMpAtr = new AttributeValue();
        public AttributeValue atkAtr = new AttributeValue();
        public AttributeValue defAtr = new AttributeValue();
        public AttributeValue critAtr = new AttributeValue(); //Float


        public void Init(int lv, AttrSetting setting)
        {
            if (lv <1)
            {
                lv = 1;
            }
            this.lv = lv;
            maxExp = 100 + 100 * (lv % 10);

            maxHpAtr.BaseValue = hp =(int) (setting.endHp * lv / setting.maxLevel);
            maxMpAtr.BaseValue = mp = (int)(setting.endMp * lv / setting.maxLevel);
            atkAtr.BaseValue = (int)(setting.endAtk * lv / setting.maxLevel);
            defAtr.BaseValue = (int)(setting.endDef * lv / setting.maxLevel);
            critAtr.BaseValue = 0;
        }

    }

    public class PlayerShareData0 : IShareData
    {
        public List<IUpdate> components = new List<IUpdate>();

        public PlayerInput input;
        public PlayerControl control;
        public RoleMovement movement;
        public PlayerAtkTimer atkTimers;
    }


    public static class InputKey
    {
        public const int NorAck = 0;
        public const int LeftShift = 1;
        public const int Space = 2; //Jump
        public const int Tab = 3;
    }
    #endregion

}
