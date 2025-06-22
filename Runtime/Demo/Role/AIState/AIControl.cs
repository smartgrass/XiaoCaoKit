using System;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public partial class AIControl : RoleControl<Role>, IDisposable
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
        public RoleMovement Movement => owner.data_R.movement;
        public bool IsAIFree => owner.IsFree && !owner.IsAnimBreak;
        public bool HasTarget => targetRole != null && !targetRole.IsDie;

        public Role targetRole;
        public float tempActDis = 1.5f;
        public float tempTargetDis;
        public Vector3 idlePos;
        private bool HasAddAction;


        public AIControl Init(string aiId)
        {
            string configPath = XCPathConfig.GetAIPath(aiId).LogStr("--");
            var so = ResMgr.LoadAseet(configPath) as MainDataFSM;
            if (so == null)
            {
                Debug.LogError($"--- no aiId {aiId}");
                configPath = XCPathConfig.GetAIPath("0").LogStr("--");
                so = ResMgr.LoadAseet(configPath) as MainDataFSM;
            }
            mainDataFSM = ScriptableObject.Instantiate(so);
            mainDataFSM.name = so.name;
            mainDataFSM.InitReset(this);
            Movement.overridBaseMoveSpeed = mainDataFSM.setting.moveSpeed;
            idlePos = transform.position;
            //只添加一次
            if (!HasAddAction)
            {
                HasAddAction = true;
                owner.OnDamageAct += OnDamageAct;
            }
            return this;
        }

        public void Dispose()
        {
            if (HasAddAction && owner != null)
            {
                owner.OnDamageAct -= OnDamageAct;
            }
        }
        private void OnDamageAct(AtkInfo info, bool arg2)
        {
            if (!owner.IsAiOn && owner.HasTag(RoleTagCommon.EnableAiIfHurt))
            {
                owner.RemoveTag(RoleTagCommon.EnableAiIfHurt);
                owner.IsAiOn = true;
            }
        }

        public override void Update()
        {
            if (owner.IsDie)
            {
                OnDeadUpdate();
                return;
            }

            owner.CheckBreakUpdate();

            if (!owner.IsAiOn) return;
            if (!IsAIFree) return;


            if (owner.HasTag(RoleTagCommon.ForceFollow))
            {
                owner.FindEnemy(out targetRole, 999, 180);
                ForceFollowTagerUpdate();
                return;
            }

            CheckTarget();

            if (mainDataFSM.State != FSMState.Finish)
            {
                mainDataFSM.OnUpdate();
            }

            if (mainDataFSM.State == FSMState.Finish)
            {
                mainDataFSM.ResetFSM();
            }
        }

        void ForceFollowTagerUpdate()
        {
            if (!HasTarget)
            {
                return;
            }
            CheckDistance();
            if (tempTargetDis < mainDataFSM.setting.seeR)
            {
                owner.RemoveTag(RoleTagCommon.ForceFollow);
            }
            owner.AIMoveTo(targetRole.transform.position ,1);
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

            float seeR = mainDataFSM.setting.seeR; 

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
        public void Lock(bool isMoveBack = false)
        {
            if (targetRole == null)
            {
                OnSearchTarget(targetRole);
            }

            if (targetRole == null)
            {
                return;
            }


            Vector3 dir = (targetRole.transform.position - transform.position).normalized;
            owner.AISetLookTarget(targetRole.transform);

            if (isMoveBack)
            {
                float deltaAngle = Vector3.Angle(dir, transform.forward);
                float sin = -1 * Mathf.Sin(deltaAngle * Mathf.Deg2Rad); ;
                owner.AIMoveDir(dir * sin, 1, false);
            }
            else
            {
                Data_R.movement.RotateByMoveDir(dir, 0.08f);
            }
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
