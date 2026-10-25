using UnityEngine;

namespace XiaoCaoKit.UI
{
    /// <summary>
    /// 面板横向适配: 屏幕比设计尺寸更宽时保持设计宽度并水平居中不拉伸;
    /// 屏幕更"窄高"时整体缩小 scale, 让面板宽度正好铺满屏幕。
    /// 挂在面板根节点上, 由它改写自身的 RectTransform 与 localScale。
    /// </summary>
    public class UIPanelAspectFit : MonoBehaviour
    {
        [Header("设计尺寸")]
        [Tooltip("面板按这个尺寸排版, 与画布参考分辨率一致")]
        public Vector2 designSize = new Vector2(2160f, 1080f);

        [Header("宽度不足时")]
        [Tooltip("勾选: 缩小后把高度补满屏幕; 不勾: 高度等比缩小, 上下留空")]
        public bool fillHeight = false;

        private RectTransform _rect;
        private Vector2 _lastParentSize;

        private void Awake()
        {
            _rect = transform as RectTransform;
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            //只在父级(画布)尺寸变化时重算, 避免每帧改 RectTransform 引起重建
            if (_rect == null)
            {
                return;
            }

            RectTransform parent = _rect.parent as RectTransform;
            if (parent != null && parent.rect.size != _lastParentSize)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 按父级尺寸重算面板的宽度、居中方式与整体缩放。
        /// </summary>
        public void Refresh()
        {
            //允许在 Awake 之前被外部调用, 这里顺手补一次引用
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (_rect == null)
            {
                return;
            }

            RectTransform parent = _rect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Vector2 parentSize = parent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            float parentAspect = parentSize.x / parentSize.y;
            float designAspect = designSize.x / designSize.y;

            float scale = 1f;
            float height = parentSize.y;

            if (parentAspect < designAspect)
            {
                //屏幕比设计更窄高: 缩小整体, 宽度正好等于屏幕宽度
                scale = Mathf.Min(1f, parentSize.x / designSize.x);
                if (fillHeight)
                {
                    //缩放后高度不足, 把面板本地高度补到缩放前的屏幕高度
                    height = parentSize.y / scale;
                }
            }

            //统一用"中心锚点 + 固定尺寸", 宽屏时宽度就是设计宽度并居中, 高度始终贴满
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(designSize.x, height);
            _rect.localScale = new Vector3(scale, scale, 1f);

            _lastParentSize = parentSize;
        }
    }
}
