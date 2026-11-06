using Cinemachine;
using MFPC;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XiaoCao
{
    ///<see cref="CameraMgr"/>
    public class CameraController : MonoBehaviour, ICameraController
    {
        public CinemachineVirtualCamera vcam_topDown;

        public CamDataCommon dataCommon;
        public CamDataHome dataHome;
        public CamDataTopDown setting_topDown;

        public float shakeIntensity = 1;
        private float shakeTimer = 0;
        public float test_shakeTime = 0.5f;

        private Transform _curLookAt;
        private Transform _curFollow;


        public CameraMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        private Cinemachine3rdPersonFollow _3rd;

        public Cinemachine3rdPersonFollow V3rdPF
        {
            get
            {
                if (_3rd == null)
                {
                    _3rd = vcam_topDown.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                }

                return _3rd;
            }
        }

        public CinemachineFramingTransposer _cft;

        public CinemachineFramingTransposer CFT
        {
            get
            {
                if (!_cft)
                {
                    _cft = vcam_topDown.GetCinemachineComponent<CinemachineFramingTransposer>();
                }

                return _cft;
            }
        }

        private CinemachineBasicMultiChannelPerlin _cPerlin;

        public CinemachineBasicMultiChannelPerlin CPerlin
        {
            get
            {
                if (_cPerlin == null)
                {
                    _cPerlin = vcam_topDown.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                }

                return _cPerlin;
            }
        }

        public bool IsRunning => GameDataCommon.Current.gameState == GameState.Running;

        [OnValueChanged(nameof(TurnMode))]
        [SerializeField]
        private CameraMode _mode;

        private bool isInited = false;


        void Start()
        {
            CameraMgr.Inst.InitCam(this);
        }

        public void Init()
        {
            if (isInited)
            {
                return;
            }

            isInited = true;
            vcam_topDown.gameObject.SetActive(true);
            TurnMode();
        }

        /// <summary>
        /// 切换视角模式, 同时按本地设置里的相机距离偏移刷新相机距离
        /// </summary>
        void TurnMode()
        {
            if (_mode == CameraMode.ThirdPerson)
            {
                curAngleX = dataHome.defaultAngle.x;
                curAngleY = dataHome.defaultAngle.y;
                CFT.m_ScreenX = dataHome.screenXY.x;
                CFT.m_ScreenY = dataHome.screenXY.y;
            }
            else
            {
                curAngleX = setting_topDown.defaultAngle.x;
                curAngleY = setting_topDown.defaultAngle.y;
                CFT.m_ScreenX = dataCommon.screenXY.x;
                CFT.m_ScreenY = dataCommon.screenXY.y;
            }

            ApplySettingCamDistance();
        }

        /// <summary>
        /// 按本地设置里的相机距离偏移刷新相机距离, 供设置面板调节时实时调用
        /// </summary>
        public void ApplySettingCamDistance()
        {
            CFT.m_CameraDistance = GetSettingCamDistance();
        }

        /// <summary>
        /// 取本地设置里的相机距离偏移(-1~1), 线性映射到 minCamDistance~maxCamDistance
        /// </summary>
        private float GetSettingCamDistance()
        {
            float offset = ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.CameraDistance, 0f);
            return MathTool.ValueMapping(offset, -1, 1.5f, setting_topDown.minCamDistance, setting_topDown.maxCamDistance);
        }

        public void OnUpdate()
        {
            SetLocalSwipeDirection();

            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                CPerlin.m_AmplitudeGain = 0;
            }


            if (Mode is CameraMode.TowDown or CameraMode.ThirdPerson)
            {
                if (BattleData.Current.CanPlayerControl)
                {
                    //炮台第一人称期间滚轮归炮台调焦距, 主相机不再跟着改距离
                    if (!TurretAimCamera.IsActive && !Input.mouseScrollDelta.IsZore())
                    {
                        var distance = Mathf.Clamp(CFT.m_CameraDistance - Input.mouseScrollDelta.y,
                            setting_topDown.minCamDistance, setting_topDown.maxCamDistance);
                        CFT.m_CameraDistance = distance;
                    }
                }
            }
        }

        private float lastTransparentUpdate = 0f;
        private const float transparentUpdateInterval = 0.1f; // 每0.1秒更新一次

        private RaycastHit[] _raycastHits = new RaycastHit[8];

        private List<Material> _materialList = new List<Material>();

        private void TransparentObjects()
        {
            if (!CheckHasPlayer())
            {
                return;
            }
            Vector3 selfPosition = player0.transform.position + Vector3.up;
            Vector3 cameraPosition = CameraMgr.Main.transform.position;
            var rayDistance = Vector3.Distance(selfPosition, cameraPosition);
            Vector3 direction = Vector3.Normalize(cameraPosition - selfPosition);
            Debug.DrawLine(selfPosition, cameraPosition, Color.red);

            var layerMask = Layers.WALL_BLOCK_MASK;
            var size = Physics.RaycastNonAlloc(selfPosition, direction, this._raycastHits, rayDistance, layerMask);
            List<Material> materials = new List<Material>();
            for (int i = 0; i < size; i++)
            {
                var meshRenderers = this._raycastHits[i].collider.GetComponentsInChildren<MeshRenderer>();
                foreach (var variable in meshRenderers)
                {
                    materials.AddRange(variable.materials);
                }
            }

            var transparentList = materials.Except(_materialList).ToList();
            var opaqueList = _materialList.Except(materials).ToList();
            foreach (var variable in transparentList)
            {
                MaterialTransparent.SetMaterialTransparent(true, variable, 0.4f);
            }

            foreach (var variable in opaqueList)
            {
                MaterialTransparent.SetMaterialTransparent(false, variable);
            }

            _materialList = materials;
        }

        private bool CheckHasPlayer()
        {
            if (player0 == null)
            {
                player0 = GameDataCommon.LocalPlayer;
            }

            return player0 != null;
        }

        public void OnFixedUpdate()
        {
            if (!IsRunning)
            {
                return;
            }

            //炮台第一人称期间画面由炮台的虚拟相机提供, 这里只保留血条等 UI 跟随;
            //同时清掉累积的拖动量, 免得退出炮台时主相机一下甩过去
            if (TurretAimCamera.IsActive)
            {
                UIMgr.Inst.battleHud.AnimTargetFixUpdate();
                _inputLook = Vector2.zero;
                return;
            }

            if (Mode == CameraMode.ThirdPerson)
            {
                On3rdCamFixedUpdate();
            }
            else
            {
                TopDownFixedUpdate();
            }

            if (Time.time - lastTransparentUpdate >= transparentUpdateInterval)
            {
                TransparentObjects();
                lastTransparentUpdate = Time.time;
            }
        }


        private Vector2 _inputLook;
        public float curAngleX;
        public float curAngleY;

        private void On3rdCamFixedUpdate()
        {
            if (_inputLook.IsZore())
            {
                FixUpdateCamRotate(vcam_topDown.transform);
            }
        }

        void TopDownFixedUpdate()
        {
            CheckPlayerLockEnemy();
            UIMgr.Inst.battleHud.AnimTargetFixUpdate();
            FixUpdateCamRotate(vcam_topDown.transform);
        }

        private void FixUpdateCamRotate(Transform VCamTran)
        {
            curAngleY += _inputLook.x * setting_topDown.swipeSpeedY * Time.fixedDeltaTime;
            curAngleX += _inputLook.y * setting_topDown.swipeSpeedX * Time.fixedDeltaTime;
            curAngleX = ClampAngle(curAngleX, dataCommon.BottomClamp, dataCommon.TopClamp);
            VCamTran.transform.rotation = Quaternion.Euler(curAngleX, curAngleY, 0.0f);
            _inputLook = Vector2.zero;
        }

        private float findEnmeyTime;
        private float remindEnmeyTime = 0.25f;
        private float tempSpeed;
        private Player0 player0;

        //AimToDirTarget 抢占用的目标与剩余时间
        private Transform _aimTarget;
        private float _aimTimer;

        /// <summary>
        /// 抢占镜头: 在 aimTime 秒内强制把视角转向 target, 期间 <see cref="CheckPlayerLockEnemy"/> 的索敌与自动回正全部让位,
        /// 时间到或目标失效后自动恢复原逻辑
        /// </summary>
        /// <param name="target">要瞄准的目标</param>
        /// <param name="aimTime">持续时间(秒), 小于等于 0 表示立即失效</param>
        public void AimToDirTarget(Transform target, float aimTime)
        {
            _aimTarget = target;
            _aimTimer = aimTime;

            //清掉滑动后的等待, 否则刚设上就被"暂时夺取控制"挡掉
            autoLookForwardWait = 0;
        }

        /// <summary>
        /// 处理 <see cref="AimToDirTarget"/> 的抢占: 有效期内把视角转向目标并返回 true, 让原本的索敌/回正整段跳过
        /// </summary>
        private bool TryAimToDirTarget()
        {
            if (_aimTarget == null)
            {
                return false;
            }

            _aimTimer -= XCTime.fixedDeltaTime;
            if (_aimTimer <= 0f || !_aimTarget.gameObject.activeInHierarchy)
            {
                _aimTarget = null;
                return false;
            }

            AimToDIr(_aimTarget.position - player0.transform.position);
            return true;
        }

        //相机索敌
        public void CheckPlayerLockEnemy()
        {
            player0 = GameDataCommon.LocalPlayer;

            //暂时夺取控制
            autoLookForwardWait -= XCTime.fixedDeltaTime;

            if (player0 == null || player0.data_R.IsBusy)
            {
                return;
            }

            //瞄准指定目标期间, 索敌和回正都不许插手
            if (TryAimToDirTarget())
            {
                return;
            }

            //关掉自动索敌就一直回正(默认关闭, 见 GameSetting.AutoLockEnemy)
            if (!GameSetting.AutoLockEnemy)
            {
                AutoDirect();
                return;
            }

            if (TryGetLockEnemy(player0, out Transform enemyTf))
            {
                //规则 偏角不能超过15度
                AimToDIr(enemyTf.position - player0.transform.position);
            }
            else
            {
                AutoDirect();
            }
        }

        /// <summary>
        /// 取当前要锁定的敌人: 优先沿用 lastEnemy, 超时或已死亡时重新搜索一次
        /// </summary>
        /// <param name="role">发起索敌的玩家</param>
        /// <param name="enemyTf">锁定目标的 Transform, 没有目标时为 null</param>
        private bool TryGetLockEnemy(Role role, out Transform enemyTf)
        {
            enemyTf = null;

            //超过提醒间隔就丢掉上一个目标, 让它重新找
            if (findEnmeyTime + remindEnmeyTime < Time.time)
            {
                role.data_R.lastEnemy = null;
            }

            Role findRole = role.data_R.lastEnemy;
            if (findRole == null || findRole.IsDie)
            {
                if (!role.FindEnemy(out findRole, setting_topDown.seeR, setting_topDown.seeAngle))
                {
                    return false;
                }

                role.data_R.lastEnemy = findRole;
                findEnmeyTime = Time.time;
            }

            if (findRole == null || findRole.IsDie)
            {
                return false;
            }

            enemyTf = findRole.transform;
            return true;
        }

        //自动回正
        void AutoDirect()
        {
            //锁定视角(默认开启): 开着就不自动回正, 镜头完全交给玩家手动转
            bool isLockCam = GameSetting.LockCam;
            if (isLockCam)
            {
                return;
            }

            if (swipe)
            {
                //转动镜头后,不再回正
                if (player0.Movement.inputDir.IsZore())
                {
                    return;
                }
                else
                {
                    swipe = false;
                }
            }

            AimToDIr(player0.transform.forward);
        }


        private float lastDeltaAngle;
        private float distance;
        private float autoLookForwardWait;
        private float setLookForwardWaitTime = 1f; //中断镜头自动回正时间

        private void AimToDIr(Vector3 dir, bool isStoping = false)
        {
            if (autoLookForwardWait > 0)
            {
                return;
            }

            //如果相差超过90度, 则不跟踪
            Vector3 inputDir = CameraMgr.Forword;

            float deltaAngle = MathTool.SignedAngleY(inputDir, dir);

            if (isStoping)
            {
                curAngleX = Mathf.Lerp(curAngleX, setting_topDown.stopAngleX,
                    setting_topDown.aimLerp * Time.fixedDeltaTime);
            }
            else
            {
                float addX = 0;
                //if (distance > 0 && distance < 10)
                //{
                //    addX = Mathf.Lerp(setting_topDown.nearAddAngleX, 0, distance / 10);
                //}

                curAngleX = Mathf.Lerp(curAngleX, setting_topDown.defaultAngle.x + addX,
                    setting_topDown.aimLerp * Time.fixedDeltaTime * 2);
            }


            if (Mathf.Abs(deltaAngle) < setting_topDown.aimSafeAngle)
            {
                return;
            }

            if (Mathf.Abs(deltaAngle) > 180 - setting_topDown.aimSafeAngle)
            {
                return;
            }

            //抖动原因: 目标处于安全角边界, 刚移出时, 触发回正
            //可能解决办法: 符号控制, 左转, 右转需要等待一个静止帧
            if (lastDeltaAngle * deltaAngle < 0)
            {
                lastDeltaAngle = deltaAngle;
                isStoping = true;
                return;
            }


            lastDeltaAngle = deltaAngle;


            if (isStoping)
            {
                curAngleY = Mathf.Lerp(curAngleY, curAngleY - deltaAngle,
                    setting_topDown.aimLerp * Time.fixedDeltaTime * setting_topDown.smoothTime);
            }
            else
            {
                curAngleY = Mathf.Lerp(curAngleY, curAngleY - deltaAngle,
                    setting_topDown.aimLerp * Time.fixedDeltaTime);
            }

            // curAngleY = Mathf.SmoothDamp(curAngleY, curAngleY - deltaAngle, ref tempSpeed, setting_topDown.smoothTime);
        }

        private bool swipe;

        void SetLocalSwipeDirection()
        {
            if (!PlayerInputData.LocalSwipeDirection.IsZore())
            {
                autoLookForwardWait = setLookForwardWaitTime;
                swipe = true;
            }

            float speed = ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 1);
            if (Application.isMobilePlatform)
            {
                speed *= 5;
            }

            _inputLook.y += PlayerInputData.LocalSwipeDirection.y * speed;
            _inputLook.x += PlayerInputData.LocalSwipeDirection.x * speed;
            //
        }

        public void SetTarget(Transform follow, Transform lookAt = null)
        {
            vcam_topDown.Follow = follow;
            _curFollow = follow;
            vcam_topDown.LookAt = CameraMgr.Inst.aimer.transform;
            _curLookAt = lookAt;

            if (Mode == CameraMode.TowDown)
            {
                TurnMode();
            }

            CameraMgr.Inst.aimer.SetAim(lookAt, 0);
        }

        /// <summary>
        /// 立即把镜头摆到角色身后: 水平角对齐角色朝向, 俯仰角回到当前模式的默认值。
        /// 出生/进图时角色朝向跟随起点, 相机也要跟着切过去, 否则一进场是背对背的视角
        /// </summary>
        public void AlignBehindTarget()
        {
            if (player0 == null)
            {
                player0 = GameDataCommon.LocalPlayer;
            }

            if (player0 == null)
            {
                return;
            }

            curAngleY = player0.transform.eulerAngles.y;
            curAngleX = Mode == CameraMode.ThirdPerson ? dataHome.defaultAngle.x : setting_topDown.defaultAngle.x;

            //清掉拖动/抢占残留, 让自动回正继续按角色朝向工作
            swipe = false;
            autoLookForwardWait = 0;
            _inputLook = Vector2.zero;
            _aimTarget = null;

            //不等下一次 FixedUpdate, 直接写进虚拟相机, 避免出生那一帧视角闪一下
            if (vcam_topDown)
            {
                vcam_topDown.transform.rotation = Quaternion.Euler(curAngleX, curAngleY, 0f);
            }
        }

        private void OnEditorModeChanged()
        {
            if (Application.isPlaying)
            {
                TurnMode();
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }


        [Button]
        void testShake()
        {
            ShakeCamera(test_shakeTime);
        }

        public void ShakeCamera(float shakeTime)
        {
            CPerlin.m_AmplitudeGain = shakeIntensity;
            shakeTimer = Mathf.Max(shakeTimer, shakeTime);
        }
    }

    [Serializable]
    public class CamDataCommon
    {
        public float TopClamp = 45.0f;
        public float BottomClamp = -15.0f;
        public Vector2 screenXY = new Vector2(0.5f, 0.7f);
    }
    [Serializable]
    public class CamDataHome
    {
        public Vector2 defaultAngle = new Vector2(15, 0);
        public Vector2 screenXY = new Vector2(0.5f, 0.78f);
    }

    [Serializable]
    public class CamDataTopDown
    {
        public Vector2 defaultAngle = new Vector2(30, 0);
        public float stopAngleX = 5;
        public float farAddAngleX = 15;
        public float camDistance = 7.5f;
        public float minCamDistance = 2.3f;
        public float maxCamDistance = 11f;
        public float aimLerp = 0.1f;
        public float smoothTime = 0.5f;
        [XCLabel("安全角度范围")] public float aimSafeAngle = 5;
        public float seeR = 8;
        public float seeAngle = 45;

        [Header("滑动转向速度 x是上下角")] public float swipeSpeedX = 0.1f;
        public float swipeSpeedY = 0.5f;
    }
}
