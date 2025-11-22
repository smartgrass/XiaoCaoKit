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
        public float keepMinDistance = 0.5f; //如果太近 则后退
        public float moveTime = 1.5f; //追踪时间
        public float minMoveTime = 0; //追踪时间

        private float moveTimer; //移动

        public override void OnStart()
        {
            State = FSMState.Update;
            moveTimer = 0;
            control.tempActDis = distance;
            distanceState = 0;
        }

        private int distanceState; //0,1远,-1近,

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

            bool isClose = keepMinDistance > 0 && control.tempTargetDis < keepMinDistance;

            bool isMoveEnd = moveTimer >= moveTime;


            if (distanceState == 0)
            {
                distanceState = isClose ? -1 : 1;
            }
            else if (distanceState < 0)
            {
                if (!isClose && moveTimer > minMoveTime)
                {
                    isMoveEnd = true;
                }
            }
            else
            {
                if (!isFar && moveTimer > minMoveTime)
                {
                    isMoveEnd = true;
                }
            }

            if (!isMoveEnd)
            {
                moveTimer += XCTime.deltaTime;
                if (distanceState < 0)
                {
                    control.Move(-1f);
                }
                else
                {
                    control.Move(1);
                }
            }


            if (isMoveEnd)
            {
                OnExit();
                return;
            }
        }
    }
}