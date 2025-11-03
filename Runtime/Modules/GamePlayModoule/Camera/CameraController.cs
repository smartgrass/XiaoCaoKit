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

        public CamData3rd setting_3rd;
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


        [OnValueChanged(nameof(TurnMode))] [SerializeField]
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
            TurnMode();
        }

        void TurnMode()
        {
            vcam_topDown.gameObject.SetActive(true);
            TopDownInit();
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


            if (Mode == CameraMode.TowDown)
            {
                if (BattleData.Current.CanPlayerControl)
                {
                    if (!Input.mouseScrollDelta.IsZore())
                    {
                        var distance = Mathf.Clamp(CFT.m_CameraDistance - Input.mouseScrollDelta.y,
                            setting_topDown.minCamDistance, setting_topDown.camDistance + 6);
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

        public void OnFixedUpdate()
        {
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

        void TopDownInit()
        {
            curAngleX = setting_topDown.defaultAngle.x;
            curAngleY = setting_topDown.defaultAngle.y;
            CFT.m_CameraDistance = setting_topDown.camDistance;
        }

        void TopDownFixedUpdate()
        {
            CheckPlayer();
            UIMgr.Inst.battleHud.AnimTargetFixUpdate();
            FixUpdateCamRotate(vcam_topDown.transform);
        }

        private void FixUpdateCamRotate(Transform VCamTran)
        {
            curAngleY += _inputLook.x * setting_topDown.swipeSpeedY * Time.fixedDeltaTime;
            curAngleX += _inputLook.y * -1f * setting_topDown.swipeSpeedX * Time.fixedDeltaTime;
            curAngleX = ClampAngle(curAngleX, setting_3rd.BottomClamp, setting_3rd.TopClamp);
            VCamTran.transform.rotation = Quaternion.Euler(curAngleX, curAngleY, 0.0f);
            _inputLook = Vector2.zero;
        }

        private float findEnmeyTime;
        private float remindEnmeyTime = 0.25f;
        private float tempSpeed;
        private Player0 player0;


        //相机索敌
        public void CheckPlayer()
        {
            bool isAutoLockEnmey = ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.AutoLockEnemy);


            player0 = GameDataCommon.LocalPlayer;

            //暂时夺取控制
            autoLookForwardWait -= XCTime.fixedDeltaTime;

            if (player0 != null && !player0.data_R.IsBusy)
            {
                if (!isAutoLockEnmey)
                {
                    AutoDirect();
                    return;
                }

                bool hasEnmey = false;

                if (findEnmeyTime + remindEnmeyTime < Time.time)
                {
                    player0.data_R.lastEnemy = null;
                }

                Role findRole = player0.data_R.lastEnemy;
                if (findRole == null || findRole.IsDie)
                {
                    if (player0.FindEnemy(out findRole, setting_topDown.seeR, setting_topDown.seeAngle))
                    {
                        player0.data_R.lastEnemy = findRole;
                        findEnmeyTime = Time.time;
                        hasEnmey = true;
                    }
                }
                else
                {
                    hasEnmey = true;
                }

                //TODO 减少频率
                if (hasEnmey && !findRole.IsDie)
                {
                    //规则 偏角不能超过15度
                    Vector3 dir = (findRole.transform.position - player0.transform.position);
                    AimToDIr(dir);
                }
                else
                {
                    AutoDirect();
                }
            }
        }

        //自动回正
        void AutoDirect()
        {
            bool isLockCam = ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.LockCam);
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

            float speed = ConfigMgr.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 1);
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
                TopDownInit();
            }

            CameraMgr.Inst.aimer.SetAim(lookAt, 0);
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
    public class CamData3rd
    {
        public float TopClamp = 45.0f;
        public float BottomClamp = -15.0f;
    }

    [Serializable]
    public class CamDataTopDown
    {
        public Vector2 defaultAngle = new Vector2(30, 0);
        public float stopAngleX = 5;
        public float farAddAngleX = 15;
        public float camDistance = 7.5f;
        public float minCamDistance = 1;
        public float aimLerp = 0.1f;
        public float smoothTime = 0.5f;
        [XCLabel("安全角度范围")] public float aimSafeAngle = 5;
        public float seeR = 8;
        public float seeAngle = 45;

        [Header("滑动转向速度")] public float swipeSpeedX = 0.1f;
        public float swipeSpeedY = 0.5f;
    }
}