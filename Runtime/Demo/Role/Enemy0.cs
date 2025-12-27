using System;
using TEngine;

namespace XiaoCao
{
    //Enemy0包括敌人和npc队友
    public class Enemy0 : EnemyBase, IMsgReceiver
    {
        public override void DataCreat()
        {
            enemyData = new EnemyData0();
            data_R = enemyData;
        }

        public EnemyData0 enemyData;

        public EnemyShareData0 component => enemyData.component;

        public AIControl AiControl => component.aiControl;

        //prefabId = enemyId
        public void Init(string prefabId, int level = 1,string skinNameSet = null)
        {
            CreateIdRole(prefabId);
            string skinName = idRole.bodyName;
            if (!string.IsNullOrEmpty(skinNameSet))
            {
                skinName = skinNameSet;
            }
            CreateRoleBody(skinName);
            SetTeam(XCSetting.EnemyTeam);
            data_R.playerAttr.lv = level;
            InitRoleData();

            component.movement = new RoleMovement(this);
            data_R.movement = component.movement;

            AddEnemyData aiData = idRole.gameObject.GetComponent<AddEnemyData>();
            enemyData.AiCmdSetting = aiData.GetAiSkillCmdSetting(raceId);
            component.aiControl = new AIControl(this).Init(aiData.aiId);


            data_R.roleControl = component.aiControl;
            RoleIn();
        }


        /// <summary>
        /// TODO 
        /// 行为: 移动,攻击,受击
        /// AI功能: 扫描目标
        /// AI配置哪里填? 拖拽&编辑器自动查找
        /// </summary>

        protected override void OnUpdate()
        {
            if (BattleData.IsTimeStop && IsEnemyIdentity)
            {
                return;
            }

            component.aiControl.Update();
            component.aiControl.OnTaskUpdate();
            component.movement.Update();

        }

        protected override void OnFixedUpdate()
        {
            if (BattleData.IsTimeStop && IsEnemyIdentity)
            {
                return;
            }

            component.aiControl.FixedUpdate();
            component.movement.FixedUpdate();

            //data_R.roleState.Used();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RoleOut();
        }

        public override void OnDie(AtkInfo atkInfo)
        {
            enemyData.deadInfo = new DeadInfo()
            {
                killerId = atkInfo.atker,
            };
            base.OnDie(atkInfo);
            GameEvent.Send(EGameEvent.EnemyDeadEvent.ToInt(), id);
        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            base.ReceiveMsg(type, fromId, msg);


        }



        public override void AIMsg(ActMsgType actType, string actMsg)
        {
            Debuger.Log($"--- AIMsg {actType} {actMsg}");
            if (string.IsNullOrEmpty(actMsg))
            {
                return;
            }
            switch (actType)
            {
                case ActMsgType.Skill:
                    int.TryParse(actMsg, out int msgNum);
                    component.aiControl.TryPlaySkill(enemyData.AiCmdSetting.GetCmdSkillByIndex(msgNum));
                    break;
                case ActMsgType.OtherSkill:

                    component.aiControl.TryPlaySkill(actMsg);
                    break;
                case ActMsgType.Idle:
                    break;
                case ActMsgType.AutoLock:
                    component.aiControl.DefaultAutoDirect();
                    break;
                default:
                    break;
            }
        }
        
        public void SetFriend(Player0 localPlayer)
        {
            //定时检测, 当与玩家距离过远时,传送到玩家附近
            component.aiControl.SetFriend(localPlayer);
        }
    }


    public enum ActMsgType
    {
        Skill,
        OtherSkill,
        Idle,
        AutoLock
    }

    public class EnemyData0 : RoleData
    {
        public EnemyShareData0 component = new EnemyShareData0();

        public AiSkillCmdSetting AiCmdSetting;

        public DeadInfo deadInfo;

        public int rewardLevel;
    }


    public struct DeadInfo
    {
        //击杀信息
        public int killerId;
        //pos
        //击杀方式

    }

    public class EnemyShareData0 : IShareData
    {
        public AIControl aiControl;
        public RoleMovement movement;

    }

    public class EnemyComponent : RoleComponent<Enemy0>
    {
        public EnemyComponent(Enemy0 _owner) : base(_owner) { }

        public EnemyData0 Data_E => owner.enemyData;
    }


}
