using Cinemachine;
using Cinemachine.Utility;
using NaughtyAttributes;
using System;
using UnityEngine;
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

        //private Cinemachine3rdPersonFollow _vcomponent_3rd;
        //public Cinemachine3rdPersonFollow Vcomponent_3rd
        //{
        //    get
        //    {
        //        if (_vcomponent_3rd == null)
        //        {
        //            _vcomponent_3rd = vcam_topDown.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        //        }
        //        return _vcomponent_3rd;
        //    }
        //}

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
            else
            {       //拖拽的移动量 TODO
                if (Input.GetMouseButton(0))
                {

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
                TopDownUpdate();
            }
        }
        //索敌的思路, 可以将瞄准中心变为 玩家和敌人的中心点
        //或者只需要将

        private Vector2 _inputLook;
        private float _curAngleX;
        private float _curAngleY;

        private void On3rdCamFixedUpdate()
        {
            if (_inputLook.IsZore())
            {
                _curAngleY += _inputLook.x;
                _curAngleX += _inputLook.y * -1f;

                _curAngleY = ClampAngle(_curAngleY, float.MinValue, float.MaxValue);
                _curAngleX = ClampAngle(_curAngleX, setting_3rd.BottomClamp, setting_3rd.TopClamp);

                vcam_topDown.transform.rotation = Quaternion.Euler(_curAngleX, _curAngleY, 0);
            }
        }

        void TopDownInit()
        {
            _curAngleX = setting_topDown.defaultAngle.x;
            _curAngleY = setting_topDown.defaultAngle.y;
        }
        void TopDownUpdate()
        {
            vcam_topDown.transform.rotation = Quaternion.Euler(_curAngleX, _curAngleY, 0.0f);
        }




        public void SetTarget(Transform follow, Transform lookAt = null)
        {
            vcam_topDown.Follow = follow;
            vcam_topDown.LookAt = lookAt;

            _curFollow = follow;
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
    }

}


