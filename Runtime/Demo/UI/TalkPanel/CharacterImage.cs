using System;
using System.Collections;
using MFPC;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace XiaoCao
{
    public class CharacterImage : MonoBehaviour
    {
        public RawImage img;
        public CameraCapture cameraCapture;
        public TouchField touchField;
        public ModelConfigEntry config;

        private bool _isInit = false;


        private void Update()
        {
            if (!ResMgr.IsLoadFinish)
            {
                return;
            }

            if (!_isInit)
            {
                CreateCamera();
                img.enabled = true;
                _isInit = true;
            }

            if (!touchField.GetSwipeDirection.IsZore())
            {
                float speed = ConfigMgr.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 1);
                cameraCapture.Model.transform.localEulerAngles -=
                    Vector3.up * (Time.deltaTime * touchField.GetSwipeDirection.x * speed);
            }

            SetImage();
        }

        private void SetImage()
        {
            Texture texture = cameraCapture.CaptureImage();
            img.texture = texture;
        }

        private void CreateCamera()
        {
            GameObject cameraCaptureObject = ResMgr.LoadInstan(CameraCapture.PrefabPath);
            cameraCapture = cameraCaptureObject.GetComponent<CameraCapture>();
            cameraCapture.SetRenderTextureSize((int)img.rectTransform.sizeDelta.x, (int)img.rectTransform.sizeDelta.y);
            cameraCaptureObject.transform.position = new Vector3(0, -200, 0);

            var loadedModel = Role.LoadModelByKey(config.roleKey);
            loadedModel.transform.SetParent(cameraCapture.transform, false);

            loadedModel.transform.localPosition = config.localPosition;
            loadedModel.transform.localEulerAngles = config.localEulerAngles;
            loadedModel.transform.localScale = config.size * Vector3.one;
            cameraCapture.Model = loadedModel;

            if (!string.IsNullOrEmpty(config.anim))
            {
                string path = XCPathConfig.GetAnimatorControllerPath(config.anim);
                var loadAc = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
                loadedModel.GetComponent<Animator>().runtimeAnimatorController = loadAc;
            }
        }

        [Button]
        public void ReadConfig()
        {
            var loadedModel = cameraCapture.Model;
            config.localPosition = loadedModel.transform.localPosition;
            config.localEulerAngles = loadedModel.transform.localEulerAngles;
            config.size = loadedModel.transform.localScale.x;
        }
    }
}