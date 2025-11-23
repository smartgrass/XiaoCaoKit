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
        [Label("角色UI相机配置")]
        public ModelConfigEntry config;

        private bool _isInit = false;


        private void Update()
        {
            if (!ResMgr.IsLoadFinish)
            {
                return;
            }

            if (string.IsNullOrEmpty(config.roleKey))
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
                float speed = ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 1);
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

        public void ChangeModelKey(string key)
        {
            if (cameraCapture)
            {
                Destroy(cameraCapture.gameObject);
            }
            config.roleKey = key;
            CreateCamera();
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
            
            cameraCapture.captureCamera.transform.localPosition = config.cameraLocalPosition;
            cameraCapture.captureCamera.transform.localEulerAngles = config.cameraLocalEulerAngles;
            
            cameraCapture.Model = loadedModel;
            cameraCapture.characterImage = this;

            if (!string.IsNullOrEmpty(config.anim))
            {
                string path = XCPathConfig.GetAnimatorControllerPath(config.anim);
                var loadAc = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
                loadedModel.GetComponent<Animator>().runtimeAnimatorController = loadAc;
            }
        }

        [Button]
        public void ReadDataToConfig()
        {
            var loadedModel = cameraCapture.Model;
            config.localPosition = loadedModel.transform.localPosition;
            config.localEulerAngles = loadedModel.transform.localEulerAngles;
            config.cameraLocalPosition = cameraCapture.captureCamera.transform.localPosition;
            config.cameraLocalEulerAngles = cameraCapture.captureCamera.transform.localEulerAngles;
            config.size = loadedModel.transform.localScale.x;
        }
    }
}