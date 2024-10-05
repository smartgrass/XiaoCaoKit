using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(fileName = "HideState", menuName = "SO/AI/HideState", order = 1)]
    public class HideState : AIFSMBase, ITimeState
    {
        public bool getFromSetting = true;

        public float totalTime = 2;


        public float Timer { get; set; }
        public float TotalTime { get => totalTime; set => totalTime = value; }

        private bool isHideToFar;
        private HideDir curHideDir;

        public override void OnStart()
        {
            if (getFromSetting)
            {
                totalTime = setting.hideTime;
            }
            Timer = TotalTime;
            State = FSMState.Update;
            control.CheckTarget();
            GetHideDir();
        }

        private void GetHideDir()
        {
            if (HasTarget)
                return;
;
            //处于玩家 偏左边 就左转, 但要look
            //远-> 靠近 内心圆   近->远离 外心圆
            isHideToFar = curDistance > control.tempActDistance  / 0.75f;
            curHideDir = (HideDir)(UnityEngine.Random.Range(0, 2)); //随机取一个方向                       
        }


        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (Timer > 0)
            {
                Timer -= XCTime.deltaTime;

                HideMove(setting.walkSR, setting.walkAnimSR);

            }

            if (Timer <= 0)
            {
                OnExit();
            }
        }


        private Vector3 tempTargetPos;

        private void HideMove(float speedRate, float animSpeedRate)
        {

            if (TargetRole == null)
            {
                tempTargetPos = transform.forward * 2f;
                tempTargetPos.y = 0;
            }
            else
            {
                tempTargetPos = TargetRole.transform.position;
            }
            //curTarget 假目标位置处理


            float targetAngle = isHideToFar ? 90 : 110;

            targetAngle = curHideDir == HideDir.MoveLeft ? targetAngle : -targetAngle;

            Vector3 dir = tempTargetPos - transform.position;
            dir.y = 0;

            var targetDir = MathTool.RotateY(dir, targetAngle);

            control.owner.AIMoveDir(targetDir.normalized * speedRate, animSpeedRate, !setting.isLookAtTargetOnHide);

            if (setting.isLookAtTargetOnHide)
            {
                control.owner.AISetLookTarget(tempTargetPos);
                //TODO 需要兼容
                
            }

        }
    }
}
