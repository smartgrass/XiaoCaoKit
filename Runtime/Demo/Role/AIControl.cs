using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public class AIControl : RoleControl<Role>
    {
        public AIControl(Role _owner) : base(_owner) { }

        public AiInfo info;
        //当前动作
        private AiAct curAct;
        //当前
        private AiActPool curPoolData;

        private Role curTarget;
        public Transform transform => owner.transform;
        private AiAct CurAction
        {
            get => curAct;
            set => curAct = value;
        }

        //Cur
        private HideDir curHideDir;
        private float curDistance;
        private bool IsAcking => CurAction != null;//TODO

        //每隔1s检查 敌人和cd
        private float searchTargetTime = 1; //无目标 每隔1s检查
        private float searchTime_hasTarget = 5;//有目标 每隔5s检查
        //Timer
        private float searchTimer = 0; //查找
        private float moveTimer = 0; //移动
        private float aimTimer = 0; //瞄准
        private float ackTimer = 0; //攻击
        private float hideTimer = 0; //躲避
        private float ackEndWaitTimer = 0;
        //Temp
        private bool isHideToFar;

        //setting
        public float ackTime = 0.8f; //act切换间隔,防止瞬间多次触发
        public float aimTime = 0.8f; //攻击前的停顿



        public override void Update()
        {
            if (owner.IsOnAi)
            {
                //查找目标
                CheckTargetAndAct();
                //执行动作
                OnActionUpdate();
            }
        }

        private void CheckTargetAndAct()
        {
            searchTimer += Time.deltaTime;
            if (curTarget != null && curTarget.IsDie)
            {
                //有目标 每隔5s检查
                if (searchTimer > searchTime_hasTarget)
                {
                    OnSearchTarget(curTarget);
                }
            }
            else
            {
                //无目标 每隔1s检查
                if (searchTimer > searchTargetTime)
                {
                    OnSearchTarget(null);
                }
            }
        }

        //执行动作
        private void OnActionUpdate()
        {
            if (CurAction == null && curTarget == null)
            {
                //处于Idle状态 啥都不干
                return;
            }

            if (CurAction == null)
            {
                //如果无动作,但有目标则 =>获取动作
                GetAct();
            }

            if (CurAction == null)
            {
                Debug.Log("No Act");
                return;
            }


            AIActState _curActState = curPoolData.curRuntimeData.state;

            if (_curActState == AIActState.Start)
            {
                ToMoveState();
            }

            if (_curActState == AIActState.MoveTo)
            {
                //靠近 瞄准
                OnMoveTo();
            }
            else if (_curActState == AIActState.Acking)
            {
                OnAcking();
            }

            if (_curActState == AIActState.Hide)
            {
                OnHide();
            }

            //逃离逻辑要做修正
            else if (_curActState == AIActState.End)
            {
                OnEnd();
            }
        }

        private void OnSearchTarget(Role last)
        {
            searchTimer = 0;

            float seeR = 20;

            float seeAngle = 45;

            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, seeR, seeAngle, out float maxS, owner.team);

            if (last != null && findRole == null)
            {
                Debug.Log($"--- Claer target");
            }

            curTarget = findRole;
        }

        private void GetAct()
        {
            curDistance = GetDistance(curTarget.gameObject.transform);

            if (curPoolData == null)
            {
                curPoolData = new AiActPool(info.actPool);
            }
            CurAction = curPoolData.GetOne();
        }

        private void ToMoveState()
        {
            searchTimer = 0;
            moveTimer = 0;
            aimTimer = 0;
            ChangeState(AIActState.MoveTo);
        }
        private void OnMoveTo()
        {
            if (curTarget == null)
            {
                ToAcking();
                return;
            }

            curDistance = GetDistance(curTarget.transform);

            bool isFar = curDistance > CurAction.distance;

            bool isMoveEnd = moveTimer >= CurAction.moveTime;

            if (!isFar)
            {
                isMoveEnd = true;
            }
            if (!isMoveEnd)
            {
                moveTimer += Time.deltaTime;
                Move(1);
            }

            if (isMoveEnd)
            {
                bool isWaitEnd = aimTimer >= aimTime;
                aimTimer += Time.deltaTime;

                var isAimEnd = RoateTo_Slow(curTarget, Data_R.moveSetting.angleSpeed);

                if (isWaitEnd || isAimEnd)
                {
                    ToAcking();
                }
            }
        }

        private void ToAcking()
        {
            owner.AIMsg(CurAction.actType, CurAction.actMsg);
            ackTimer = 0;
            ackEndWaitTimer = 0;
            ChangeState(AIActState.Acking);
        }

        private void OnAcking()
        {
            ackEndWaitTimer += Time.deltaTime;
            if (ackEndWaitTimer > CurAction.endWaitTime)
            {
                ackTimer += Time.deltaTime;
                if (ackTimer > ackTime && Data_R.IsFree)
                {
                    GetHideDir();
                    ChangeState(AIActState.Hide); //结束执行下一个动作
                }
            }
        }


        private void OnHide()
        {
            hideTimer += Time.deltaTime;
            if (hideTimer > CurAction.hideTime)
            {
                ChangeState(AIActState.End);
            }
            else
            {
                //躲避
                float HideSpeedRate = 0.4f;
                HideMove(HideSpeedRate);
            }
        }
        private void OnEnd()
        {
            if (CurAction.HasNextAct)
            {
                CurAction = curPoolData.actPool.Find(a => a.actName == CurAction.nextActName);
            }
            else
            {
                CurAction = null;
            }
        }

        #region Move and Rotate
        private void Move(float speedRate = 1)
        {
            if (curTarget != null)
            {
                Vector3 dir = (curTarget.transform.position - transform.position).normalized;
                owner.AIMoveTo(dir, speedRate);
            }
            else
            {
                Vector3 dir = transform.forward;
                owner.AIMoveTo(dir, speedRate);
            }
        }


        private void HideMove(float speedRate = 0.5f)
        {
            if (curTarget == null)
                return;

            float targetAngle = isHideToFar ? 90 : 110;

            targetAngle = curHideDir == HideDir.MoveLeft ? targetAngle : -targetAngle;

            var dir = curTarget.transform.position - owner.transform.position;

            var targetDir = MathTool.Rotate(dir, targetAngle);

            owner.AIMoveTo(targetDir, speedRate, !CurAction.isLookAtTargetOnHide);

            if (CurAction.isLookAtTargetOnHide)
            {
                RoateTo_Slow(curTarget, Data_R.moveSetting.angleSpeed);
            }

        }

        private bool RoateTo_Slow(Role target, float angleSpeed)
        {
            //owner.SetSlowRoteAnim(true); 转向动画可跳过
            return transform.RoateY_Slow(target.transform.position, angleSpeed);
        }
        #endregion
        #region math
        private float GetDistance(Transform tf)
        {
            return Vector3.Distance(owner.gameObject.transform.position, tf.position);
        }

        private void GetHideDir()
        {
            hideTimer = 0;
            if (curTarget == null)
                return;
            curDistance = GetDistance(curTarget.transform);
            //处于玩家 偏左边 就左转, 但要look
            //远-> 靠近 内心圆   近->远离 外心圆
            isHideToFar = curDistance > CurAction.distance / 0.75f;
            curHideDir = (HideDir)(UnityEngine.Random.Range(0, 2)); //随机取一个方向
            //Debug.Log($"yns Get HideAct {curHideAct}");
        }
        #endregion

        private void ChangeState(AIActState newState)
        {
            curPoolData.curRuntimeData.state = newState;
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
