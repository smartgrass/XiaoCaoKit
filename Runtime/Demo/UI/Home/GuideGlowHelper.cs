using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 新手引导发光特效: 可挂到任意按钮/格子上, 与目标矩形完全重合, 排在目标之前绘制
    /// </summary>
    public static class GuideGlowHelper
    {
        //发光呼吸的缩放倍数
        public const float ScaleMult = 1.2f;

        public static GameObject Create(RectTransform target)
        {
            if (target == null)
            {
                return null;
            }

            UIPrefabSo so = UIPrefabSo.Inst != null ? UIPrefabSo.Inst : Resources.Load<UIPrefabSo>("UIPrefabSo");
            if (so == null || so.btnGlowEffect == null)
            {
                return null;
            }

            //放到父节点才能排在目标之前, 否则会盖住目标自身的底图
            Transform parent = target.parent;
            //父节点带布局组时改挂到目标自身, 否则会多出一个布局元素把其它节点挤开
            bool parentHasLayout = parent != null && parent.GetComponent<LayoutGroup>() != null;
            if (parent == null || parentHasLayout)
            {
                parent = target;
            }

            GameObject glow = Object.Instantiate(so.btnGlowEffect, parent);
            glow.name = "GuideGlowEffect";

            ApplyRect(target, glow);
            if (parent == target)
            {
                //挂在自己身上时排最前: 绘制在自身底图之上、图标之下
                glow.transform.SetAsFirstSibling();
            }
            else
            {
                //sibling 越小绘制越靠后
                glow.transform.SetSiblingIndex(target.GetSiblingIndex());
            }

            SimpleImageTween tween = glow.GetComponent<SimpleImageTween>();
            if (tween != null)
            {
                tween.scaleMult = ScaleMult;
            }

            glow.SetActive(true);
            if (tween != null && glow.activeInHierarchy)
            {
                //动画不会自动开始, 需要手动播放
                tween.Play();
            }

            return glow;
        }

        /// <summary>
        /// 让发光与目标矩形完全重合(布局变化后可重复调用)
        /// </summary>
        public static void ApplyRect(RectTransform target, GameObject glow)
        {
            RectTransform rt = glow != null ? glow.transform as RectTransform : null;
            RectTransform parentRt = rt != null ? rt.parent as RectTransform : null;
            if (target == null || rt == null || parentRt == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = parentRt.InverseTransformPoint(corners[0]);
            Vector2 max = parentRt.InverseTransformPoint(corners[2]);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = max - min;
            rt.anchoredPosition = (min + max) * 0.5f - parentRt.rect.min;
        }

        public static void Destroy(GameObject glow)
        {
            if (glow != null)
            {
                Object.Destroy(glow);
            }
        }
    }
}
