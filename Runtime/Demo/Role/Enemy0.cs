using System;
using TEngine;

namespace XiaoCao
{
    //Enemy0相当于base
    public class Enemy0 : EnemyBase, IMsgReceiver
    {
        public override void DataCreat()
        {
            enemyData = new EnemyData0();
            roleData = enemyData;
        }

        public EnemyData0 enemyData;

        public EnemyShareData0 component => enemyData.component;


        //prefabId = enemyId
        public void Init(string prefabId, int level = 1)
        {
            CreateIdRole(prefabId);
            CreateRoleBody(idRole.bodyName);
            SetTeam(0);
            roleData.playerAttr.lv = level;
            InitRoleData();

            component.movement = new RoleMovement(this);
            roleData.movement = component.movement;

            AddEnemyData aiData = idRole.gameObject.GetComponent<AddEnemyData>();

            int curCmdSettingId = aiData.cmdSettingId >= 0 ? aiData.cmdSettingId : raceId;
            enemyData.AiCmdSetting = ConfigMgr.LoadSoConfig<AiCmdSettingSo>().GetOrDefault(curCmdSettingId, 0);
            component.aiControl = new AIControl(this).Init(aiData.aiId);


            roleData.roleControl = component.aiControl;
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
            if (BattleData.IsTimeStop)
                return;

            component.aiControl.Update();
            component.aiControl.OnTaskUpdate();
            component.movement.Update();

        }

        protected override void OnFixedUpdate()
        {
            if (BattleData.IsTimeStop)
                return;

            component.aiControl.FixedUpdate();
            component.movement.FixedUpdate();

            roleData.roleState.Used();
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
            GameEvent.Send(EGameEvent.EnemyDeadEvent.Int(), id);
        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            base.ReceiveMsg(type, fromId, msg);


        }



        public override void AIMsg(ActMsgType actType, string actMsg)
        {
            Debuger.Log($"--- {actType} {actMsg}");
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

        //internal void AddTag(object forceFollow)
        //{
        //    throw new NotImplementedException();
        //}
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
