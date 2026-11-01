using System.Collections.Generic;
using UnityEngine;
using XiaoCao.UI;
using XiaoCaoKit.Runtime.Demo.Item;

namespace XiaoCao
{
    /// <summary>
    /// 炮台飞行物类型: 子弹-直线; 导弹-抛物线; 激光
    /// </summary>
    public enum ETurretProjectileType
    {
        Bullet = 0,
        Missile = 1,
        Laser = 2,
    }

    /// <summary>
    /// 炮台操控者类型
    /// </summary>
    public enum ETurretOperatorType
    {
        None = 0,
        Player = 1, //玩家亲自操控
        AiRole = 2, //非玩家角色(支援角色/敌人)操控
    }

    /// <summary>
    /// 关卡炮台: 固定功能建筑, 炮头可偏航(-60~60)与俯仰(-30~30), 有攻击冷缩与攻击范围;
    /// 同一时间只能被一个角色操控。玩家靠近按攻击键弹出交互界面,
    /// 玩家操控时切换为第一人称准心视角(隐藏角色与炮台模型, 准心方向即射击方向, 开火有后坐力抖动),
    /// 非玩家角色(支援角色/敌人)操控时自动索敌, 并按"按住攻击 → 停顿"的节奏循环开火。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class Turret : MonoBehaviour
    {
        /// <summary>炮口与目标方向的夹角小于该值才算瞄准到位(度)</summary>
        private const float AimReadyAngle = 8f;

        /// <summary>后坐力抖动的噪声采样频率</summary>
        private const float RecoilNoiseSpeed = 45f;

        /// <summary>鼠标轴死区, 小于该值视为未移动</summary>
        private const float MouseDeadZone = 0.001f;

        /// <summary>瞄准射线起点被挡住时, 每次沿射向前移的距离(米)</summary>
        private const float AimRayAdvanceStep = 1f;

        /// <summary>起点前移重测的最大次数</summary>
        private const int AimRayAdvanceCount = 3;

        /// <summary>落点离起点小于该距离(米)就认为起点被碰撞体包住了</summary>
        private const float AimRayTooNear = 0.2f;

        #region Inspector

        [Header("节点绑定")]
        [Tooltip("炮头, 做偏航与俯仰转动")]
        public Transform head;

        [Tooltip("炮口, 飞行物出生点与视野基准点")]
        public Transform muzzle;

        [Tooltip("操控者站位(炮口后方)")]
        public Transform standPoint;

        [Header("交互")]
        [Tooltip("玩家进入该半径内按攻击键即可交互")]
        public float interactRadius = 2.5f;

        [Header("攻击")]
        [Tooltip("攻击配置, 子弹/导弹/激光各一份 ScriptableObject; 决定射程/冷缩/弹道/检测层")]
        public TurretAttackConfig attackConfig;

        [Header("头部转向")]
        [Tooltip("头部偏航上限(度), 设计为 -60~60")]
        public float headYawLimit = 60f;

        [Tooltip("头部上仰上限(度)")]
        public float headPitchMax = 30f;

        [Tooltip("头部下俯下限(度), 填负数")]
        public float headPitchMin = -30f;

        [Tooltip("头部转向速度(度/秒)")]
        public float headRotateSpeed = 180f;

        [Tooltip("玩家操控时方向键的转向速度(度/秒)")]
        public float playerAimSpeed = 90f;

        [Header("第一人称瞄准视角")]
        [Tooltip("操控时隐藏炮台自身的模型")]
        public bool hideTurretModel = true;

        [Tooltip("操控时隐藏操控者的模型")]
        public bool hideOperatorModel = true;

        [Tooltip("瞄准时关闭炮台自身的碰撞体: 视角在炮口后上方, 个别角度准星射线仍可能扫到炮台本体, 关掉后射线彻底穿过去")]
        public bool disableAimSelfCollider = true;

        [Tooltip("视角在炮口上方的高度(米), 相当于第一人称的眼睛位置")]
        public float cameraUpOffset = 0.4f;

        [Tooltip("视角相对炮口的后移距离(米), 0 表示正好在炮口")]
        public float cameraBackOffset = 0f;

        [Tooltip("炮台视角焦距(FOV), 越小看得越远")]
        public float aimFieldOfView = 20f;

        [Tooltip("炮台虚拟相机的优先级, 要高于主相机(10)才能接管画面")]
        public int aimCameraPriority = 20;

        [Tooltip("拖动屏幕转视角的灵敏度(度/像素)")]
        public float dragAimSensitivity = 0.25f;

        [Tooltip("拖动屏幕时上下方向取反(想和主相机手感一致时打开)")]
        public bool invertDragPitch = false;

        [Tooltip("俯仰灵敏度相对横向的比例, 0.5 表示上下只有左右的一半")]
        public float dragPitchScale = 0.5f;

        [Tooltip("PC 端鼠标移动转视角的灵敏度(度/鼠标轴单位, 约等于 0.3 度/像素); PC 下无需拖拽, 移动鼠标即转视角")]
        public float mouseAimSensitivity = 3f;

        [Tooltip("PC 端操控时锁定并隐藏光标(更接近 FPS 手感; 锁定后无法用鼠标点界面按钮, 需用 Esc 退出)")]
        public bool lockCursorOnAim = false;

        [Header("后坐力")]
        [Tooltip("每次开火炮口上跳的角度(度)")]
        public float recoilKick = 1.5f;

        [Tooltip("上跳角度的回落速度(度/秒)")]
        public float recoilRecoverSpeed = 10f;

        [Tooltip("开火瞬间视角抖动的幅度(度)")]
        public float recoilShakeAngle = 0.5f;

        [Tooltip("视角抖动持续时间(秒)")]
        public float recoilShakeTime = 0.12f;

        [Header("激光")]
        [Tooltip("激光组件; 只有激光配置会用到, 因为组件引用不能存进 ScriptableObject")]
        public TurretLaser laser;

        [Header("瞄准线")]
        [Tooltip("玩家操控时从炮口连到准星落点的辅助线; 为空则不画")]
        public TurretAimLine aimLine;

        [Header("调试")]
        [Tooltip("开火时把准星射线检测到的物体打印到 Console, 排查打不中的问题用")]
        public bool logAimRay = true;

        [Header("互动提示")]
        [Tooltip("玩家靠近时显示的世界名字牌(本地化 key), 留空不显示")]
        public string interactTipKey = "TurretTitle";

        [Tooltip("名字牌相对炮台根节点的高度")]
        public float interactTipOffsetY = 1.8f;

        #endregion

        #region RuntimeData

        /// <summary>当前操控者, 为空表示空闲</summary>
        public Role OperatorRole { get; private set; }

        /// <summary>操控者类型</summary>
        public ETurretOperatorType OperatorType { get; private set; } = ETurretOperatorType.None;

        public bool HasOperator => OperatorRole != null;

        /// <summary>射击方向(含偏航/俯仰/后坐力), 与准心完全一致</summary>
        public Vector3 AimDirection => AimRotation * Vector3.forward;

        /// <summary>准星/发射检测用的层, 由 <see cref="TurretAttackConfig.AimRayMask"/> 决定</summary>
        public int AimRayMask => attackConfig != null ? attackConfig.AimRayMask : 0;

        /// <summary>炮口朝向(含偏航/俯仰/后坐力), 飞行物用它出生</summary>
        public Quaternion AimRotation =>
            transform.rotation * Quaternion.Euler(-(curPitch + recoilOffset), curYaw, 0f);

        private Quaternion headBaseRot;
        private float curYaw;
        private float targetYaw;
        private float curPitch;
        private float targetPitch;
        private float recoilOffset;
        private float recoilShakeTimer;
        private float cdTimer;

        /// <summary>支援角色当前是否处于"按住攻击"阶段</summary>
        private bool aiFiring;

        /// <summary>当前开火阶段(按住/松开)的剩余时间</summary>
        private float aiTimer;
        private Role aiTarget;
        private HomeTriggerNameText tipHelper;
        private bool isTipShowing;

        /// <summary>名字牌是否已被"进入操控"整体隐藏, 只在状态切换时改 SetActive</summary>
        private bool isTipHiddenByControl;

        /// <summary>名字牌朝向用的相机, 首次使用时取主相机并缓存</summary>
        private Camera tipFaceCamera;
        private AssetPool bulletPool;
        private AssetPool missilePool;

        private readonly TurretAimCamera aimCamera = new TurretAimCamera();
        private readonly List<Renderer> turretRenderers = new List<Renderer>();
        private readonly List<Renderer> operatorRenderers = new List<Renderer>();
        private readonly List<RaycastHit> aimHits = new List<RaycastHit>();

        /// <summary>RaycastNonAlloc 的固定缓冲, 避免瞄准线每帧检测时产生 GC</summary>
        private readonly RaycastHit[] aimHitBuffer = new RaycastHit[32];

        /// <summary>最近一次准星检测是否命中了敌对角色, 给瞄准线的落点标记用</summary>
        private bool aimHitTarget;

        /// <summary>瞄准期间被临时关掉的自身碰撞体, 退出时按原状态恢复</summary>
        private readonly List<Collider> aimDisabledColliders = new List<Collider>();

        private CursorLockMode savedCursorLock;
        private bool savedCursorVisible;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (attackConfig == null)
            {
                Debug.LogError($"[Turret] {name} 没有配置 attackConfig(炮台攻击配置), 炮台不工作", this);
                enabled = false;
                return;
            }

            //记录炮头的初始朝向, 偏航与俯仰都在它之上叠加
            headBaseRot = head.localRotation;
            CacheTurretRenderers();
        }

        private void OnEnable()
        {
            TurretSystem.Register(this);
        }

        private void OnDisable()
        {
            TurretSystem.Unregister(this);
            ExitControl();

            //兜底: 非玩家操控分支不会走 ExitAimView, 这里保证碰撞体一定被恢复
            SetAimSelfCollidersDisabled(false);
        }

        private void Update()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
            {
                return;
            }

            float dt = XCTime.deltaTime;

            ReportLocalPlayerNear();
            UpdateInteractTip();
            UpdateRecoil(dt);

            if (OperatorRole == null)
            {
                //无人操控时炮口缓慢回正
                targetYaw = 0f;
                targetPitch = 0f;
                ApplyHeadAim(dt);
                return;
            }

            if (OperatorRole.IsDie)
            {
                ExitControl();
                return;
            }

            //Esc 与顶部 CloseBtn 等价, 都是离开炮台; 有面板在显示时 Esc 先交给 UI 关面板
            if (OperatorType == ETurretOperatorType.Player &&
                Input.GetKeyDown(KeyCode.Escape) &&
                UIMgr.HasInst && UIMgr.Inst.showingPanels.Count == 0)
            {
                ExitControl();
                return;
            }

            if (OperatorType == ETurretOperatorType.Player)
            {
                ApplyMouseAim();
            }
            else if (OperatorType == ETurretOperatorType.AiRole)
            {
                UpdateAiRoleOperator(dt);
            }

            ApplyHeadAim(dt);
            cdTimer -= dt;
        }

        /// <summary>
        /// 第一人称视角在 LateUpdate 写进虚拟相机, 早于 CinemachineBrain 采样,
        /// 保证画面用的是本帧最终的炮口朝向(含后坐力抖动)
        /// </summary>
        private void LateUpdate()
        {
            if (!aimCamera.IsOn)
            {
                return;
            }

            aimCamera.Apply(GetAimCameraPos(), AimRotation * GetRecoilShakeRotation());

            if (aimLine != null)
            {
                //落点和开火用的是同一套检测, 所以线头正好压在准星瞄准的位置
                aimLine.SetLine(muzzle.position, GetCrosshairAimPoint(), aimHitTarget);
            }
        }

        /// <summary>视角位置: 炮口上方一段距离, 朝向由 <see cref="AimRotation"/> 决定</summary>
        private Vector3 GetAimCameraPos()
        {
            return muzzle.position +
                   Vector3.up * cameraUpOffset -
                   AimRotation * Vector3.forward * cameraBackOffset;
        }

        #endregion

        #region Operator

        /// <summary>
        /// 玩家能否与该炮台交互: 自己已在操控时不再需要交互,
        /// 被支援角色占用时仍可交互(界面里可以终止它的操控)
        /// </summary>
        public bool CanInteract(Role role)
        {
            return OperatorRole != role;
        }

        /// <summary>玩家靠近按攻击键时打开炮台交互界面</summary>
        public void OpenInteractionPanel()
        {
            UIMgr.Inst.ShowView(UIPanelType.TurretPanel, new TurretPanelData(this));
        }

        /// <summary>玩家亲自操控炮台</summary>
        public bool TakeControlByPlayer(Player0 player)
        {
            if (OperatorRole == player)
            {
                return true;
            }

            ExitControl();
            EnterControl(player, ETurretOperatorType.Player);
            return true;
        }

        /// <summary>指派非玩家角色(支援角色/敌人)操控炮台, 已死亡时返回 false</summary>
        public bool TakeControlByAiRole(Role role)
        {
            if (role == null || role.IsDie || role.IsPlayer)
            {
                return false;
            }

            if (OperatorRole == role)
            {
                return true;
            }

            ExitControl();
            EnterControl(role, ETurretOperatorType.AiRole);
            return true;
        }

        /// <summary>终止当前操控, 角色恢复自由行动</summary>
        public void ExitControl()
        {
            if (OperatorRole == null)
            {
                return;
            }

            Role role = OperatorRole;
            ETurretOperatorType type = OperatorType;
            OperatorRole = null;
            OperatorType = ETurretOperatorType.None;
            aiTarget = null;
            targetYaw = 0f;
            targetPitch = 0f;
            recoilOffset = 0f;
            recoilShakeTimer = 0f;

            if (type == ETurretOperatorType.Player)
            {
                ExitAimView();
                TurretSystem.SetPlayerOperating(null);

                if (UIMgr.HasInst)
                {
                    UIMgr.Inst.keyInputHud.SetTurretControlMode(false);
                    UIMgr.Inst.battleHud.SetTurretControlMode(false);
                }
            }

            UnlockRole(role);
        }

        private void EnterControl(Role role, ETurretOperatorType type)
        {
            OperatorRole = role;
            OperatorType = type;
            curYaw = 0f;
            targetYaw = 0f;
            curPitch = 0f;
            targetPitch = 0f;
            recoilOffset = 0f;
            recoilShakeTimer = 0f;
            cdTimer = 0f;
            aiFiring = true;
            aiTimer = AiHoldDuration;
            aiTarget = null;

            LockRole(role);

            if (type == ETurretOperatorType.Player)
            {
                TurretSystem.SetPlayerOperating(this);
                EnterAimView();

                if (UIMgr.HasInst)
                {
                    UIMgr.Inst.keyInputHud.SetTurretControlMode(true);
                    UIMgr.Inst.battleHud.SetTurretControlMode(true);
                }
            }
        }

        /// <summary>操控开始时把角色固定到炮口后方, 期间不能再移动/施法</summary>
        private void LockRole(Role role)
        {
            role.data_R.roleControl.BreakAllBusy();
            role.data_R.roleState.moveLockFlag = true;
            role.Movement.MoveToImmediate(standPoint.position);
            role.Movement.LookToDir(transform.forward);
            role.Anim.CrossFade(AnimNames.Idle, 0.2f);

            //支援角色由 AIControl 驱动, 操控炮台时关掉 AI, 不再自己索敌/移动
            if (role is Enemy0 enemy)
            {
                enemy.IsAiOn = false;
                enemy.AiControl.MoveStop();
            }
        }

        private void UnlockRole(Role role)
        {
            role.data_R.roleState.moveLockFlag = false;

            if (role is Enemy0 enemy && !enemy.IsDie)
            {
                enemy.IsAiOn = true;
            }
        }

        #endregion

        #region AimView

        /// <summary>
        /// 进入第一人称瞄准视角: 提高炮台虚拟相机的优先级接管画面(过渡见 MainCameraBlends),
        /// 隐藏角色与炮台模型, 显示准星
        /// </summary>
        private void EnterAimView()
        {
            aimCamera.Begin(transform, GetAimCameraPos(), AimRotation, aimFieldOfView, aimCameraPriority);

            if (hideTurretModel)
            {
                SetRenderersEnabled(turretRenderers, false);
            }

            if (hideOperatorModel)
            {
                CacheOperatorRenderers(OperatorRole);
                SetRenderersEnabled(operatorRenderers, false);
            }

            if (UIMgr.HasInst && UIMgr.Inst.turretCrosshair != null)
            {
                UIMgr.Inst.turretCrosshair.Show();
            }

            if (aimLine != null)
            {
                aimLine.Show();
            }

            SetAimSelfCollidersDisabled(true);
            ApplyCursorLock(true);
        }

        /// <summary>退出第一人称视角: 交还相机, 恢复模型与准星</summary>
        private void ExitAimView()
        {
            aimCamera.End();
            SetRenderersEnabled(turretRenderers, true);
            SetRenderersEnabled(operatorRenderers, true);
            operatorRenderers.Clear();

            if (UIMgr.HasInst && UIMgr.Inst.turretCrosshair != null)
            {
                UIMgr.Inst.turretCrosshair.Hide();
            }

            if (aimLine != null)
            {
                aimLine.Hide();
            }

            SetAimSelfCollidersDisabled(false);
            ApplyCursorLock(false);
        }

        /// <summary>缓存炮台自身可隐藏的模型渲染器(激光、拖尾等特效不参与隐藏)</summary>
        private void CacheTurretRenderers()
        {
            turretRenderers.Clear();

            Renderer[] all = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in all)
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    turretRenderers.Add(renderer);
                }
            }
        }

        /// <summary>缓存操控者身上可隐藏的模型渲染器</summary>
        private void CacheOperatorRenderers(Role role)
        {
            operatorRenderers.Clear();

            Renderer[] all = role.transform.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in all)
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    operatorRenderers.Add(renderer);
                }
            }
        }

        private static void SetRenderersEnabled(List<Renderer> renderers, bool isEnabled)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = isEnabled;
                }
            }
        }

        /// <summary>
        /// 瞄准期间关掉炮台自身的碰撞体。射线过滤(<see cref="IsSelfCollider"/>)只能挡当前这几种层级关系,
        /// 实际游戏中个别角度仍会命中本体(碰撞体挂在炮台之外的位置等), 直接关掉物理检测最彻底。
        /// 只动炮台本体, 不碰操控者: 角色碰撞体一关会穿地/掉下去。
        /// </summary>
        private void SetAimSelfCollidersDisabled(bool isDisabled)
        {
            if (!isDisabled)
            {
                for (int i = 0; i < aimDisabledColliders.Count; i++)
                {
                    if (aimDisabledColliders[i] != null)
                    {
                        aimDisabledColliders[i].enabled = true;
                    }
                }

                aimDisabledColliders.Clear();
                return;
            }

            aimDisabledColliders.Clear();

            if (!disableAimSelfCollider)
            {
                return;
            }

            Collider[] all = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in all)
            {
                //本来就没启用的不去碰, 免得退出时把它打开
                if (!col.enabled)
                {
                    continue;
                }

                col.enabled = false;
                aimDisabledColliders.Add(col);
            }
        }

        /// <summary>开火后坐力: 炮口上跳 + 视角抖动 + 准星张开</summary>
        private void ApplyRecoil()
        {
            recoilOffset += recoilKick;
            recoilShakeTimer = recoilShakeTime;

            if (UIMgr.HasInst && UIMgr.Inst.turretCrosshair != null)
            {
                UIMgr.Inst.turretCrosshair.PlayFirePulse();
            }
        }

        private void UpdateRecoil(float dt)
        {
            if (recoilOffset > 0f)
            {
                recoilOffset = Mathf.MoveTowards(recoilOffset, 0f, recoilRecoverSpeed * dt);
            }

            if (recoilShakeTimer > 0f)
            {
                recoilShakeTimer -= dt;
            }
        }

        /// <summary>用噪声做开火瞬间的视角抖动, 幅度随时间线性衰减</summary>
        private Quaternion GetRecoilShakeRotation()
        {
            if (recoilShakeTimer <= 0f)
            {
                return Quaternion.identity;
            }

            float weight = recoilShakeTimer / Mathf.Max(0.01f, recoilShakeTime);
            float noiseTime = Time.time * RecoilNoiseSpeed;
            float pitchShake = (Mathf.PerlinNoise(noiseTime, 0.37f) - 0.5f) * 2f;
            float yawShake = (Mathf.PerlinNoise(0.71f, noiseTime) - 0.5f) * 2f;

            return Quaternion.Euler(pitchShake * recoilShakeAngle * weight,
                yawShake * recoilShakeAngle * weight, 0f);
        }

        #endregion

        /// <summary>当前是否为 PC 鼠标模式(该模式下视角自动跟随鼠标, 屏幕拖拽无效)</summary>
        private static bool IsMouseMode => GameSetting.UserInputType == UserInputType.Mouse;

        /// <summary>
        /// PC 端: 鼠标移动即转炮口, 无需按住/拖拽。
        /// 在 Update(渲染帧)读取鼠标增量, 避免 FixedUpdate 一帧多跑或零跑导致丢帧或重复
        /// </summary>
        private void ApplyMouseAim()
        {
            if (!IsMouseMode)
            {
                return;
            }

            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            if (Mathf.Abs(mx) < MouseDeadZone && Mathf.Abs(my) < MouseDeadZone)
            {
                return;
            }

            targetYaw = Mathf.Clamp(targetYaw + mx * mouseAimSensitivity,
                -headYawLimit, headYawLimit);

            //鼠标上移 my 为正 = 抬头, 与拖拽的"手指向上=抬头"方向一致
            float pitchDelta = my * mouseAimSensitivity * dragPitchScale *
                               (invertDragPitch ? -1f : 1f);
            targetPitch = Mathf.Clamp(targetPitch + pitchDelta, headPitchMin, headPitchMax);
        }

        /// <summary>PC 端操控时可选锁定光标, 退出时恢复进入前的状态</summary>
        private void ApplyCursorLock(bool enter)
        {
            if (!IsMouseMode || !lockCursorOnAim)
            {
                return;
            }

            if (enter)
            {
                savedCursorLock = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = savedCursorLock;
                Cursor.visible = savedCursorVisible;
            }
        }

        #region PlayerOperator

        /// <summary>
        /// 玩家操控时的输入处理, 由 <see cref="TurretSystem.TryConsumePlayerInput"/> 在
        /// PlayerControl 的 FixedUpdate 中调用, 此时其他操作输入都被吞掉
        /// </summary>
        public void OnPlayerInput(Player0 player)
        {
            PlayerInputData input = player.playerData.inputData;

            //拖动屏幕转炮口: 仅移动端生效; PC 端是"鼠标自动跟随"(见 ApplyMouseAim), 拖拽无效
            Vector2 swipe = IsMouseMode ? Vector2.zero : PlayerInputData.LocalSwipeDirection;
            if (!swipe.IsZore())
            {
                targetYaw = Mathf.Clamp(targetYaw + swipe.x * dragAimSensitivity,
                    -headYawLimit, headYawLimit);

                //GetSwipeDirection 的 y 是"手指下移为正", 取负号才是手指向上=抬头(invertDragPitch 时反向)
                float pitchDelta = -swipe.y * dragAimSensitivity * dragPitchScale *
                                   (invertDragPitch ? -1f : 1f);
                targetPitch = Mathf.Clamp(targetPitch + pitchDelta, headPitchMin, headPitchMax);
            }

            //左右输入控制炮口偏航, 受 -60~60 限制
            if (Mathf.Abs(input.X) > 0.1f)
            {
                targetYaw = Mathf.Clamp(targetYaw + input.X * playerAimSpeed * Time.fixedDeltaTime,
                    -headYawLimit, headYawLimit);
            }

            //上下输入控制炮口俯仰, 受 -30~30 限制
            if (Mathf.Abs(input.Y) > 0.1f)
            {
                targetPitch = Mathf.Clamp(targetPitch + input.Y * playerAimSpeed * Time.fixedDeltaTime,
                    headPitchMin, headPitchMax);
            }

            //点一下开一炮, 长按(鼠标左键/J/普攻按钮)则按攻击冷缩自动连发
            if (input.inputs[(int)InputKey.NorAck] || input.norAckHold)
            {
                TryFire();
            }
        }

        #endregion

        #region AiRoleOperator

        /// <summary>
        /// 当前操控者是否玩家阵营(玩家自己/支援角色)。
        /// 敌人操控时走 <see cref="TurretAutoFireConfig.enemyHoldDuration"/> 那套更慢的节奏
        /// </summary>
        private bool IsFriendlyOperator => OperatorRole != null && OperatorRole.team == XCSetting.PlayerTeam;

        /// <summary>当前操控者对应的"按住攻击"时长(秒)</summary>
        private float AiHoldDuration
        {
            get
            {
                TurretAutoFireConfig cfg = attackConfig.AutoFire;
                return Mathf.Max(0.01f,
                    IsFriendlyOperator ? cfg.friendlyHoldDuration : cfg.enemyHoldDuration);
            }
        }

        /// <summary>当前操控者对应的"松开/停顿"时长(秒)</summary>
        private float AiReleaseDuration
        {
            get
            {
                TurretAutoFireConfig cfg = attackConfig.AutoFire;
                return Mathf.Max(0f,
                    IsFriendlyOperator ? cfg.friendlyReleaseDuration : cfg.enemyReleaseDuration);
            }
        }

        /// <summary>
        /// 非玩家角色操控: 自动索敌转向, 开火节奏模拟"按住攻击一段 → 松开一段"的循环,
        /// 按住期间按攻击冷缩连发, 松开期间完全不开火。
        /// 节奏按操控者阵营取(见 <see cref="TurretAutoFireConfig"/>):
        /// 支援角色用 friendlyHoldDuration/friendlyReleaseDuration, 敌人用 enemyHoldDuration/enemyReleaseDuration。
        /// 打不了的时候(没敌人/炮口没转到位)把节奏重置到按住阶段的起点,
        /// 这样目标一出现或炮口刚转到位就能立刻开始输出, 不会白等一个松开间隔
        /// </summary>
        private void UpdateAiRoleOperator(float dt)
        {
            float hold = AiHoldDuration;
            float release = AiReleaseDuration;

            aiTarget = FindAimTarget();
            if (aiTarget == null)
            {
                //没目标时炮口回正
                targetYaw = 0f;
                targetPitch = 0f;
            }
            else
            {
                Vector3 aimPos = GetAiAimPos();
                targetYaw = Mathf.Clamp(GetLocalYaw(aimPos), -headYawLimit, headYawLimit);
                targetPitch = Mathf.Clamp(GetLocalPitch(aimPos), headPitchMin, headPitchMax);
            }

            if (!CanAimAtTarget())
            {
                aiFiring = true;
                aiTimer = hold;
                return;
            }

            //阶段计时只在真能开火的时候走
            aiTimer -= dt;
            if (aiTimer <= 0f)
            {
                aiFiring = !aiFiring;
                aiTimer = aiFiring ? hold : release;
            }

            if (aiFiring)
            {
                //连发间隔仍由 attackCooldown 控制, 与玩家长按行为一致
                TryFire();
            }
        }

        /// <summary>
        /// 在索敌范围内找一个敌方角色, 沿用现有的索敌评分。
        /// 范围由自动攻击配置的 searchRange/searchAngle 决定(默认 80 米 80 度),
        /// 角度会按炮头偏航上限收窄, 否则会选中根本转不到的目标导致一直不开火
        /// </summary>
        private Role FindAimTarget()
        {
            float range = attackConfig.AutoFire.searchRange;
            if (aiTarget != null && !aiTarget.IsDie &&
                Vector3.Distance(transform.position, aiTarget.transform.position) <= range)
            {
                return aiTarget;
            }

            float angle = Mathf.Min(attackConfig.AutoFire.searchAngle, headYawLimit);
            return RoleMgr.Inst.SearchEnemyRole(transform, range, angle, out _, OperatorRole.team);
        }

        private Vector3 GetAiAimPos()
        {
            return aiTarget.transform.position + Vector3.up * attackConfig.AutoFire.aimOffsetY;
        }

        /// <summary>
        /// 目标是否在炮头偏航/俯仰范围内并且炮口已经转到位, 转不到或没转完都不开火
        /// </summary>
        private bool CanAimAtTarget()
        {
            if (aiTarget == null || aiTarget.IsDie)
            {
                return false;
            }

            Vector3 aimPos = GetAiAimPos();
            if (Mathf.Abs(GetLocalYaw(aimPos)) > headYawLimit)
            {
                return false;
            }

            float pitch = GetLocalPitch(aimPos);
            if (pitch < headPitchMin || pitch > headPitchMax)
            {
                return false;
            }

            return Mathf.Abs(targetYaw - curYaw) <= AimReadyAngle &&
                   Mathf.Abs(targetPitch - curPitch) <= AimReadyAngle;
        }

        #endregion

        #region Fire

        /// <summary>尝试开火, 冷缩未结束或无人操控时返回 false</summary>
        public bool TryFire()
        {
            if (OperatorRole == null || cdTimer > 0f)
            {
                return false;
            }

            cdTimer = attackConfig.attackCooldown;
            Fire();
            return true;
        }

        private void Fire()
        {
            //玩家操控时以准星射线为准(屏幕正中), 其余情况从炮口自己检测
            //开火这一次带上日志, 把沿途挡住的物体都打出来
            Vector3 aimPoint = OperatorType == ETurretOperatorType.Player
                ? RaycastAimPoint(GetAimCameraPos(), AimDirection, attackConfig.aimDetectDistance, logAimRay)
                : RaycastAimPoint(muzzle.position, AimDirection, attackConfig.AiDetectDistance, logAimRay);

            //实际发射方向 = 炮口指向准星命中的点, 近距离射击也能打中准星位置
            Vector3 fireDir = GetFireDirection(aimPoint);

            switch (attackConfig.ProjectileType)
            {
                case ETurretProjectileType.Bullet:
                    if (attackConfig is TurretBulletConfig bulletConfig)
                    {
                        LaunchProjectile(bulletConfig.bulletPrefab, aimPoint, fireDir, ref bulletPool);
                    }
                    break;
                case ETurretProjectileType.Missile:
                    if (attackConfig is TurretMissileConfig missileConfig)
                    {
                        LaunchProjectile(missileConfig.missilePrefab, aimPoint, fireDir, ref missilePool);
                    }
                    break;
                case ETurretProjectileType.Laser:
                    if (laser != null)
                    {
                        laser.Fire(this, OperatorRole, attackConfig.skillId,
                            OperatorType == ETurretOperatorType.AiRole
                                ? attackConfig.AiDetectDistance
                                : attackConfig.attackRange, fireDir);
                        PlayFireSfx();
                    }
                    break;
            }

            ApplyRecoil();
        }

        private void LaunchProjectile(GameObject prefab, Vector3 aimPoint, Vector3 fireDir, ref AssetPool pool)
        {
            pool ??= PoolMgr.Inst.GetOrCreatPrefabPool(prefab);

            GameObject go = pool.Get();
            TurretProjectile projectile = go.GetComponent<TurretProjectile>();

            //发射音效统一在炮台播(配置在攻击配置里), 飞行物不再另存一份
            PlayFireSfx();

            projectile.Launch(this, OperatorRole, attackConfig.skillId, attackConfig.attackRange,
                aimPoint, fireDir, pool, aiTarget);
        }

        /// <summary>播放攻击配置里的发射音效</summary>
        private void PlayFireSfx()
        {
            if (attackConfig.fireSfx != null)
            {
                SoundMgr.Inst.PlayClip(attackConfig.fireSfx, attackConfig.fireSfxVolume);
            }
        }

        /// <summary>
        /// 准星落点: 从第一人称视角位置沿瞄准方向检测(所以和屏幕正中的准星严格一致),
        /// 命中敌人或场景取命中点, 一路无阻挡则取配置里的 aimDetectDistance 米处
        /// </summary>
        public Vector3 GetCrosshairAimPoint()
        {
            return RaycastAimPoint(GetAimCameraPos(), AimDirection, attackConfig.aimDetectDistance);
        }

        /// <summary>炮口落点: 从炮口沿瞄准方向检测(非玩家操控时使用)</summary>
        public Vector3 GetAimPoint()
        {
            return RaycastAimPoint(muzzle.position, AimDirection, attackConfig.attackRange);
        }

        /// <summary>
        /// 实际发射方向: 炮口指向准星命中的点。
        /// 炮口在准星下方/前方, 近处射击时两者方向不同, 这里按落点反算方向以补齐视差
        /// </summary>
        public Vector3 GetFireDirection(Vector3 aimPoint)
        {
            Vector3 dir = aimPoint - muzzle.position;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return AimDirection;
            }

            dir.Normalize();

            //落点跑到炮口后面(贴脸、打到自身等)时按瞄准方向发射, 否则子弹会往回飞
            return Vector3.Dot(dir, AimDirection) < 0f ? AimDirection : dir;
        }

        /// <summary>
        /// 是否是炮台自身或当前操控者的碰撞体。
        /// 视角挂在炮口后上方(cameraBackOffset > 0 时在炮台背后), 准星射线必然先穿过炮台本体,
        /// 这类碰撞体既不挡准心也不该挡住子弹
        /// </summary>
        public bool IsSelfCollider(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (collider.transform.IsChildOf(transform))
            {
                return true;
            }

            Role operatorRole = OperatorRole;
            return operatorRole != null && operatorRole.transform != null &&
                   collider.transform.IsChildOf(operatorRole.transform);
        }

        /// <summary>
        /// 沿指定起点与方向检测落点, 带"起点被包住"的兜底:
        /// 炮口正好卡在空气墙(LevelAction/Air)或炮台自身碰撞体里时, 射线刚出发就被挡住,
        /// 落点贴在起点上, 这时把起点沿射向前移 <see cref="AimRayAdvanceStep"/> 米重新检测,
        /// 避免瞄准方向被拉回到炮口自己身上
        /// </summary>
        private Vector3 RaycastAimPoint(Vector3 origin, Vector3 dir, float distance, bool log = false)
        {
            Vector3 point = RaycastAimPointOnce(origin, dir, distance, log);

            for (int i = 0; i < AimRayAdvanceCount && IsAimPointTooNear(origin, point); i++)
            {
                float tooNear = Vector3.Distance(origin, point);

                origin += dir * AimRayAdvanceStep;
                distance = Mathf.Max(AimRayAdvanceStep, distance - AimRayAdvanceStep);

                if (log)
                {
                    Debug.Log($"[Turret] 瞄准射线起点被挡住(落点距起点 {tooNear:F2} 米), " +
                              $"沿射向前移 {AimRayAdvanceStep:F1} 米重测, 新起点={FormatVec(origin)}", this);
                }

                point = RaycastAimPointOnce(origin, dir, distance, log);
            }

            return point;
        }

        /// <summary>落点离起点近到可以把起点视为"被碰撞体包住"</summary>
        private static bool IsAimPointTooNear(Vector3 origin, Vector3 point)
        {
            return (point - origin).sqrMagnitude < AimRayTooNear * AimRayTooNear;
        }

        /// <summary>
        /// 沿指定起点与方向检测落点: 同阵营角色与已死亡角色不挡,
        /// 命中场景或敌人取命中点, 无阻挡取 distance 尽头
        /// </summary>
        private Vector3 RaycastAimPointOnce(Vector3 origin, Vector3 dir, float distance, bool log = false)
        {
            int count = Physics.RaycastNonAlloc(origin, dir, aimHitBuffer, distance, AimRayMask, QueryTriggerInteraction.Collide);

            aimHits.Clear();
            for (int i = 0; i < count; i++)
            {
                aimHits.Add(aimHitBuffer[i]);
            }

            aimHits.Sort(CompareByDistance);
            aimHitTarget = false;

            Vector3 result = origin + dir * distance;
            string target = "无";

            for (int i = 0; i < aimHits.Count; i++)
            {
                RaycastHit hit = aimHits[i];

                //视角在炮口后上方(cameraBackOffset), 射线会先穿过炮台本体与操控者,
                //不排掉的话落点直接落在自己身上, 发射方向会反成朝后打
                if (IsSelfCollider(hit.collider))
                {
                    continue;
                }

                //落点在炮口后方: cameraBackOffset 把视角推到炮台背后时, 相机身后的栏杆/墙
                //会挡在射线上, 这种命中既不能当准星落点, 也不该挡住子弹, 继续往后找
                if (Vector3.Dot(hit.point - muzzle.position, AimDirection) <= 0f)
                {
                    continue;
                }

                //角色碰撞体挂在子节点上, IdRole 在刚体所在节点
                Rigidbody body = hit.collider.attachedRigidbody;
                if (body != null && body.TryGetComponent(out IdRole idRole) &&
                    idRole.GetEntity() is Role role)
                {
                    bool isSameTeam = role.team == OperatorRole.team;

                    if (isSameTeam || role.IsDie)
                    {
                        //同阵营与尸体不挡准心, 继续往后找
                        continue;
                    }

                    aimHitTarget = true;
                    if (log)
                    {
                        FlushAimLog(origin, dir, hit.point, DescribeHit(hit));
                    }
                    return hit.point;
                }

                result = hit.point;
                target = DescribeHit(hit);
                break;
            }

            if (log)
            {
                FlushAimLog(origin, dir, result, target);
            }

            return result;
        }

        /// <summary>射线命中按距离排序, 缓存成静态委托避免每次排序都分配</summary>
        private static readonly System.Comparison<RaycastHit> CompareByDistance =
            (a, b) => a.distance.CompareTo(b.distance);

        /// <summary>输出本次开火的射线结果: 打到谁、朝哪打、打了多远</summary>
        private void FlushAimLog(Vector3 origin, Vector3 dir, Vector3 result, string target)
        {
            Debug.Log($"[Turret] 射线目标:{target};方向{FormatVec(dir)}," +
                      $"距离{Vector3.Distance(origin, result):F2}", this);
        }

        /// <summary>命中项的短名: 物体名/层名(层号), 供首行显示"射线目标"</summary>
        private static string DescribeHit(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return "无";
            }

            int layer = hit.collider.gameObject.layer;
            return $"{hit.collider.name}/{LayerMask.LayerToName(layer)}({layer})";
        }

        private static string FormatVec(Vector3 v)
        {
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }

        #endregion

        #region Util

        /// <summary>每帧上报与本地玩家的距离, 由 TurretSystem 挑出最近的可交互炮台</summary>
        private void ReportLocalPlayerNear()
        {
            Player0 player = GameDataCommon.LocalPlayer;
            if (player == null || player.IsDie)
            {
                TurretSystem.ReportNearby(this, false);
                return;
            }

            float distance = Vector3.Distance(player.transform.position, transform.position);
            TurretSystem.ReportNearby(this, distance <= interactRadius, distance);
        }

        /// <summary>
        /// 炮台上方的名字牌: 未进入操控时始终面向玩家, 玩家自己操控时整块隐藏, 退出操控后恢复。
        /// 显隐只在状态变化时改, 朝向每帧跟随
        /// </summary>
        private void UpdateInteractTip()
        {
            if (string.IsNullOrEmpty(interactTipKey))
            {
                return;
            }

            if (tipHelper == null)
            {
                tipHelper = new HomeTriggerNameText(transform, interactTipKey);
                tipHelper.Ensure(interactTipOffsetY);

                //Resources 里没有 Canvas_Talk 时降级为不显示, 不影响交互本身
                if (tipHelper.Root == null)
                {
                    return;
                }
            }

            GameObject root = tipHelper.Root;
            if (root == null)
            {
                return;
            }

            //玩家自己操控时整块关掉: 只降 alpha 会残留在第一人称视野里
            bool isOperating = OperatorType == ETurretOperatorType.Player;
            if (isOperating != isTipHiddenByControl)
            {
                isTipHiddenByControl = isOperating;
                root.SetActive(!isOperating);
            }

            if (isOperating)
            {
                return;
            }

            //靠近的炮台名字牌变亮, 其余淡显
            bool isNear = TurretSystem.Nearby == this;
            if (isNear != isTipShowing)
            {
                isTipShowing = isNear;

                if (isNear)
                {
                    tipHelper.ShowUI();
                }
                else
                {
                    tipHelper.HideUI();
                }
            }

            //未操控时始终面向玩家视角(主相机)
            FaceToCamera(root.transform);
        }

        /// <summary>
        /// 名字牌只绕 Y 轴转向相机(玩家视角): 抬头/低头时牌子不会跟着前后翻, 从任何方位看都是正的。
        /// 用相机而不是角色位置 —— 第三人称相机在角色后上方, 按角色位置摆会偏;
        /// 炮台瞄准时相机由 vcam 接管, 但仍是同一个主相机, 朝向会自动跟随。
        /// </summary>
        private void FaceToCamera(Transform tip)
        {
            if (tipFaceCamera == null)
            {
                tipFaceCamera = Camera.main;
            }

            if (tipFaceCamera == null)
            {
                return;
            }

            Vector3 dir = tipFaceCamera.transform.position - tip.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            //世界空间 Canvas 的正面在 -Z(forward 的反方向), 所以要让 forward 背离相机,
            //-Z 才朝向相机; 直接用 dir 会让玩家看到背面, 文字是镜像的
            tip.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        }

        private void ApplyHeadAim(float dt)
        {
            curYaw = Mathf.MoveTowards(curYaw, targetYaw, headRotateSpeed * dt);
            curPitch = Mathf.MoveTowards(curPitch, targetPitch, headRotateSpeed * dt);

            float pitch = Mathf.Clamp(curPitch + recoilOffset, headPitchMin, headPitchMax);
            head.localRotation = headBaseRot * Quaternion.Euler(-pitch, curYaw, 0f);
        }

        /// <summary>
        /// 世界坐标相对炮台正前方的偏航角(-180~180), 符号与 transform 的 Y 轴旋转一致:
        /// 目标在右侧为正, 直接作为炮头的目标偏航角使用
        /// </summary>
        private float GetLocalYaw(Vector3 worldPos)
        {
            Vector3 dir = worldPos - transform.position;
            dir.y = 0f;
            return Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        }

        /// <summary>
        /// 世界坐标相对炮口水平面的俯仰角(度), 上为正、下为负,
        /// 与 <see cref="AimRotation"/> 的俯仰符号一致
        /// </summary>
        private float GetLocalPitch(Vector3 worldPos)
        {
            Vector3 dir = worldPos - muzzle.position;
            float flatDistance = new Vector2(dir.x, dir.z).magnitude;
            return Mathf.Atan2(dir.y, flatDistance) * Mathf.Rad2Deg;
        }

        /// <summary>在编辑器里显示交互半径、攻击范围与炮头的偏航/俯仰限制, 方便摆位</summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, interactRadius);

            //攻击范围来自配置, 没配就不画
            if (attackConfig == null)
            {
                return;
            }

            float range = attackConfig.attackRange;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, range);

            Gizmos.color = Color.red;
            Vector3 leftDir = Quaternion.Euler(0f, -headYawLimit, 0f) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0f, headYawLimit, 0f) * transform.forward;
            Gizmos.DrawLine(origin, origin + leftDir * range);
            Gizmos.DrawLine(origin, origin + rightDir * range);

            Vector3 muzzlePos = muzzle.position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(muzzlePos, muzzlePos + Quaternion.Euler(-headPitchMax, 0f, 0f) * transform.forward * range);
            Gizmos.DrawLine(muzzlePos, muzzlePos + Quaternion.Euler(-headPitchMin, 0f, 0f) * transform.forward * range);
        }

        #endregion
    }
}
