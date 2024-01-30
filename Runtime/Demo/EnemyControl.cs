using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public class EnemyControl : RoleControl<Enemy0>
    {
        public EnemyControl(Enemy0 _owner) : base(_owner) { }

        public AIInfo aiInfo;

        private AIAct _curAct;

        private Role curTarget;

        public ActGroup actGroup;

        private bool IsAcking => CurAction != null;//TODO

        //每隔1s检查 敌人和cd
        private float searchTargetTime = 1; //无目标 每隔1s检查
        private float searchTime_hasTarget = 5;//有目标 每隔5s检查
        private float searchTimer = 0; //查找
        private float moveTimer = 0; //移动
        private float aimTimer = 0; //瞄准
        private float ackTimer = 0; //攻击
        private float hideTimer = 0; //躲避


        public float ackTime = 0.8f; //act切换间隔,防止瞬间多次触发
        public float aimTime = 0.8f; //攻击前的停顿

        private float ackEndWaitTimer = 0;
        private bool isFarHide;
        private bool isAimEnd;

        //Cur
        private HideAct curHideAct;
        private float curDistance;

        private AIAct CurAction
        {
            get => _curAct;
            set
            {
                _curAct = value;
            }
        }

        public override void Update()
        {
            if (owner.isAiRuning)
            {

                //查找目标
                UpdateTarget();
                //执行动作
                UpdateAction();
            }
        }

        private void UpdateTarget()
        {
            searchTimer += Time.deltaTime;
            if (curTarget != null && curTarget.IsAlive)
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
            curDistance = GetDis(curTarget.gameObject.transform);

            if (actGroup == null)
            {
                actGroup = new ActGroup(aiInfo.actPool);
            }
            CurAction = actGroup.GetOne();
        }

        private float GetDis(Transform tf)
        {
            return Vector3.Distance(owner.gameObject.transform.position, tf.position);
        }
        //执行动作
        private void UpdateAction()
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
                Debug.LogError("No Act");
                return;
            }


            AIActState _curActState = actGroup.curRuntimeData.state;

            if (_curActState == AIActState.Start)
            {
                //TODO
                //OnMoveStateEnter();
            }

            //OnAckStart
            if (_curActState == AIActState.MoveTo)
            {
                //靠近 瞄准
                //OnMoveState();
            }
            else if (_curActState == AIActState.Acking)
            {
                ackEndWaitTimer += Time.deltaTime;
                if (ackEndWaitTimer > CurAction.endWaitTime)
                {
                    ackTimer += Time.deltaTime;
                    if (ackTimer > ackTime && Data_R.IsFree)
                    {
                        GetHideAct();
                        ChangeState(AIActState.Hide); //结束执行下一个动作
                    }
                }
            }

            if (_curActState == AIActState.Hide)
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
            //逃离逻辑要做修正
            else if (_curActState == AIActState.End)
            {
                if (CurAction.HasNextAct)
                {
                    CurAction = actGroup.actPool.Find(a => a.actName == CurAction.nextActName);
                }
                else
                {
                    CurAction = null;
                }
            }
        }

        private void HideMove(float speedRate = 0.5f)
        {
            if (curTarget == null)
                return;

            float targetAngle = isFarHide ? 90 : 110;

            targetAngle = curHideAct == HideAct.MoveLeft ? targetAngle : -targetAngle;

            var dir = curTarget.transform.position - owner.transform.position;

            var targetDir = MathTool.Rotate(dir, targetAngle);

            //TODO-> SetLookAt
           owner.AIMoveTo(targetDir, speedRate, !CurAction.isHide_LookAt); //HideMove

            if (CurAction.isHide_LookAt)
            {
                //RoateTo_Slow(curTarget, angleSpeed);
            }

        }

        private bool RoateTo_Slow(Role target, float angleSpeed, float minDetal = 5)
        {
            //owner.SetSlowRoteAnim(true);
            //return selfAck.transform.RoateY_Slow(target.transform.position, angleSpeed, minDetal);
            return false;
        }

        private void GetHideAct()
        {
            hideTimer = 0;
            if (curTarget == null)
                return;
            curDistance = GetDis(curTarget.transform);
            //处于玩家 偏左边 就左转, 但要look
            //远-> 靠近 内心圆   近->远离 外心圆
            isFarHide = curDistance > CurAction.targetDis / 0.75f;
            curHideAct = (HideAct)(UnityEngine.Random.Range(0, 2)); //随机取一个方向
            //Debug.Log($"yns Get HideAct {curHideAct}");
        }

        private void OnMoveStateEnter()
        {
            searchTimer = 0;
            moveTimer = 0;
            aimTimer = 0;
            isAimEnd = false;
            ChangeState(AIActState.MoveTo);
        }
        private void ChangeState(AIActState newState)
        {
            actGroup.curRuntimeData.state = newState;
        }
    }


}
