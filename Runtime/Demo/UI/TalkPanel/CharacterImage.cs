using MFPC;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;


namespace XiaoCao
{
    public class CharacterImage : MonoBehaviour
    {
        /// <summary>抬手姿势的 animPose 取值</summary>
        public const string YesHandPose = "yesHand";

        /// <summary>抬手姿势使用的动画机 key,对应 Assets/_Res/Role/AnimController/npc_idle.controller</summary>
        private const string YesHandControllerKey = "npc_idle";

        /// <summary>切到抬手姿势的 Animator 触发名</summary>
        private const string YesHandTrigger = "idle2Trigger";

        public RawImage img;
        public CameraCapture cameraCapture;
        public TouchField touchField;
        [Label("角色UI相机配置")]
        public ModelConfigEntry config;

        [Label("角色展示姿势")]
        [Tooltip("留空走 config.anim 的默认动画机;填 yesHand 时改用 npc_idle 动画机并触发抬手姿势")]
        public string animPose;

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
                EnsureCameraReady();
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

        /// <summary>
        /// 切换展示的角色模型：销毁旧的拍照相机后，交由统一入口重建。
        /// </summary>
        public void ChangeModelKey(string key)
        {
            if (cameraCapture)
            {
                Destroy(cameraCapture.gameObject);
                cameraCapture = null;
            }

            config.roleKey = key;
            // 重置初始化标记,保证重建走 EnsureCameraReady 这一条创建路径
            _isInit = false;
            EnsureCameraReady();
        }

        /// <summary>
        /// 拍照相机的唯一创建入口:未初始化时才创建,并同时打开角色图显示。
        /// Update 与 ChangeModelKey 都调用这里,避免各自创建出一个相机。
        /// </summary>
        private void EnsureCameraReady()
        {
            if (_isInit)
            {
                return;
            }

            CreateCamera();
            img.enabled = true;
            _isInit = true;
        }

        /// <summary>
        /// 判断角色展示是否已经完成初始化，供 Home 新手引导等待稳定后再显示。
        /// </summary>
        public bool IsReadyForGuide()
        {
            return ResMgr.IsLoadFinish &&
                   _isInit &&
                   img != null &&
                   img.enabled &&
                   img.texture != null &&
                   cameraCapture != null &&
                   cameraCapture.Model != null;
        }
        
        /// <summary>
        /// 创建拍照相机,并按配置摆放角色模型与相机。
        /// </summary>
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

            Animator animator = loadedModel.GetComponent<Animator>();
            if (!string.IsNullOrEmpty(config.anim))
            {
                string path = XCPathConfig.GetAnimatorControllerPath(config.anim);
                animator.runtimeAnimatorController = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
            }

            //抬手姿势:换用 NPC 待机动画机,并触发到 yesHand 状态
            if (animPose == YesHandPose)
            {
                string path = XCPathConfig.GetAnimatorControllerPath(YesHandControllerKey);
                animator.runtimeAnimatorController = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
                animator.SetTrigger(YesHandTrigger);

                //抬手姿势不带武器:隐藏武器挂点(Npc.SetWeapon(false) 同款处理)
                WeaponHelper.FindWeapon(animator).gameObject.SetActive(false);
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
