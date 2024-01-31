using System.ComponentModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace XiaoCao
{
    public class Enemy0 : EnemyBase, IMsgReceiver
    {

        internal EnemyData0 enemyData = new EnemyData0();

        public EnemyShareData0 component = new EnemyShareData0();


        public void Init()
        {
            component.aiControl = new AIControl(this);
            component.aiControl.info = ConfigMgr.LoadSoConfig<AiInfoSo>().GetSetting(idRole.aiId);

            component.movement = new RoleMovement(this);
            roleData.movement = component.movement;
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

            if (type == EntityMsgType.StartSkill)
            {
                int msgInt = (int)msg;
                //playerComponent.control.TryPlaySkill(msgInt);
            }
            else if (type == EntityMsgType.MoveToPos)
            {
                MoveMsg moveMsg = (MoveMsg)msg;
                AIMoveTo(moveMsg.pos, moveMsg.speedFactor);

            }

        }


        public override void AIMoveTo(Vector3 pos, float speedFactor = 1, bool isLookDir = false)
        {
            var dir = (pos - gameObject.transform.position).normalized;
            roleData.movement.SetMoveDir(dir, isLookDir);
        }

        public override void AIMsg(ActMsgType actType, string actMsg)
        {
            switch (actType)
            {
                case ActMsgType.Skill:
                    int skillId = component.aiControl.GetFullSkillId(int.Parse(actMsg));
                    component.aiControl.TryPlaySkill(skillId);
                    break;
                case ActMsgType.OtherSkill:
                    //特殊处理
                    int otherSkilId = int.Parse(actMsg);
                    component.aiControl.TryPlaySkill(otherSkilId);
                    break;
                case ActMsgType.Move:
                    break;
                default:
                    break;
            }
        }

    }


    public class EnemyData0 : IData
    {

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
