using UnityEngine;
using System.Collections.Generic;

namespace XiaoCaoKit.UI
{
    /// <summary>
    /// 面板横向适配: 屏幕比设计尺寸更宽时保持设计宽度并水平居中不拉伸;
    /// 屏幕更"窄高"时整体缩小 scale, 让面板宽度正好铺满屏幕。
    /// 挂在面板根节点上, 由它改写自身的 RectTransform 与 localScale;
    /// fitRects 里列出的节点会同步套用同一套结果。
    /// </summary>
    public class UIPanelAspectFit : MonoBehaviour
    {
        [Header("设计尺寸")]
        [Tooltip("面板按这个尺寸排版, 与画布参考分辨率一致")]
        public Vector2 designSize = new Vector2(2160f, 1080f);

        [Header("宽度不足时")]
        [Tooltip("勾选: 缩小后把高度补满屏幕; 不勾: 高度等比缩小, 上下留空")]
        public bool fillHeight = false;

        [Header("同步适配的节点")]
        [Tooltip("跟着面板套用同一套适配结果的 RectTransform(宽高/居中/缩放都和面板一致)。" +
                 "填同级节点, 不要填面板自己的子节点, 否则缩放会叠加两次")]
        public List<RectTransform> fitRects = new List<RectTransform>();

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

            //面板自身先套用适配结果, 再把同一套结果同步给列表里的节点
            ApplyFit(_rect, height, scale);

            for (int i = 0; i < fitRects.Count; i++)
            {
                RectTransform target = fitRects[i];
                if (target)
                {
                    ApplyFit(target, height, scale);
                }
            }

            _lastParentSize = parentSize;
        }

        /// <summary>
        /// 把算好的适配结果写到指定节点: 统一用"中心锚点 + 固定尺寸", 宽屏时宽度就是设计宽度并居中, 高度始终贴满。
        /// </summary>
        private void ApplyFit(RectTransform rect, float height, float scale)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(designSize.x, height);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
