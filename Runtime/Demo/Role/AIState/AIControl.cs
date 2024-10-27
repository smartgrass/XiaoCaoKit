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


        //每隔1s检查 敌人和cd
        private float searchTargetTime = 1; //无目标 每隔1s检查
        private float searchTime_hasTarget = 5;//有目标 每隔5s检查
        private float searchTimer = 0; //查找


        public Transform transform => owner.transform;
        public RoleMovement Movement => owner.roleData.movement;
        public bool IsAIFree => owner.IsFree && !owner.IsAnimBreak;
        public bool HasTarget => targetRole != null && !targetRole.IsDie;

        public Role targetRole;
        public float tempActDis = 1.5f;
        public float tempTargetDis;
        public Vector3 idlePos;


        public AIControl Init(int aiId)
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
            Movement.overridBaseMoveSpeed = mainDataFSM.setting.moveSpeed;
            idlePos = transform.position;
            return this;
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

        #region CheckTarget

        private void CheckTarget()
        {
            searchTimer += Time.deltaTime;
            if (targetRole != null && !targetRole.IsDie)
            {
                //有存活目标 每隔5s检查
                if (searchTimer > searchTime_hasTarget)
                {
                    OnSearchTarget(targetRole);
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

            if (targetRole != null && !targetRole.IsDie)
            {
                CheckDistance();
            }
        }


        private void OnSearchTarget(Role last)
        {
            searchTimer = 0;

            float seeR = mainDataFSM.setting.seeR; ;

            float seeAngle = mainDataFSM.setting.seeAngle;

            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, seeR, seeAngle, out float maxS, owner.team);

            if (findRole == null)
            {
                //如果没找到, 则保持原来
                findRole = last;
            }

            targetRole = findRole;
        }

        #endregion


        #region Move
        public void Move(float speedRate = 1)
        {
            Vector3 dir = targetRole == null ? transform.forward :
                  (targetRole.transform.position - transform.position).normalized;

            owner.AIMoveDir(dir, speedRate);
        }
        public void CheckDistance()
        {
            tempTargetDis = GetDistance(targetRole.transform);
        }
        private float GetDistance(Transform tf)
        {
            return Vector3.Distance(transform.position, tf.position);
        }
        #endregion

    }
}
