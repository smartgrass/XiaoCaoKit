using NaughtyAttributes;
using OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public partial class AIControl : RoleControl<Role>
    {
        public AIControl(Role _owner) : base(_owner) { }
        /*
         AI配置的矛盾点:
        一个角色可以有多种行为
        如休息,追踪,攻击,躲避
        但是攻击需要比较多的参数, 如攻击前摇,后摇
        以及衔接接下一个行为
        而配置需要序列化,不支持多态字段 [SerializeReference], 得借助odin插件才能

        或者借助ScriptableObject,分成子节点,缺点是,子节点都需要实例化一遍,防止多个敌人引用了同一SO文件
        So方案可行,但要重写很多东西.
         */
        public MainFSM mainFSM;

        public Role TargetRole;

        public Transform transform => owner.transform;

        public bool IsAIFree { get => owner.IsFree && !owner.IsAnimBreak; }


        //Cur
        private HideDir curHideDir;

        public float curDistance;

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
        private bool aimFinish;

        //setting
        public float ackTime = 0.8f; //act切换间隔,防止瞬间多次触发

        public MiniTimer sleepTimer = new MiniTimer();

        public void Init(int aiId)
        {
            string configPath = XCPathConfig.GetAIPath(aiId).LogStr("--");
            var so = ResMgr.LoadAseet(configPath) as MainFSM;
            if (so == null)
            {
                Debug.LogError($"--- no aiId {aiId}");
                configPath = XCPathConfig.GetAIPath(0).LogStr("--");
                so = ResMgr.LoadAseet(configPath) as MainFSM;
            }
            mainFSM = ScriptableObject.Instantiate(so);
            mainFSM.InitReset(this);
            //this.info = info;
            //_actPool = new AIActPool(info.actPool, info.idleAct);
            //owner.roleData.movement.newBaseMoveSpeed = info.config.moveSpeed;
        }

        public override void Update()
        {
            if (owner.IsDie)
            {
                OnDeadUpdate();
                return;
            }

            if (!owner.IsAiOn) return;
            if (!IsAIFree) return;


            CheckTarget();

            if (mainFSM.State != FSMState.Finish)
            {
                mainFSM.OnUpdate();
            }

            if (mainFSM.State == FSMState.Finish)
            {
                Debug.Log($"--- all Finish");
                mainFSM.ResetFSM();
            }
        }

        public void CheckTarget()
        {
            searchTimer += Time.deltaTime;
            if (TargetRole != null && !TargetRole.IsDie)
            {
                //有存活目标 每隔5s检查
                if (searchTimer > searchTime_hasTarget)
                {
                    OnSearchTarget(TargetRole);
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

            if (TargetRole != null && !TargetRole.IsDie)
            {
                curDistance = GetDistance(TargetRole.transform);
            }
        }

        private void OnSearchTarget(Role last)
        {
            searchTimer = 0;

            float seeR = 20;

            float seeAngle = 45;

            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, seeR, seeAngle, out float maxS, owner.team);

            if (findRole == null)
            {
                //如果没找到, 则保持原来
                findRole = last;
            }

            TargetRole = findRole;
        }


        private void OnHide()
        {
            //hideTimer += Time.deltaTime;
            //if (hideTimer > CurAct.hideTime)
            //{
            //    ChangeState(ActStateType.End);
            //}
            //else
            //{
            //    //躲避
            //    HideMove(Config.walkSR, Config.walkAnimSR);
            //}
        }
        private void OnEnd()
        {
            //if (Data_R.IsBusy)
            //{
            //    //等待技能事件结束
            //    return;
            //}
            //if (!owner.IsFree)
            //{
            //    return;
            //}


            //if (CurAct.HasNextAct)
            //{
            //    CurAct = _actPool.FindAct(CurAct.nextActName);
            //    CurActData.stateType = ActStateType.Start;
            //}
            //else
            //{
            //    CurAct = null;
            //}
        }

        #region Move and Rotate
        public void Move(float speedRate = 1)
        {
            Vector3 dir = TargetRole == null ? transform.forward :
                  (TargetRole.transform.position - transform.position).normalized;

            owner.AIMoveDir(dir, speedRate);
        }

        Vector3 tempTargetPos;

        private void HideMove(float speedRate, float animSpeedRate)
        {

            //if (TargetRole == null)
            //{
            //    tempTargetPos = transform.forward * 2f;
            //    tempTargetPos.y = 0;
            //}
            //else
            //{
            //    tempTargetPos = TargetRole.transform.position;
            //}
            ////curTarget 假目标位置处理


            //float targetAngle = isHideToFar ? 90 : 110;

            //targetAngle = curHideDir == HideDir.MoveLeft ? targetAngle : -targetAngle;

            //Vector3 dir = tempTargetPos - owner.transform.position;
            //dir.y = 0;

            //var targetDir = MathTool.RotateY(dir, targetAngle);

            //owner.AIMoveDir(targetDir.normalized * speedRate, animSpeedRate, !CurAct.isLookAtTargetOnHide);

            //if (CurAct.isLookAtTargetOnHide)
            //{
            //    //TODO 需要兼容
            //    transform.RoateY_Slow(tempTargetPos, Data_R.moveSetting.angleSpeed, 8);
            //}

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
            //    hideTimer = 0;
            //    if (TargetRole == null)
            //        return;
            //    curDistance = GetDistance(TargetRole.transform);
            //    //处于玩家 偏左边 就左转, 但要look
            //    //远-> 靠近 内心圆   近->远离 外心圆
            //    isHideToFar = curDistance > CurAct.distance / 0.75f;
            //    curHideDir = (HideDir)(UnityEngine.Random.Range(0, 2)); //随机取一个方向
            //                                                            //Debug.Log($"yns Get HideAct {curHideAct}");
        }
        #endregion
    }


    [Serializable]
    public class AIActPool
    {
        //配置数据
        public List<AiAct> aiActList { get; set; }

        // public Dictionary<string, AiAct> aiActDict { get; set; }

        //行为池中数据,只保存Power大于0的行为
        public List<AIActData> ActDataPool { get; set; }
        public AIActData IdleActData { get; set; }

        //全部使用完
        public bool IsAllUsed { get; set; }

        public AIActPool(List<AiAct> aiActList, AiAct idleAct)
        {
            this.aiActList = aiActList;
            ActDataPool = new List<AIActData>();
            IdleActData = new AIActData(idleAct);
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


        public AiAct FindAct(string actName)
        {
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
                if (act.power <= 0)
                {
                    continue;
                }
                AIActData data = new AIActData(act);
                ActDataPool.Add(data);
            }
        }
    }


    public class AIActData : PowerModel
    {
        public AIActData() { }
        public AIActData(AiAct act)
        {
            this.act = act;
            power = act.power;
            hasUseTime = 0;
            stateType = ActStateType.Start;
        }

        public AiAct act;
        public int hasUseTime;
        public ActStateType stateType;
    }

    public enum ActStateType
    {
        Start,
        MoveTo, //靠近或者瞄准时间
        WaitAckEnd, //等待技能结束时间
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
