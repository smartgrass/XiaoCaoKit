using UnityEngine;

namespace XiaoCao
{
    public class Enmey0 : EnemyBase, IMsgReceiver
    {
        public bool isRuning;

        internal EnemyData0 enemyData;

        public void EnableAI(bool isOn)
        {

        }

        /// <summary>
        /// TODO 
        /// 行为: 移动,攻击,受击
        /// AI功能: 扫描目标
        /// AI配置哪里填? 拖拽&编辑器自动查找
        /// </summary>

        protected override void OnUpdate()
        {


        }


        void MoveTo(Vector3 pos, float speedFactor = 1)
        {
            //
            var dir = (pos - gameObject.transform.position).normalized;

            //TODO moveSetting

            //Vector3 moveDelta = moveDir * Data.moveSetting.baseMoveSpeed * RoleData.roleState.MoveMultFinal * XCTime.fixedDeltaTime;


            //TODO 重力
            //v = v + gt
            float v = 0;
            //v = v + Data.moveSetting.g * XCTime.fixedDeltaTime;
            //moveDelta.y += v * XCTime.fixedDeltaTime;

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
                MoveTo(moveMsg.pos, moveMsg.speedFactor);

            }

        }
    }


    public class EnemyData0 : IData
    {

    }


    public class EnemyComponent : EntityComponent<Enmey0>
    {
        public EnemyComponent(Enmey0 owner) : base(owner){}
        public EnemyData0 Data_E  => owner.enemyData;
    }

}
