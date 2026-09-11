using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// Home 页面新手引导的基础容器，保留挂载、初始化和显隐框架，便于后续重做流程。
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeGuideController : MonoBehaviour
    {
        private HomeHud _homeHud;
        private RectTransform _guideRoot;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _guideRoot = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>();
            ResolveHomeHud();
            SetGuideVisibility(false);
        }

        private void Start()
        {
            SetGuideVisibility(false);
        }

        /// <summary>
        /// 由 <see cref="HomeHud"/> 在实例化完成后注入引用。
        /// </summary>
        public void Initialize(HomeHud homeHud)
        {
            _homeHud = homeHud;
            _rootCanvas = homeHud != null ? homeHud.Canvas : _rootCanvas;
            _guideRoot ??= transform as RectTransform;
            _canvasGroup ??= GetComponent<CanvasGroup>();
            SetGuideVisibility(false);
        }

        /// <summary>
        /// 调试入口保留，当前仅重置到隐藏状态。
        /// </summary>
        public void ResetGuideForDebug()
        {
            SetGuideVisibility(false);
        }

        /// <summary>
        /// 调试入口保留，当前不再依赖存档标记。
        /// </summary>
        public static bool IsGuideResetForDebug()
        {
            return false;
        }

        /// <summary>
        /// 调试入口保留，当前仅重置到隐藏状态。
        /// </summary>
        public void CompleteGuideForDebug()
        {
            SetGuideVisibility(false);
        }

        /// <summary>
        /// 保留旧调用点，当前仅确保控制器可用。
        /// </summary>
        public static bool ResetGuideProgressOnly()
        {
            return false;
        }

        /// <summary>
        /// 保留旧调用点，当前仅确保控制器可用。
        /// </summary>
        public static bool CompleteGuideProgressOnly()
        {
            return false;
        }

        /// <summary>
        /// 尝试解析当前 HomeGuideRoot 对应的 HomeHud 引用。
        /// </summary>
        private void ResolveHomeHud()
        {
            if (_homeHud != null)
            {
                return;
            }

            _homeHud = HomeHud.Inst;
            if (_homeHud == null && _rootCanvas != null)
            {
                _homeHud = _rootCanvas.GetComponentInChildren<HomeHud>(true);
            }

            if (_rootCanvas == null && _homeHud != null)
            {
                _rootCanvas = _homeHud.Canvas;
            }
        }

        /// <summary>
        /// 仅控制引导根节点的显示状态，保留后续重建流程的入口。
        /// </summary>
        private void SetGuideVisibility(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
