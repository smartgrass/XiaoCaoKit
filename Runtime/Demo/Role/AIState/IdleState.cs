using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(fileName = "IdleState", menuName = "SO/AI/IdleState", order = 1)]
    public class IdleState : AIFSMBase
    {
        /*
         攻击结束->停在原地->躲避走动 

         没有目标时->随意走动

         并且,进入攻击状态,需要攻击欲望的判定,以及目标存在的判定
         */

        public bool getFromSetting = true;

        [HideIf(nameof(getFromSetting))] public float hideTime = 2;
        [HideIf(nameof(getFromSetting))] public float sleepTimeWhenLoop = 1f;
        [HideIf(nameof(getFromSetting))] public float idleExitRate = 0.5f;

        //将修改Timer的时间, 1表示随机范围为 (-sleepTimeWhenLoop ,hideTime)
        //0.1表示为  (Mathf.Lerp(hideTime, --sleepTimeWhenLoop, enterRandom) , hideTime)
        [Header("随机start时间, 1表示start时间为idleTime结尾,直接退出第一次循环")]
        public float enterRandom = 0.5f;

        public int maxLoop = 3;


        public float Timer { get; set; }

        //Temp
        private HideDir _tempHideDir;
        private Vector3 _tempTargetPos;
        private bool isHideToFar;
        private int _hasLoopTime;
        private bool hasNoTarget;


        public override void OnStart()
        {
            if (getFromSetting)
            {
                hideTime = Setting.hideTime;
                sleepTimeWhenLoop = Setting.sleepTime;
                idleExitRate = Setting.idleExitRate;
            }
            Timer = hideTime;
            State = FSMState.Update;
            GetHideDir();

            if (enterRandom > 0)
            {
                float startValue = Mathf.Lerp(hideTime, -sleepTimeWhenLoop, enterRandom);
                Timer = RandomHelper.RangeFloat(startValue, hideTime);
            }

            if (_isEnterIdle)
            {
                _tempTargetPos = control.idlePos + Random.insideUnitCircle.To3D();
                _idleDir = (_tempTargetPos - transform.position).ToY0();
                Timer = hideTime;
            }

        }

        private void GetHideDir()
        {
            if (!HasTarget)
            {
                control.tempTargetDis = Vector3.Distance(transform.position, control.transform.position);
                //远-> 靠近 内心圆   近->远离 外心圆
                isHideToFar = control.tempTargetDis > control.tempActDis / 0.75f;
            }
            else
            {
                isHideToFar = false;
            }

            //处于玩家 偏左边 就左转
            _tempHideDir = (HideDir)(RandomHelper.Range(0, 2)); //随机取一个方向
        }


        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            Timer -= XCTime.deltaTime;
            if (Timer > 0)
            {
                if (control.HasTarget)
                {
                    HideMove(Setting.walkSR, Setting.walkAnimSR);
                }
                else
                {
                    IdleMove(Setting.walkSR, Setting.walkAnimSR);
                }
            }

            if (Timer <= -sleepTimeWhenLoop)
            {
                if (CheckLoopTimeEnd())
                {
                    OnExit();
                }
            }
        }

        public bool CheckLoopTimeEnd()
        {
            if (maxLoop > 0)
            {
                if (_hasLoopTime >= maxLoop || RandomHelper.GetRandom(idleExitRate))
                {
                    _hasLoopTime = 0;
                    return true;
                }
                //重启循环
                State = FSMState.None;
                _hasLoopTime++;
                return false;
            }
            _hasLoopTime = 0;
            return true;
        }



        private void HideMove(float speedRate, float animSpeedRate)
        {
            _isEnterIdle = false;

            _tempTargetPos = TargetRole.transform.position;

            float targetAngle = isHideToFar ? 90 : 110;

            targetAngle = _tempHideDir == HideDir.MoveLeft ? targetAngle : -targetAngle;

            Vector3 dir = _tempTargetPos - transform.position;
            dir.y = 0;

            var targetDir = MathTool.RotateY(dir, targetAngle);

            control.owner.AIMoveDir(targetDir.normalized * speedRate, animSpeedRate, !Setting.isLookAtTargetOnHide);

            if (Setting.isLookAtTargetOnHide)
            {
                control.owner.AISetLookTarget(_tempTargetPos);
                //TODO 需要兼容
            }
        }

        private bool _isEnterIdle;
        private Vector3 _idleDir;

        private void IdleMove(float speedRate, float animSpeedRate)
        {
            if (!_isEnterIdle)
            {
                control.idlePos = transform.position;
                _tempTargetPos = control.idlePos + Random.insideUnitCircle.To3D();
                _idleDir = (_tempTargetPos - transform.position).ToY0(); ;
                _isEnterIdle = true;
            }

            control.owner.AIMoveDir(_idleDir.normalized * speedRate * 0.8f, animSpeedRate, true);
        }

    }

    public enum HideDir
    {
        MoveLeft, //左移
        MoveRight, //右移
        //Back, //后退
        //Dash //后闪
    }
}
