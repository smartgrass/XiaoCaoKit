using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 执行追踪, 范围判断的操作
    /// <para>追击有 <see cref="moveTime"/> 的时间上限, 到点就结束追踪、进 <see cref="AtkState"/>。</para>
    /// <para>唯一例外: 目标距离超过 AI 配置里的「寻路: 接入半径」(<see cref="MainDataFSM.pathFarDistance"/>) 时
    /// 不按时间中断 —— 这段路本来就要先绕到通道图入口, 路上被时间上限砍断只会让敌人站在远处空挥;
    /// 等走进半径之后再恢复时间上限。</para>
    /// </summary>
    ///<see cref="AtkState"/>
    [CreateAssetMenu(fileName = "PreAtkState", menuName = "SO/AI/PreAtkState", order = 1)]
    public class PreAtkState : AIFSMBase
    {
        public string des;
        public float distance = 3; //执行距离
        public float keepMinDistance = 0.5f; //如果太近 则后退
        public float moveTime = 1.5f; //追踪时间上限(目标超出「寻路: 接入半径」时不生效)
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

            // 目标还在「寻路: 接入半径」之外: 不按时间中断, 继续追。
            // 这段路要先绕到通道图入口再一路走过来, moveTime 往往不够, 到点就退出只会原地空挥。
            bool noTimeOut = control.tempTargetDis > control.mainDataFSM.pathFarDistance;

            bool isMoveEnd = !noTimeOut && moveTimer >= moveTime;


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
                // 半径外不计时: 走进半径后才开始计时, 否则一路上攒的时间会让它刚进半径就当场放弃
                if (noTimeOut)
                {
                    moveTimer = 0f;
                }
                else
                {
                    moveTimer += XCTime.deltaTime;
                }

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