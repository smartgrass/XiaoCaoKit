﻿using Cinemachine.Utility;
using NaughtyAttributes;
using System;
using System.Buffers.Text;
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
            this.CreateGameObject();
            this.team = 1;
            BaseInit();
            Debug.Log($"---  raceId {idRole.raceId} {idRole.aiId}");
            playerData.playerSetting = ConfigMgr.LoadSoConfig<PlayerSettingSo>().GetSetting(settingId);
            roleData.playerAttr.Init(savaData.lv);

            component.input = new PlayerInput(this);
            component.control = new PlayerControl(this);
            roleData.roleControl = component.control;
            component.aiControl = new AIControl(this);
            component.atkTimers = new PlayerAtkTimer(this);
            component.movement = new PlayerMovement(this);

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
            component.control.OnTaskUpdate();
            component.movement.Update();

            DebugGUI.Log("TimeScale", Time.timeScale.ToString("#.##"));
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

        public override void Update()
        {
            if (!GameData.battleData.CanPlayerControl)
            {
                return;
            }

            data.x = Input.GetAxis("Horizontal");
            data.y = Input.GetAxis("Vertical");
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.J))
            {
                data.inputs[InputKey.NorAck] = true;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[InputKey.Space] = true;

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
            if (owner.IsDie)
            {
                return;
            }
            owner.CheckBreakUpdate();

            if (InputData.inputs[InputKey.LeftShift])
            {
                TryRoll();
                return;
            }

            if (InputData.skillInput != 0)
            {
                TryPlaySkill(InputData.skillInput);
            }

            if (InputData.inputs[InputKey.NorAck])
            {
                TryNorAck();
            }
        }

        public void TryRoll()
        {
            int rollId = GetRollSkillId();

            //判断冷缩
            if (!AtkTimers.IsSkillReady(rollId))
            {
                DebugGUI.Log($"{rollId} cd", AtkTimers.GetWaitTime(rollId));
                return;
            }

            AtkTimers.SetSkillEnterCD(rollId);

            //停止&打断当前动作
            if (IsBusy())
            {
                SetNoBusy();
            }

            RcpPlaySkill(rollId);
        }

        public void TryNorAck()
        {
            if (!Data_R.IsFree || IsBusy())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());


            int nextNorAckIndex = AtkTimers.GetNextNorAckIndex();

            DebugGUI.Log("nextNorAckIndex", nextNorAckIndex);

            Data_P.curNorAckIndex = nextNorAckIndex;

            //索敌
            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, 3, 30, out float maxS, owner.team);
            if (findRole != null)
            {
                owner.transform.RotaToPos(findRole.transform.position);
                Debug.Log($"--- findRole RotaToPos {findRole.gameObject}");
            }
            else
            {
                Debug.Log($"--- findRole no");
            }


            RcpPlaySkill(GetNorAckIdFull(nextNorAckIndex));
        }


        //执行使用技能, 一般不直接使用
        public override void RcpPlaySkill(int skillId)
        {

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


        private void CheckDic(int skillIndex)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.cd = playerSetting.GetSkillCd(skillIndex).cd;
                dic[skillIndex] = skillCdData;
            }
        }


        public bool IsSkillReady(int skillIndex)
        {
            CheckDic(skillIndex);
            return !dic[skillIndex].IsCd;
        }

        public void SetSkillEnterCD(int skillIndex)
        {
            CheckDic(skillIndex);
            dic[skillIndex].EnterCD();
        }

        public float GetProcess(int skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetCurProccess();
        }

        public float GetWaitTime(int skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetWaitTime();
        }


        public class SkillCdData
        {
            public int skillId;

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

        public int raceId = 1;


        public SkillBarData skillBarData = new SkillBarData();

        public Inventory inventory = new Inventory();
        //持有物
        public List<Item> holdItems = new List<Item>();

        //反序列化读取的数据, 可能会出现空的现象
        internal void CheckNull()
        {
            if (skillBarData == null)
            {
                skillBarData = new SkillBarData();
            }
            if (inventory == null)
            {
                inventory = new Inventory();
            }
            if (holdItems == null)
            {
                holdItems = new List<Item>();
            }
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
        public int[] onSkill = new int[GameSetting.SkillCountOnBar];
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
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int maxExp;
        public int atk;
        public int def;
        public float crit;

        public void Init(int lv)
        {
            this.lv = lv;
            maxHp = hp = 100 + 10 * lv;
            maxMp = mp = 100 + 10 * lv;
            maxExp = 100 + 100 * (lv % 10);
            atk = lv * 5;
            def = lv * 1;
        }

        public int GetAtk()
        {
            return atk;
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
