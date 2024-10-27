using Cinemachine;
using NaughtyAttributes;
using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    ///<see cref="CameraMgr"/>
    public class CameraController : MonoBehaviour, ICameraController
    {
        public CinemachineVirtualCamera vcam_topDown;

        public CamData3rd setting_3rd;
        public CamDataTopDown setting_topDown;


        private Transform _curLookAt;
        private Transform _curFollow;


        public CameraMode Mode { get => _mode; set => _mode = value; }

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
            TurnMode();
        }

        void TurnMode()
        {
            vcam_topDown.gameObject.SetActive(true);
            TopDownInit();
        }
        public void OnUpdate()
        {
            if (Mode == CameraMode.ThirdPerson)
            {
                _inputLook.x = Input.GetAxis("Mouse X");
                _inputLook.y = Input.GetAxis("Mouse Y");
            }
            else if (Mode == CameraMode.TowDown)
            {
                if (!Input.mouseScrollDelta.IsZore())
                {
                    var distance = Mathf.Clamp(CFT.m_CameraDistance - Input.mouseScrollDelta.y,
                        setting_topDown.camDistance - 3, setting_topDown.camDistance + 3);
                    CFT.m_CameraDistance = distance;
                }
            }



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
        }
        //索敌的思路, 可以将瞄准中心变为 玩家和敌人的中心点
        //或者只需要将

        private Vector2 _inputLook;
        public float curAngleX;
        public float curAngleY;

        private void On3rdCamFixedUpdate()
        {
            if (_inputLook.IsZore())
            {
                curAngleY += _inputLook.x;
                curAngleX += _inputLook.y * -1f;

                curAngleY = ClampAngle(curAngleY, float.MinValue, float.MaxValue);
                curAngleX = ClampAngle(curAngleX, setting_3rd.BottomClamp, setting_3rd.TopClamp);

                vcam_topDown.transform.rotation = Quaternion.Euler(curAngleX, curAngleY, 0);
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
            
            vcam_topDown.transform.rotation = Quaternion.Euler(curAngleX, curAngleY, 0.0f);
        }


        private Role lastEnemy;
        private float findEnmeyTime;
        private float remindEnmeyTime = 0.5f;
        public void CheckPlayer()
        {
            bool isAutoLockEnmey = ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.AutoLockEnemy);
            bool isLockCam = ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.LockCam);
            
            Player0 player0 = GameDataCommon.Current.player0;
            if (player0 != null && !player0.roleData.IsBusy)
            {
                if (!isAutoLockEnmey)
                {
                    if (!isLockCam)
                    {
                        AimToDIr(player0.transform.forward);
                    }
                    return;
                }


                bool hasEnmey = false;

                if (findEnmeyTime + remindEnmeyTime < Time.time)
                {
                    lastEnemy = null;
                }

                Role findRole = lastEnemy;
                if (lastEnemy == null || lastEnemy.IsDie)
                {
                    if (player0.FindEnemy(out findRole, setting_topDown.seeR, setting_topDown.seeAngle))
                    {
                        lastEnemy = findRole;
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
                    if (!isLockCam)
                    {
                        AimToDIr(player0.transform.forward);
                    }
                }
            }

        }

        private void AimToDIr(Vector3 dir)
        {
            //如果相差超过90度, 则不跟踪
            Vector3 inputDir = CameraMgr.Forword;

            float deltaAngle = MathTool.SignedAngleY(inputDir, dir);
            DebugGUI.Log("deltaAngle", deltaAngle);

            if (Mathf.Abs(deltaAngle) < setting_topDown.aimSafeAngle)
            {
                return;
            }
            //float addSpeed = 1;

            if (Mathf.Abs(deltaAngle) > 180 - setting_topDown.aimSafeAngle)
            {
                return;
            }

            curAngleY = Mathf.Lerp(curAngleY, curAngleY - deltaAngle, setting_topDown.aimLerp * Time.fixedDeltaTime);
        }

        public void SetTarget(Transform follow, Transform lookAt = null)
        {
            vcam_topDown.Follow = follow;
            _curFollow = follow;
            CameraMgr.Inst.aimer.SetAim(lookAt, 0);
            vcam_topDown.LookAt = CameraMgr.Inst.aimer.transform;
            _curLookAt = lookAt;

            if (Mode == CameraMode.TowDown)
            {
                TopDownInit();
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
        public float camDistance = 7.5f;
        public float aimLerp = 0.1f;
        [XCLabel("安全角度范围")]
        public float aimSafeAngle = 5;
        public float seeR = 8;
        public float seeAngle = 45;

    }

}


