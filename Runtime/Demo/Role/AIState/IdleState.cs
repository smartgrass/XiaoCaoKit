using NaughtyAttributes;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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

        public float timeRandom = 0.5f;
        public int maxLoop = 3;

        public bool getFromSetting = true;
        [Foldout("getFromSetting")]
        public float baseHideTime = 2.5f;
        [Foldout("getFromSetting")]
        public float baseSleepTime = 1.5f; //发呆时间
        [Foldout("getFromSetting")]
        [Range(0, 1)]
        public float idleExitRate = 0.3f;

        public float Timer { get; set; }

        //Temp
        private Vector3 _tempTargetPos;
        private Vector3 _tempHideDir;
        private bool isFarToNear;
        private int _hasLoopTime;
        private bool hasNoTarget;
        private float curSleepTime;
        private float curHideTime;

        public override void OnStart()
        {
            if (getFromSetting)
            {
                baseHideTime = Setting.idleTime;
                baseSleepTime = Setting.sleepTime;
                idleExitRate = Setting.idleExitRate;
            }

            State = FSMState.Update;
            curSleepTime = RandomHelper.RangeFloat(baseSleepTime * (1 + timeRandom), baseSleepTime * (1 - timeRandom));
            curHideTime = RandomHelper.RangeFloat(baseHideTime * (1 + timeRandom), baseHideTime * (1 - timeRandom));
            Timer = curHideTime;
            GetHideDir();

            //第一次启动
            if (!_isEnterIdle)
            {
                Timer = RandomHelper.RangeFloat(curSleepTime, curHideTime);
            }
            else
            {
                //后续启动
                _tempTargetPos = control.idlePos + Random.insideUnitCircle.To3D();
                _idleDir = (_tempTargetPos - transform.position).ToY0();
                Timer = curHideTime;
            }
        }

        private void GetHideDir()
        {
            if (HasTarget)
            {
                control.tempTargetDis = Vector3.Distance(TargetRole.transform.position, control.transform.position);
                //远-> 靠近 内心圆   近->远离 外心圆
                isFarToNear = control.tempTargetDis > control.tempActDis;
            }
            else
            {
                isFarToNear = false;
            }

            //处于玩家 偏左边 就左转
            HideDirType hideDirType = (HideDirType)(RandomHelper.Range(0, 2)); //随机取一个方向

            if (HasTarget) {
                _isEnterIdle = false;
                _tempTargetPos = TargetRole.transform.position;
                float targetAngle = isFarToNear ? 45 : RandomHelper.RangeFloat(90, 90+45);
                targetAngle = hideDirType == HideDirType.MoveLeft ? targetAngle : -targetAngle;
                Vector3 dir = (_tempTargetPos - transform.position).SetY(0);
                _tempHideDir = MathTool.RotateY(dir, targetAngle);
                control.owner.AISetLookTarget(TargetRole.transform);
            }
        }


        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }
            Timer -= XCTime.deltaTime;
            // Timer > 0 移动, 小于0发呆
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
            else
            {
                control.MoveStop();
            }

            if (Timer <= -curSleepTime)
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


        /// <summary>
        /// TargetRole no null
        /// </summary>
        private void HideMove(float speedRate, float animSpeedRate)
        {
            bool isLookAtTargetOnHide = Setting.isLookAtTargetOnHide;

            control.owner.AIMoveVector(_tempHideDir.normalized * speedRate, animSpeedRate, !isLookAtTargetOnHide);

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

            control.owner.AIMoveVector(_idleDir.normalized * speedRate * 0.8f, animSpeedRate, true);
        }

    }

    public enum HideDirType
    {
        MoveLeft, //左移
        MoveRight, //右移
        //Back, //后退
        //Dash //后闪
    }
}
