using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 执行追踪, 范围判断的操作
    /// </summary>
    ///<see cref="AtkState"/>
    [CreateAssetMenu(fileName = "PreAtkState", menuName = "SO/AI/PreAtkState", order = 1)]
    public class PreAtkState : AIFSMBase
    {
        public string des;
        public float distance = 3; //执行距离
        public float moveTime = 1.5f; //追踪时间
        public float minMoveTime = 0; //追踪时间

        private float moveTimer; //移动
        public override void OnStart()
        {
            State = FSMState.Update;
            moveTimer = 0;
            control.tempActDis = distance;
        }
        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (TargetRole == null)
            {
                OnExit();
                return;
            }

            control.CheckDistance();

            bool isFar = control.tempTargetDis > distance;

            bool isMoveEnd = moveTimer >= moveTime;

            if (!isFar && moveTimer > minMoveTime)
            {
                isMoveEnd = true;
            }

            //control.Move(0.1); 可以当做瞄准
            //倒车要考虑下

            if (!isMoveEnd)
            {
                moveTimer += XCTime.deltaTime;
                control.Move(1);
            }


            if (isMoveEnd)
            {
                OnExit();
                return;
            }
        }
    }
}
