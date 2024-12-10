using Cinemachine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{

    public class CameraMgr : MonoSingleton<CameraMgr>, IMgr
    {
        #region static
        public static Camera Main
        {
            get
            {
                if (!_main)
                {
                    _main = Camera.main;
                }
                return _main;
            }
        }

        private static Camera _main;

        public static Vector3 Forword { get => Main.transform.forward; }

        #endregion
        public const string camPath = "Camera/CameraController";

        public ICameraController controller;
        public Transform target;
        public CamerAimer aimer;
        public Transform tempLookAt;

        public override void Init()
        {
            base.Init();
            tempLookAt = new GameObject("tempLookAt").transform;

            var tempAimerGo = new GameObject("CamerAimer");
            aimer = tempAimerGo.AddComponent<CamerAimer>();
            tempAimerGo.transform.SetParent(transform,true);
            tempLookAt.transform.SetParent(transform, true);
        }

        public void InitCam(ICameraController setCam = null)
        {
            if (setCam != null)
            {
                controller = setCam;
            }
            
            if (controller == null)
            {
                GameObject camPrefab = Resources.Load(camPath) as GameObject;
                var go = Instantiate(camPrefab);
                controller = go.GetComponent<CameraController>();
            }
            controller.Init();
        }

        //刷新用
        public static void ReFindCamera()
        {
            _main = Camera.main;
        }

        private CinemachineVirtualCamera GetCurVirtualCamera()
        {
            CinemachineBrain cb = CinemachineCore.Instance.GetActiveBrain(0);
            if (cb.ActiveVirtualCamera != null)
            {
                CinemachineVirtualCamera virtualCamera = cb.ActiveVirtualCamera as CinemachineVirtualCamera;
                return virtualCamera;
            }
            return null;
        }
        private void Update()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
                return;

            if (controller != null)
                controller.OnUpdate();

        }

        private void FixedUpdate()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
                return;

            if (null == target)
            {
                FindPlayerTarget();
                return;
            }
            if (controller != null)
                controller.OnFixedUpdate();
        }


        private void FindPlayerTarget()
        {
            Role role = GameDataCommon.LocalPlayer;
            if (role != null && role.isBodyCreated)
            {
                target = role.idRole.GetFollow;
                controller.SetTarget(role.idRole.GetFollow, role.idRole.GetLookAt);
            }
        }

        private void LookAtTarget(Transform tf)
        {
            //controller.SetTarget(role.idRole.GetFollow, role.idRole.GetLookAt);
        }
    }


    public enum CameraMode
    {
        TowDown,
        ThirdPerson
    }

    public enum CameraLayer
    {
        Normal = 2, //正常视图
        Disable = -1,  //关闭
        Pop = 10 //弹出
    }

    public interface ICameraController
    {
        public CameraMode Mode { get; set; }
        void Init();
        void SetTarget(Transform target, Transform aim = null);
        public void OnUpdate();
        public void OnFixedUpdate();

    }

}


