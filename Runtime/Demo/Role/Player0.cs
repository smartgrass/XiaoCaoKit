using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
            if (isMainPlayer)
            {
                idRole.bodyName = ConfigMgr.GetSettingSkinName();
            }
            CreateRoleBody(idRole.bodyName);
            SetTeam(1);

            playerData.playerSetting = ConfigMgr.playerSettingSo.GetOrDefault(raceId, 0);
            roleData.playerAttr.lv = savaData.lv;
            InitRoleData();

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
            // ConfigMgr.LocalSetting.GetBoolValue 暂时不用

            //if (skillBarData == null || skillBarData.onSkill == null)
            //{
            skillBarData = SkillBarData.GetDefault();
            //}
            if (inventory == null)
            {
                inventory = new Inventory();
            }
            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<int, int>();
            }
            if (string.IsNullOrEmpty(prefabId))
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
        public string[] onSkill;

        public static SkillBarData GetDefault()
        {
            SkillBarData skillBarData = new SkillBarData();
            skillBarData.onSkill = new string[GameSetting.SkillCountOnBar];
            for (int i = 0; i < skillBarData.onSkill.Length; i++)
            {
                skillBarData.onSkill[i] = (i + 1).ToString();
            }
            return skillBarData;
        }
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

        public void AddInputXY(float x,float y)
        {
            if (x != 0 || y != 0)
            {
                this.x = x;
                this.y = y;
            }
        }

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
            if (lv < 1)
            {
                lv = 1;
            }
            this.lv = lv;
            maxExp = 100 + 100 * (lv % 10);

            maxHpAtr.BaseValue = hp = (int)(setting.endHp * lv / setting.maxLevel);
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
        public const int Focus = 4;
    }
    #endregion

}
