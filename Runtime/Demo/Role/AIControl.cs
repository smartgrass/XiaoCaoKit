using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public class AIControl : RoleControl<Role>
    {
        public AIControl(Role _owner) : base(_owner)
        {
        }
        
        public AiInfo info;

        //当前行为池
        private AIActPool _actPool;
        //当前动作记录数据
        private AIActData CurActData{ get; set; }
        //当前动作
        private AiAct CurAct{ get; set; }
        
        private Role _targetRole;
        
        public AIRoleConfig Config => info.config;

        public Transform transform => owner.transform;


        //Cur
        private HideDir curHideDir;
        private float curDistance;

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


        public void Init(AiInfo info){
            this.info = info;
            _actPool = new AIActPool(info.actPool);
            owner.roleData.movement.newBaseMoveSpeed = info.config.moveSpeed;
        }

        public override void Update()
        {
            if (owner.IsAiOn)
            {
                //查找目标
                CheckTarget();
                //执行动作
                OnActionUpdate();
            }
        }

        private void CheckTarget()
        {
            searchTimer += Time.deltaTime;
            if (_targetRole != null && !_targetRole.IsDie)
            {
                //有存活目标 每隔5s检查
                if (searchTimer > searchTime_hasTarget)
                {
                    OnSearchTarget(_targetRole);
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
            if (CurAct == null && _targetRole == null)
            {
                //处于Idle状态 啥都不干
                return;
            }

            if (CurAct == null)
            {
                //如果无动作,但有目标则 =>获取动作
                GetAct();
            }

            if (CurAct.actType == ActMsgType.Move){
                
            }

            if (CurAct.actType == ActMsgType.OtherSkill){
                
            }

            ActStateType stateType = CurActData.stateType;
            if (stateType == ActStateType.Start)
            {
                ToMoveState();
            }

            if (stateType == ActStateType.MoveTo)
            {
                //靠近 瞄准
                OnMoveTo();
            }
            else if (stateType == ActStateType.Acking)
            {
                OnAcking();
            }

            if (stateType == ActStateType.Hide)
            {
                OnHide();
            }
            
            else if (stateType == ActStateType.End)
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

            if (findRole == null){
                //如果没找到, 则保持原来
                findRole = last;
            }

            _targetRole = findRole;
        }

        private void GetAct()
        {
            curDistance = GetDistance(_targetRole.gameObject.transform);

            CurActData = _actPool.GetOne();
            CurAct = CurActData.act;
            CurActData.stateType = ActStateType.Start;

            if (CurAct == null)
            {
                Debug.LogError("No Act");
            }

            DebugGUI.Log("actMsg", CurAct.actMsg);
        }

        private void ToMoveState()
        {
            searchTimer = 0;
            moveTimer = 0;
            aimTimer = 0;
            ChangeState(ActStateType.MoveTo);
        }
        private void OnMoveTo()
        {
            if (_targetRole == null)
            {
                ToAcking();
                return;
            }

            curDistance = GetDistance(_targetRole.transform);

            bool isFar = curDistance > CurAct.distance;

            bool isMoveEnd = moveTimer >= CurAct.moveTime;

            if (!isFar && moveTimer > CurAct.minMoveTime)
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

                var isAimEnd = RoateToRole_Slow(_targetRole, Data_R.moveSetting.angleSpeed);

                if (isWaitEnd || isAimEnd)
                {
                    ToAcking();
                }
            }
        }

        private void ToAcking()
        {
            owner.AIMsg(CurAct.actType, CurAct.actMsg);
            ackTimer = 0;
            ackEndWaitTimer = 0;
            ChangeState(ActStateType.Acking);
        }

        private void OnAcking()
        {
            ackEndWaitTimer += Time.deltaTime;
            if (ackEndWaitTimer > CurAct.endWaitTime)
            {
                ackTimer += Time.deltaTime;
                if (ackTimer > ackTime && Data_R.IsFree)
                {
                    GetHideDir();
                    ChangeState(ActStateType.Hide);   
                }
            }
        }

        //TODO 更自然的效果
        private void OnHide()
        {
            hideTimer += Time.deltaTime;
            if (hideTimer > CurAct.hideTime)
            {
                ChangeState(ActStateType.End);
            }
            else
            {
                //躲避
                HideMove(Config.walkSR, Config.walkAnimSR);
            }
        }
        private void OnEnd()
        {
            if (owner.roleData.IsBusy)
            {
                //等待技能事件结束
                return;
            }

            if (CurAct.HasNextAct)
            {
                CurAct = _actPool.FindAct(CurAct.nextActName);
                CurActData.stateType = ActStateType.Start;
            }
            else
            {
                CurAct = null;
            }
        }

        #region Move and Rotate
        private void Move(float speedRate = 1)
        {
            Vector3 dir = _targetRole == null ? transform.forward :
                  (_targetRole.transform.position - transform.position).normalized;

            owner.AIMoveDir(dir, speedRate);
        }

        Vector3 tempTargetPos;

        private void HideMove(float speedRate , float animSpeedRate)
        {
            
            if (_targetRole == null)
            {
                tempTargetPos = transform.forward * 2f;
                tempTargetPos.y = 0;
            }
            else
            {
                tempTargetPos = _targetRole.transform.position;
            }
            //curTarget 假目标位置处理


            float targetAngle = isHideToFar ? 90 : 110;

            targetAngle = curHideDir == HideDir.MoveLeft ? targetAngle : -targetAngle;

            Vector3 dir = tempTargetPos - owner.transform.position;
            dir.y = 0;

            var targetDir = MathTool.RotateY(dir, targetAngle);

            owner.AIMoveDir(targetDir.normalized * speedRate, animSpeedRate, !CurAct.isLookAtTargetOnHide);

            if (CurAct.isLookAtTargetOnHide)
            {
                transform.RoateY_Slow(tempTargetPos, Data_R.moveSetting.angleSpeed);
            }

        }

        private bool RoateToRole_Slow(Role target, float angleSpeed)
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
            if (_targetRole == null)
                return;
            curDistance = GetDistance(_targetRole.transform);
            //处于玩家 偏左边 就左转, 但要look
            //远-> 靠近 内心圆   近->远离 外心圆
            isHideToFar = curDistance > CurAct.distance / 0.75f;
            curHideDir = (HideDir)(UnityEngine.Random.Range(0, 2)); //随机取一个方向
            //Debug.Log($"yns Get HideAct {curHideAct}");
        }
        #endregion

        private void ChangeState(ActStateType newStateType)
        {
            CurActData.stateType = newStateType;
        }
    }


    [Serializable]
    public class AIActPool{
        //配置数据
        public List<AiAct> aiActList { get; set; }
        
        // public Dictionary<string, AiAct> aiActDict { get; set; }
        
        //行为池中数据
        public List<AIActData> ActDataPool { get; set; }
        //全部使用完
        public bool IsAllUsed { get; set; }

        public AIActPool(List<AiAct> aiActList)
        {
            this.aiActList = aiActList;
            ActDataPool = new List<AIActData>();
            CreatRuntimeData();
        }


        public AIActData GetOne()
        {
            if (ActDataPool.Count == 0)
            {
                ReInitAll();
            }

            //随机取出一个
            var actData = ActDataPool.GetRandom(out int index);
            actData.hasUseTime++;

            if (actData.hasUseTime >= actData.act.maxUseTime)
            {
                ActDataPool.RemoveAt(index);
            }
            //全用完,切换下一组
            if (ActDataPool.Count == 0)
            {
                IsAllUsed = true;
            }
            return actData;
        }


        public AiAct FindAct(string actName){
            return aiActList.Find(a => a.actName == actName);
        }
        
        //当行为池抽完时执行重置
        public void ReInitAll()
        {
            IsAllUsed = false;
            ActDataPool.Clear();
            CreatRuntimeData();
        }

        private void CreatRuntimeData()
        {
            foreach (AiAct act in this.aiActList)
            {
                AIActData data = new AIActData()
                {
                    power = act.power,
                    act = act,
                    hasUseTime = 0,
                    stateType = ActStateType.Start
                };
                ActDataPool.Add(data);
            }
        }
    }  
    
    
    public class AIActData : PowerModel
    {
        public AiAct act;
        public int hasUseTime;
        public ActStateType stateType;
    }
    
    public enum ActStateType
    {
        Start,
        MoveTo, //靠近或者瞄准时间
        Acking, //等待技能结束时间
        Hide, //躲避时间
        End, //切换下一技能
    }
    public enum HideDir
    {
        MoveLeft, //左移
        MoveRight, //右移
        //Back, //后退
        //Dash //后闪
    }

}
