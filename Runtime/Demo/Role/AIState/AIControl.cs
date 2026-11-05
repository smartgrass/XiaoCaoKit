using GG.Extensions;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

// 移除了错误的引用，不应该在运行时代码中引用 UnityEditor 命名空间

namespace XiaoCao
{
    /// <summary>
    /// 敌人AI
    /// </summary>
    public partial class AIControl : RoleControl<Role>, IDisposable
    {
        public AIControl(Role _owner) : base(_owner)
        {
        }

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
        private float searchTime_hasTarget = 5; //有目标 每隔5s检查
        private float searchTimer = 0; //查找

        //新增距离检测定时器
        private float checkDistanceTimer = 0;
        private float checkDistanceInterval = 0.5f; //每隔0.5s检测一次距离

        public Transform transform => owner.transform;
        public RoleMovement Movement => owner.data_R.movement;
        public bool IsAIFree => owner.IsFree && !owner.IsAnimBreak;
        public bool HasTarget => targetRole != null && !targetRole.IsDie;

        //处于表演状态
        public bool IsOnShowAction { get; set; }
        private FriendState _friendState;
        private bool IsFriendRole => _friendState != FriendState.NoFriend;
        private Role _friend;

        /// <summary>
        /// 友军"离玩家太远就自己回到玩家身边"的总开关(含 20 米外的瞬移, 见 <see cref="CheckFriendMove"/>)。
        /// 支援角色被派去操作功能件时要关掉, 否则会被这套逻辑从功能件旁边拽回玩家身边。
        /// </summary>
        public bool FriendKeepWithPlayer { get; set; } = true;

        public Role targetRole;
        private bool HasAddEnemyDeadEvent;
        public float tempActDis = 1.5f;
        public float tempTargetDis;
        public Vector3 idlePos;
        private bool HasAddAction;

        #region 寻路

        private EnemyMoveMode _moveMode;

        /// <summary>移动模式: Init 时取 AI 配置(MainDataFSM.moveMode), 运行时也可以直接改, 改了会立刻重算路径</summary>
        public EnemyMoveMode MoveMode
        {
            get => _moveMode;
            set
            {
                if (_moveMode == value)
                {
                    return;
                }

                _moveMode = value;
                ResetPath();
            }
        }

        /// <summary>
        /// 演出里执行过 FollowPlayer: 之后由 <see cref="UpdateShowFollowPlayer"/> 接管移动, 按当前移动模式追玩家。
        /// <para>这是持续状态: <see cref="EnemyShowAction"/> 会一直挂着演出协程, 期间不进 AI 状态机(不攻击),
        /// 直到追进 <see cref="TargetStopDistance"/> (留空时) / 到点(填了秒数) / 受击打断(见 <see cref="OnDamageAct"/>)
        /// / 玩家或敌人死亡; 每次演出开始会被重置。</para>
        /// </summary>
        public bool IsShowFollowPlayer { get; set; }

        /// <summary>当前路径点(世界坐标), 最后一个点就是目标</summary>
        private readonly List<Vector3> _pathPoints = new List<Vector3>();

        private int _pathIndex;
        private float _pathRefreshTimer;
        private Vector3 _pathQueryTargetPos;
        private PassPathResult _pathResult;
        private bool _pathQueried;

        /// <summary>路径重算间隔(秒); 走完当前路径点或目标挪远了也会立刻重算</summary>
        private const float PathRefreshInterval = 1f;

        /// <summary>目标移动超过这个距离就立刻重算路径</summary>
        private const float PathTargetMoveDistance = 1.5f;

        /// <summary>离路径点多近算到达, 换下一个点</summary>
        private const float PathReachDistance = 0.8f;

        /// <summary>重算路径后, 在新路径里找回"原来那个目标点"的容差; 超过它就当作路径变了, 从头走</summary>
        private const float PathProgressMatchDistance = 1f;

        #endregion

        private enum FriendState
        {
            NoFriend,
            Idle,
            Follow,
        }


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
            Movement.curBaseMoveSpeed = mainDataFSM.setting.moveSpeed;
            MoveMode = mainDataFSM.moveMode;
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

            if (HasAddEnemyDeadEvent)
            {
                HasAddEnemyDeadEvent = false;
                GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
            }
        }

        private void OnDamageAct(AtkInfo info, bool arg2)
        {
            //正在操作炮台/功能件: 受击不打断操控, 别把 AI 重新打开。
            //操控只有在被打出 break 时才断开, 由功能件自己处理(见 Turret.OnOperatorDamaged);
            //这里提前开 AI 会让角色站在炮台上自己打拳, 却还占着炮台
            if (FunctionItemMgr.IsOperating(owner))
            {
                return;
            }

            //演出里的"跟随玩家"被打断: 结束跟随, 交回 AI 状态机开始正常战斗
            if (IsShowFollowPlayer)
            {
                IsShowFollowPlayer = false;
            }

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


            if (_friendState != FriendState.NoFriend)
            {
                CheckFriendMove();
            }
            //屏蔽原本ai逻辑
            if (IsOnShowAction || (_friendState == FriendState.Follow))
            {

                ForceFollowTargetUpdate();
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

        /// <summary>
        /// 演出里切换到"跟随玩家": 目标切回玩家, 并把移动模式切成寻路模式。
        /// <para>演出里这一行会一直挂着(见 <see cref="EnemyShowAction"/>), 期间由
        /// <see cref="ForceFollowTargetUpdate"/> 逐帧追, AI 状态机不更新, 所以不会开始攻击;
        /// 追进 <see cref="TargetStopDistance"/> 后跟随结束(留空时), 交回 AI 由
        /// <see cref="PreAtkState"/> → <see cref="AtkState"/> 开始攻击循环。</para>
        /// </summary>
        public void FollowPlayer()
        {
            targetRole = GameAllData.CommonData.Player0;
            //触发即进入寻路模式
            MoveMode = EnemyMoveMode.Path;
            IsShowFollowPlayer = true;
            ResetPath();
        }

        public enum TargetPosType
        {
            Default,
            Transform,
            Point,
            Stop
        }

        public TargetPosType TargetPosTypeValue { get; set; }
        public Vector3 TargetPos { get; set; }
        public Transform TargetTf { get; set; }

        public float MoveSpeedShowAction { get; set; } = 1f;

        //目标点的停止距离
        public float TargetStopDistance { get; set; } = 3;

        public Vector3 GetTargetPos()
        {
            //演出里用 SetTargetMove / FindFunction 指了明确目标点时, 优先级高于"跟着玩家走",
            //否则支援角色接到"去操作炮台"的指令后还是会朝玩家走
            if (_friendState == FriendState.Follow &&
                (TargetPosTypeValue == TargetPosType.Default || TargetPosTypeValue == TargetPosType.Stop))
            {
                return _friend.transform.position + _friend.transform.forward;
            }

            switch (TargetPosTypeValue)
            {
                case TargetPosType.Default:
                    if (targetRole != null)
                    {
                        return targetRole.transform.position;
                    }

                    break;
                case TargetPosType.Point:
                    return TargetPos;
                case TargetPosType.Transform:
                    if (TargetTf)
                    {
                        return TargetTf.position;
                    }

                    break;
                default:
                    return TargetPos;
            }

            return TargetPos;
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
                    if (IsFriendRole)
                    {
                        OnSearchTarget(_friend.data_R.lastEnemy);
                    }
                    else
                    {
                        OnSearchTarget(null);
                    }
                }
            }

            if (targetRole != null && !targetRole.IsDie)
            {
                CheckDistancePoint(targetRole.transform.position);
            }
        }


        private void OnSearchTarget(Role last)
        {
            searchTimer = 0;

            float seeR = mainDataFSM.setting.seeR;

            float seeAngle = mainDataFSM.setting.seeAngle;

            var findRole =
                RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, seeR, seeAngle, out float maxS, owner.team);

            if (findRole == null)
            {
                //如果没找到, 则保持原来(已死亡的目标不再保留)
                findRole = last != null && !last.IsDie ? last : null;
            }

            targetRole = findRole;
        }

        #endregion

        #region 目标死亡监听

        /// <summary>
        /// 敌人死亡事件: 死亡的正是当前目标时立即重新索敌
        /// </summary>
        private void OnEnemyDeadEvent(int deadId)
        {
            if (targetRole == null || targetRole.id != deadId)
            {
                return;
            }

            ReSearchTarget();
        }

        /// <summary>
        /// 立即重新索敌: 先清空已死亡的目标, 再按当前身份重新搜索一次
        /// </summary>
        private void ReSearchTarget()
        {
            targetRole = null;
            searchTimer = 0;

            //友军优先接着打玩家长时间攻击的那个敌人
            Role last = null;
            if (IsFriendRole && _friend != null)
            {
                Role lastEnemy = _friend.data_R.lastEnemy;
                if (lastEnemy != null && !lastEnemy.IsDie)
                {
                    last = lastEnemy;
                }
            }

            OnSearchTarget(last);
        }

        #endregion


        #region Move

        /// <summary>
        /// 朝目标移动。默认模式直接朝目标走; 寻路模式沿通道连线点绕开墙体, 路径不可达时停下。
        /// </summary>
        public void Move(float speedRate = 1)
        {
            Vector3 dir;
            if (MoveMode == EnemyMoveMode.Path && targetRole != null)
            {
                if (!TryGetPathDir(out dir))
                {
                    MoveStop();
                    return;
                }
            }
            else
            {
                dir = targetRole == null
                    ? transform.forward
                    : (targetRole.transform.position - transform.position).normalized;
            }

            if (speedRate < 0)
            {
                dir = -dir;
            }

            owner.AIMoveVector(dir, speedRate, false);
        }

        /// <summary>
        /// 取寻路模式下一步的移动方向(平面上指向当前路径点)。
        /// 返回 false 表示当前不可达, 调用方应该停下而不是朝墙里走。
        /// </summary>
        private bool TryGetPathDir(out Vector3 dir)
        {
            Vector3 targetPos = targetRole.transform.position;

            _pathRefreshTimer -= Time.deltaTime;
            bool needRefresh = !_pathQueried
                || _pathRefreshTimer <= 0f
                || (targetPos - _pathQueryTargetPos).sqrMagnitude > PathTargetMoveDistance * PathTargetMoveDistance;
            if (needRefresh)
            {
                RefreshPath(targetPos);
            }

            if (_pathResult == PassPathResult.Unreachable || _pathPoints.Count == 0)
            {
                dir = Vector3.zero;
                return false;
            }

            // 走到当前路径点就换下一个
            while (_pathIndex < _pathPoints.Count - 1 &&
                   (_pathPoints[_pathIndex] - transform.position).ToY0().sqrMagnitude <= PathReachDistance * PathReachDistance)
            {
                _pathIndex++;
            }

            Vector3 moveDir = (_pathPoints[_pathIndex] - transform.position).ToY0();
            if (moveDir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.zero;
                return false;
            }

            dir = moveDir.normalized;
            return true;
        }

        /// <summary>
        /// 按当前目标位置重算一次路径(不可达时路径为空)。
        /// <para>重算后要接着原来的进度走: 路径是"从现在的位置重新接入通道图"算出来的,
        /// A* 会给离自己最近的那个通道点最低代价 —— 那个点很可能就是刚从它旁边走过、还在身后的点。
        /// 每次都把下标清 0 的话, 敌人会掉头走回刚才那个点, 到下一个点又重算, 表现成围着通道点来回转圈。</para>
        /// </summary>
        private void RefreshPath(Vector3 targetPos)
        {
            // 记住正在追的那个点(必须在 Query 之前取: Query 会先把 _pathPoints 清空重填)
            bool hasProgress = _pathPoints.Count > 0 && _pathIndex < _pathPoints.Count;
            Vector3 curTarget = hasProgress ? _pathPoints[_pathIndex] : targetPos;

            _pathQueried = true;
            _pathRefreshTimer = PathRefreshInterval;
            _pathQueryTargetPos = targetPos;
            _pathResult = PassPathFinder.Query(transform.position, targetPos, mainDataFSM.pathFarDistance, _pathPoints);

            _pathIndex = 0;
            if (!hasProgress || _pathPoints.Count == 0)
            {
                return;
            }

            // 在新路径里找回同一个点: 找到就接着走, 找不到说明路线变了, 从头开始
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _pathPoints.Count; i++)
            {
                float sqr = (_pathPoints[i] - curTarget).ToY0().sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            if (best >= 0 && bestSqr <= PathProgressMatchDistance * PathProgressMatchDistance)
            {
                _pathIndex = best;
            }
        }

        /// <summary>丢掉缓存的路径, 下一次移动立刻重新查询(换目标 / 换移动模式时调用)</summary>
        public void ResetPath()
        {
            _pathQueried = false;
            _pathRefreshTimer = 0f;
            _pathPoints.Clear();
            _pathIndex = 0;
            _pathResult = PassPathResult.Direct;
        }

        public void MoveStop()
        {
            owner.AIMoveVector(Vector3.zero, 0);
        }

        public void MoveSpeedDown(float time)
        {
            owner.Movement.speedDownTime = time;
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
                float sin = -1 * Mathf.Sin(deltaAngle * Mathf.Deg2Rad);
                ;
                owner.AIMoveVector(dir * sin, 1, false);
            }
            else
            {
                Data_R.movement.RotateByMoveDir(dir, 0.08f);
            }
        }

        public void CheckDistance()
        {
            tempTargetDis = Vector3.Distance(transform.position, GetTargetPos());
        }

        public void CheckDistancePoint(Vector3 pos)
        {
            tempTargetDis = Vector3.Distance(transform.position, pos);
        }

        private float GetDistance(Transform tf)
        {
            return Vector3.Distance(transform.position, tf.position);
        }

        #endregion

        #region Distance Check And Move Logic

        private void ForceFollowTargetUpdate()
        {
            //演出里执行过 FollowPlayer: 目标就是玩家, 按当前移动模式追过去。
            //(演出里另有 SetTargetMove 指定了 Point/Transform 时让它优先, 不跟它抢)
            if (IsShowFollowPlayer &&
                (TargetPosTypeValue == TargetPosType.Stop || TargetPosTypeValue == TargetPosType.Default))
            {
                UpdateShowFollowPlayer();
                return;
            }

            bool isMove = TargetPosTypeValue != TargetPosType.Stop || _friendState == FriendState.Follow;
            if (TargetPosTypeValue == TargetPosType.Default)
            {
                isMove = false;
                if (owner.RoleIdentityType == RoleIdentityType.PlayerFriend)
                {
                    _friendState = FriendState.Idle;
                }
            }
            
            if (isMove)
            {
                Vector3 targetPos = GetTargetPos();
                CheckDistancePoint(targetPos);

                if (tempTargetDis >= TargetStopDistance)
                {
                    //移速处理
                    owner.AIMoveTo(targetPos, MoveSpeedShowAction, MoveSpeedShowAction);
                }
                else
                {
                    //到达则停止
                    TargetPosTypeValue = TargetPosType.Stop;
                    if (_friendState == FriendState.Follow)
                    {
                        _friendState = FriendState.Idle;
                    }
                }
            }
            else
            {
                MoveStop();
            }
        }


        /// <summary>
        /// 演出里的"跟随玩家"(FollowPlayer 之后接管): 寻路模式下沿通道连线点绕开墙体, 默认模式下直接朝玩家走。
        /// 玩家为空或已死亡就停下, 别让演出里的敌人顶着墙空走。
        /// </summary>
        private void UpdateShowFollowPlayer()
        {
            if (targetRole == null || targetRole.IsDie)
            {
                MoveStop();
                return;
            }

            Vector3 targetPos = targetRole.transform.position;
            CheckDistancePoint(targetPos);

            if (tempTargetDis <= TargetStopDistance)
            {
                MoveStop();
                return;
            }

            if (MoveMode == EnemyMoveMode.Path)
            {
                if (!TryGetPathDir(out Vector3 dir))
                {
                    //不可达: 停下等下一次重算, 而不是朝墙里走
                    MoveStop();
                    return;
                }

                //和 AIMoveTo 同一套写法: 方向长度带移速, 第二个参数是动画移速倍率
                owner.AIMoveVector(dir * MoveSpeedShowAction, MoveSpeedShowAction, false);
                return;
            }

            owner.AIMoveTo(targetPos, MoveSpeedShowAction, MoveSpeedShowAction);
        }


        private float nearFriendDistance = 6;

        /// <summary>
        /// 每隔0.5s检测一次与玩家的距离,
        /// 当周围没有敌人时,则向玩家移动
        /// 如果距离大于10,则移动到玩家附近,
        /// 如果距离大于20,则传送到玩家
        /// </summary>
        private void CheckFriendMove()
        {
            //演出(如 FindFunction 去操作功能件)期间移动由 ShowAct 接管, 不让跟随逻辑抢目标点;
            //关掉开关后支援角色也不会再自动回到/传送回玩家身边
            if (IsOnShowAction || !FriendKeepWithPlayer)
            {
                return;
            }

            checkDistanceTimer += Time.deltaTime;
            if (checkDistanceTimer >= checkDistanceInterval)
            {
                checkDistanceTimer = 0;

                float distanceToPlayer = Vector3.Distance(transform.position, _friend.transform.position);

                //  周围有敌人则不执行
                if (targetRole != null)
                {
                    _friendState = FriendState.Idle;
                    return;
                }

                idlePos = _friend.transform.position + _friend.transform.forward;
                // 距离大于20，则传送到玩家身边
                if (distanceToPlayer > 20f)
                {
                    // 传送到玩家身边（稍微偏移一点避免完全重合）
                    Vector3 teleportPosition = _friend.transform.position - _friend.transform.forward * 2f;
                    owner.Movement.MoveToImmediate(teleportPosition);
                }
                // 距离大于nearFriendDistance，则移动到玩家附近
                else if (distanceToPlayer > nearFriendDistance)
                {
                    // 继续执行原有的寻路和移动逻辑
                    _friendState = FriendState.Follow;
                    TargetStopDistance = 2f;
                }

                //只接管还活着的目标, 已死亡的交给死亡回调去重新索敌
                Role lastEnemy = _friend.data_R.lastEnemy;
                if (lastEnemy != null && !lastEnemy.IsDie)
                {
                    targetRole = lastEnemy;
                }
            }
        }

        #endregion

        public void SetFriend(Player0 localPlayer)
        {
            _friend = localPlayer;
            _friendState = FriendState.Follow;
            owner.RoleIdentityType = RoleIdentityType.PlayerFriend;
            localPlayer.playerData.AddFriend(owner);
            //友军只在转身份时订阅一次敌人死亡事件, 之后换目标无需重绑
            if (!HasAddEnemyDeadEvent)
            {
                HasAddEnemyDeadEvent = true;
                GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
            }

            GameEvent.Send<int>(EGameEvent.AddFriend.ToInt(), owner.id);
        }
    }
}