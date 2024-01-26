using UnityEngine;

namespace XiaoCao
{
    public class Enmey0 : EnemyBase, IMsgReceiver
    {
        public bool isRuning;





        public void EnableAI(bool isOn)
        {

        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

        }


        void MoveTo(Vector3 pos, float speedFactor = 1)
        {
            //
            var dir = (pos - gameObject.transform.position).normalized;

            //TODO moveSetting

            //Vector3 moveDelta = moveDir * Data.moveSetting.baseMoveSpeed * Data.roleState.MoveMultFinal * XCTime.fixedDeltaTime;


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
        public int prefabID = 0;
        public EBodyState bodyState;
        public int curSkillId;
        public bool IsFree => bodyState is not EBodyState.Break or EBodyState.Dead;

        public MoveSetting moveSetting;

    }

}
