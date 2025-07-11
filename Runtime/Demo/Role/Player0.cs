﻿using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class Player0 : PlayerBase, IMsgReceiver
    {
        public override void DataCreat()
        {
            playerData = new PlayerData0();
            data_R = playerData;
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
                AddTag(RoleTagCommon.MainPlayer);
                GameDataCommon.Current.player0 = this;
                GameDataCommon.Current.LocalPlayerId = this.id;
            }
            CreateRoleBody(idRole.bodyName);
            SetTeam(XCSetting.PlayerTeam);

            playerData.playerSetting = ConfigMgr.PlayerSettingSo.GetOrDefault(raceId, 0);
            GetPlayerCmdList(true);
            data_R.playerAttr.lv = savaData.lv;
            InitRoleData();

            component.input = new PlayerInput(this);
            component.control = new PlayerControl(this);
            component.buffControl = new BuffControl(this);
            data_R.roleControl = component.control;
            //component.aiControl = new AIControl(this);
            component.atkTimers = new PlayerAtkTimer(this);
            component.movement = new RoleMovement(this);
            data_R.movement = component.movement;

            RoleIn();
        }

        public void GetPlayerCmdList(bool isLoadByConfig)
        {
            AiSkillCmdSetting AiCmdSetting = ConfigMgr.LoadSoConfig<AiCmdSettingSo>().GetOrDefault(raceId, 0);
            var seting = playerData.playerSetting;
            seting.rollSkillId = AiCmdSetting.rollId;


            if (isLoadByConfig && ConfigMgr.LocalRoleSetting.saveSkillBar)
            {
                seting.skillIdList = ConfigMgr.LocalRoleSetting.skillBarSetting;
            }
            else
            {
                seting.skillIdList = AiCmdSetting.cmdSkillList;
            }

        }

        protected override void OnUpdate()
        {
            component.input.Update();
            component.control.Update();
            component.control.OnTaskUpdate();
            component.movement.Update();
            component.buffControl.Update();

            //考虑增加add模式
            ForDebug();
        }

        void ForDebug()
        {
            if (Time.timeScale > 1)
            {
                DebugGUI.Log("TimeScale", Time.timeScale.ToString("#.##"));
            }
            if (Anim.speed > 1)
            {
                DebugGUI.Log("Anim", Anim.speed);
            }

        }


        protected override void OnFixedUpdate()
        {
            component.control.FixedUpdate();
            component.movement.FixedUpdate();
            component.input.FixedUpdate();

            //DataClear
            //component.input.Used();
            //data_R.roleState.Used();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            component.buffControl.OnDestroy();
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
            component.control.BreakAllBusy();
        }


        public void ChangeToTestEnemy(string testChangeToEnmey)
        {
            string rolePath = XCPathConfig.GetIdRolePath(testChangeToEnmey);
            GameObject idRoleGo = ResMgr.LoadAseet<GameObject>(rolePath);
            IdRole targetIdRole = idRoleGo.GetComponent<IdRole>();
            string bodyName = targetIdRole.bodyName;

            ChangeRaceId(targetIdRole.raceId);
            ChangeBody(bodyName);
        }

        public void ChangeRaceId(int setRaceId)
        {
            idRole.raceId = setRaceId;
            this.raceId = setRaceId;
            GetPlayerCmdList(false);
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
        public static PlayerSaveData Current => GameAllData.playerSaveData;

        public int lv;

        public int raceId = 0;

        public string prefabId;

        //技能解锁状态
        public Dictionary<string, int> skillUnlockDic = new Dictionary<string, int>();

        //ItemUI
        public Inventory inventory = new Inventory();
        //持有物
        public List<Item> holdItems = new List<Item>();

        //反序列化读取的数据, 可能会出现空的现象
        internal void CheckNull()
        {
            // ConfigMgr.LocalSetting.GetBoolValue 暂时不用

            if (inventory == null)
            {
                inventory = new Inventory();
            }
            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<string, int>();
            }
            if (string.IsNullOrEmpty(prefabId))
            {
                prefabId = "P_0";
            }
        }

        public void AddSkillLevel(string skillId)
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

        public float lastNorAckTime;

        //平a缓存
        public bool norAckCache;

        public PlayerInputData inputData = new PlayerInputData(); //方向,ack 1,2 ,skill,空格

        public PlayerSetting playerSetting;

        internal string GetBarSkillId(int index)
        {
            if (index < 0)
            {
                return "";
            }

            var list = playerSetting.skillIdList;
            if (list.Count > index)
            {
                return list[index];
            }
            return list[index % list.Count];
        }
    }

    //为了兼容触摸和键盘都能输入
    public class LocalInput
    {
        public float x;
        public float y;
    }


    ///<see cref="InputKey"/>
    public class PlayerInputData
    {
        public float X
        {
            get
            {
                return x + localInput.x;
            }
        }
        public float Y
        {
            get
            {
                return y + localInput.y;
            }
        }

        private float x;
        private float y;

        public LocalInput localInput = new LocalInput();

        //InputKey
        public bool[] inputs = new bool[8];
        public int skillInput = -1; //-1无效值


        public KeyCode[] CheckKeyCode = new KeyCode[] {
            KeyCode.Alpha0, KeyCode.Alpha1
        };


        public KeyCode[] CheckKeyCode2 = new KeyCode[] {
            KeyCode.K, KeyCode.L , KeyCode.U,KeyCode.I,KeyCode.O
        };


        public void SetXY(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public void SetLocalXY(float x, float y)
        {
            localInput.x = x;
            localInput.y = y;
        }

        public void ResetKey()
        {
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = false;
            skillInput = -1;
        }



        //public void Copy(PlayerInputData data)
        //{
        //    this.x = data.x;
        //    this.y = data.y;
        //    this.inputs = data.inputs;
        //    skillInput = data.skillInput;
        //}

    }

    public static class InputKey
    {
        public const int NorAck = 0;
        public const int LeftShift = 1;
        public const int Space = 2; //Jump
        public const int Tab = 3;
        public const int Focus = 4;
    }
    public class PlayerAttr
    {
        public int lv;
        public int maxExp;

        public float hp; //实时数值
        public float mp;

        public float MapHp => GetAttribute(EAttr.MaxHp).CurrentValue;
        public int MapMp => (int)GetAttribute(EAttr.MaxMp).CurrentValue;
        public int Atk => (int)GetAttribute(EAttr.Atk).CurrentValue;
        public int Def => (int)GetAttribute(EAttr.Def).CurrentValue;
        public float Crit => GetAttribute(EAttr.Crit).CurrentValue;
        //吸血 
        public float AtkRecoverHp => GetAttribute(EAttr.AtkRecoverHp).CurrentValue;

        public int RoleId { get; set; }

        public Dictionary<string, AttributeValue> attrDic = new Dictionary<string, AttributeValue>();

        public void Init(int roleId, int lv, AttrSetting setting)
        {
            if (lv < 1)
            {
                lv = 1;
            }
            RoleId = roleId;
            this.lv = lv;
            if (setting.id == 0)
            {
                Debug.Log($"--- set player level {lv}");
            }

            maxExp = 100 + 100 * (lv % 10);

            float power = (lv + setting.offsetLevel) / setting.maxLevel;

            GetAttribute(EAttr.MaxHp).BaseValue = hp = (int)(setting.endHp * power);
            GetAttribute(EAttr.MaxMp).BaseValue = mp = (int)(setting.endMp * power);
            GetAttribute(EAttr.Atk).BaseValue = (int)(setting.endAtk * power);
            GetAttribute(EAttr.Def).BaseValue = (int)(setting.endDef * power);
            GetAttribute(EAttr.Crit).BaseValue = 0;
            GetAttribute(EAttr.MoveSpeedMult).BaseValue = 1;
            GetAttribute(EAttr.NoDamage).BaseValue = 0;

        }

        public AttributeValue GetAttribute(EAttr eAttr, float defaultValue = 0)
        {
            string key = eAttr.ToString();
            if (!attrDic.ContainsKey(key))
            {
                attrDic[key] = new AttributeValue()
                {
                    BaseValue = defaultValue,
                };
            }
            return attrDic[key];
        }

        public void ChangeAttrValue(EAttr eAttr, string key, AttributeModifier modifier)
        {
            AttributeValue attr = GetAttribute(eAttr);
            attr.AddModifier(key, modifier);
        }

        public void RemoveModifier(EAttr eAttr, string key)
        {
            AttributeValue attr = GetAttribute(eAttr);
            attr.RemoveModifier(key);
        }


        public float GetValue(EAttr eAttr, float defaultValue = 0) //冗余参数
        {
            string key = eAttr.ToString();
            if (!attrDic.ContainsKey(key))
            {
                attrDic[key] = new AttributeValue()
                {
                    BaseValue = defaultValue,
                };
            }
            return attrDic[key].CurrentValue;
        }

        public void ChangeNowValue(ENowAttr eAttr, float delta)
        {
            bool isSendMsg = RoleId.IsLocalPlayerId();
            switch (eAttr)
            {
                case ENowAttr.Hp:
                    hp += delta;
                    break;
                case ENowAttr.Mp:
                    mp += delta;
                    break;
                default:
                    //无处理
                    isSendMsg = false;
                    return;
            }
            if (isSendMsg)
            {
                GameEvent.Send<ENowAttr, float>(EGameEvent.LocalPlayerChangeNowAttr.Int(), eAttr, delta);
            }

        }
    }

    public class PlayerShareData0 : IShareData
    {
        public List<IUpdate> components = new List<IUpdate>();

        public PlayerInput input;
        public PlayerControl control;
        public RoleMovement movement;
        public PlayerAtkTimer atkTimers;
        public BuffControl buffControl;
    }

    #endregion

}
