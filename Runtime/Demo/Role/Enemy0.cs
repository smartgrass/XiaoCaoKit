using System.ComponentModel;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        public void Init(int prefabId, int level = 1)
        {
            CreateIdRole(prefabId);
            CreateRoleBody(idRole.bodyName);
            SetTeam(0);


            component.movement = new RoleMovement(this);
            roleData.movement = component.movement;

            var info = ConfigMgr.LoadSoConfig<AiInfoSettingSo>().GetOnArray(idRole.aiId);
            component.aiControl = new AIControl(this);
            component.aiControl.Init(info);

            roleData.playerAttr.Init(level);
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
            component.aiControl.Update();
            component.aiControl.OnTaskUpdate();
            component.movement.Update();

        }

        protected override void OnFixedUpdate()
        {
            component.aiControl.FixedUpdate();
            component.movement.FixedUpdate();

            roleData.roleState.Used();
        }

        protected override void OnDestroy()
        {
            RoleOut();
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
                    int msgNum = int.Parse(actMsg);
                    int skillId =  raceInfo.GetCmdSkillIndex(msgNum);
                    component.aiControl.TryPlaySkill(skillId);
                    break;
                case ActMsgType.OtherSkill:
                    //特殊处理
                    int otherSkilId = int.Parse(actMsg);
                    component.aiControl.TryPlaySkill(otherSkilId);
                    break;
                case ActMsgType.Idle:
                    break;
                default:
                    break;
            }
        }

    }


    public class EnemyData0 : RoleData
    {
        public EnemyShareData0 component = new EnemyShareData0();
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
