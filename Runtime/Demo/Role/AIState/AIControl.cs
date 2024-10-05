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
        public MainDataFSM mainDataFSM;

        public Role TargetRole;

        public Transform transform => owner.transform;

        public bool IsAIFree { get => owner.IsFree && !owner.IsAnimBreak; }


        public float tempActDistance;
        public float curDistance;

        //每隔1s检查 敌人和cd
        private float searchTargetTime = 1; //无目标 每隔1s检查
        private float searchTime_hasTarget = 5;//有目标 每隔5s检查
        private float searchTimer = 0; //查找

        //setting
        public float ackTime = 0.8f; //act切换间隔,防止瞬间多次触发

        public MiniTimer sleepTimer = new MiniTimer();

        public void Init(int aiId)
        {
            string configPath = XCPathConfig.GetAIPath(aiId).LogStr("--");
            var so = ResMgr.LoadAseet(configPath) as MainDataFSM;
            if (so == null)
            {
                Debug.LogError($"--- no aiId {aiId}");
                configPath = XCPathConfig.GetAIPath(0).LogStr("--");
                so = ResMgr.LoadAseet(configPath) as MainDataFSM;
            }
            mainDataFSM = ScriptableObject.Instantiate(so);
            mainDataFSM.InitReset(this);
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

            if (mainDataFSM.State != FSMState.Finish)
            {
                mainDataFSM.OnUpdate();
            }

            if (mainDataFSM.State == FSMState.Finish)
            {
                Debug.Log($"--- all Finish");
                mainDataFSM.ResetFSM();
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

        public bool HasTarget => TargetRole != null && !TargetRole.IsDie;


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



        #region Move and Rotate
        public void Move(float speedRate = 1)
        {
            Vector3 dir = TargetRole == null ? transform.forward :
                  (TargetRole.transform.position - transform.position).normalized;

            owner.AIMoveDir(dir, speedRate);
        }

        #endregion
        #region math
        private float GetDistance(Transform tf)
        {
            return Vector3.Distance(owner.gameObject.transform.position, tf.position);
        }
        #endregion

    }
    public enum HideDir
    {
        MoveLeft, //左移
        MoveRight, //右移
                   //Back, //后退
                   //Dash //后闪
    }

}
