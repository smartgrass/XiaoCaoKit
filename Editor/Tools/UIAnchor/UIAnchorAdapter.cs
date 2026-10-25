using UnityEditor;
using UnityEngine;

namespace XiaoCaoEditor
{
    /// <summary>
    /// 单个轴向上的锚点适配方式
    /// </summary>
    public enum AxisAnchorMode
    {
        /// <summary>保持当前锚点, 不做改动</summary>
        Keep = 0,

        /// <summary>按对象与父级的边距自动判断</summary>
        Auto = 1,

        /// <summary>贴住最近的一侧, 尺寸保持不变</summary>
        Pin = 2,

        /// <summary>两端都贴住父级两侧, 边距固定, 尺寸随父级伸缩</summary>
        Stretch = 3,

        /// <summary>锚定父级中心, 尺寸保持不变</summary>
        Center = 4,

        /// <summary>位置与尺寸都按父级比例换算</summary>
        Proportional = 5,
    }

    /// <summary>
    /// 对象相对父级矩形四条边的距离(以父级参考尺寸为单位计算)
    /// </summary>
    public struct AnchorInsets
    {
        public float left;
        public float right;
        public float bottom;
        public float top;
        public float width;
        public float height;
    }

    /// <summary>
    /// 单轴锚点适配的计算结果
    /// </summary>
    public struct AxisAnchorSolution
    {
        public bool apply;          //是否需要改动该轴
        public AxisAnchorMode mode; //实际采用的适配方式
        public float anchorMin;
        public float anchorMax;
        public float offsetMin;
        public float offsetMax;
    }

    /// <summary>
    /// UI 锚点适配的换算与写入。
    /// 只依赖锚点/偏移与父级参考尺寸做纯数学换算, 因此与当前场景分辨率无关,
    /// 换算后只要父级仍是参考尺寸, 对象的视觉位置和大小完全不变。
    /// </summary>
    public static class UIAnchorAdapter
    {
        /// <summary>默认吸附容差(像素), 小于该值的边距视为贴边</summary>
        public const float DefaultTolerance = 4f;

        /// <summary>兜底参考尺寸, 与 UIMgr 画布的 CanvasScaler 参考分辨率保持一致</summary>
        public static readonly Vector2 DefaultReferenceSize = new Vector2(2160f, 1080f);

        /// <summary>占父级该轴长度超过该比例的对象视为可伸缩的大块区域</summary>
        public const float BigBlockRatio = 0.4f;

        /// <summary>两侧留白差异小于该轴长度的这个比例时视为居中</summary>
        public const float CenterRatio = 0.02f;

        /// <summary>
        /// 解析对象在父级参考尺寸下的四边距。
        /// Unity 的 offsetMin/offsetMax 以锚点为参考, 与父级尺寸一起就能还原出真实边距。
        /// </summary>
        public static AnchorInsets GetInsets(RectTransform rt, Vector2 parentSize)
        {
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;

            AnchorInsets insets = new AnchorInsets();
            insets.left = anchorMin.x * parentSize.x + offsetMin.x;
            insets.right = parentSize.x * (1f - anchorMax.x) - offsetMax.x;
            insets.bottom = anchorMin.y * parentSize.y + offsetMin.y;
            insets.top = parentSize.y * (1f - anchorMax.y) - offsetMax.y;
            insets.width = parentSize.x - insets.left - insets.right;
            insets.height = parentSize.y - insets.bottom - insets.top;
            return insets;
        }

        /// <summary>
        /// 计算单个轴上的锚点方案。
        /// startInset/endInset 分别是对象距离该轴起点(左/下)和终点(右/上)的边距。
        /// 结果中的 offset 已按新锚点反推, 因此原矩形在参考尺寸下保持不变。
        /// </summary>
        public static AxisAnchorSolution SolveAxis(float parentSize, float startInset, float endInset, AxisAnchorMode mode,
            float tolerance)
        {
            AxisAnchorSolution solution = new AxisAnchorSolution();
            if (mode == AxisAnchorMode.Keep || parentSize <= 0f)
            {
                solution.apply = false;
                solution.mode = mode;
                return solution;
            }

            float tol = Mathf.Max(0f, tolerance);
            AxisAnchorMode resolved = mode == AxisAnchorMode.Auto
                ? ResolveAuto(parentSize, startInset, endInset, tol)
                : mode;

            switch (resolved)
            {
                case AxisAnchorMode.Stretch:
                    solution.anchorMin = 0f;
                    solution.anchorMax = 1f;
                    break;

                case AxisAnchorMode.Center:
                    solution.anchorMin = 0.5f;
                    solution.anchorMax = 0.5f;
                    break;

                case AxisAnchorMode.Proportional:
                    //边距已经在容差内时直接归到边上, 避免留下 0.0002 这种脏值
                    solution.anchorMin = startInset <= tol ? 0f : Mathf.Clamp01(startInset / parentSize);
                    solution.anchorMax = endInset <= tol ? 1f : Mathf.Clamp01(1f - endInset / parentSize);
                    break;

                default: //Pin
                    bool pinStart = startInset <= endInset;
                    solution.anchorMin = pinStart ? 0f : 1f;
                    solution.anchorMax = solution.anchorMin;
                    break;
            }

            solution.apply = true;
            solution.mode = resolved;
            solution.offsetMin = startInset - solution.anchorMin * parentSize;
            solution.offsetMax = (parentSize - endInset) - solution.anchorMax * parentSize;
            return solution;
        }

        /// <summary>
        /// 自动判断单轴适配方式: 贴边跟随、大块区域伸缩、居中、或贴最近一侧。
        /// 自动模式不会使用等比比例, 以免 UI 元素被跨分辨率拉伸变形。
        /// </summary>
        private static AxisAnchorMode ResolveAuto(float parentSize, float startInset, float endInset, float tol)
        {
            bool touchStart = startInset <= tol;
            bool touchEnd = endInset <= tol;

            //两侧都贴边, 整条拉满
            if (touchStart && touchEnd)
            {
                return AxisAnchorMode.Stretch;
            }

            //占据父级大部分长度的区域随父级伸缩, 两侧边距保持不变
            float length = parentSize - startInset - endInset;
            if (length >= parentSize * BigBlockRatio)
            {
                return AxisAnchorMode.Stretch;
            }

            //两侧留白接近, 视为居中
            if (Mathf.Abs(startInset - endInset) <= Mathf.Max(tol, parentSize * CenterRatio))
            {
                return AxisAnchorMode.Center;
            }

            return AxisAnchorMode.Pin;
        }

        /// <summary>
        /// 按参考尺寸计算适配方案, 不做写入
        /// </summary>
        public static void Compute(RectTransform rt, Vector2 parentSize, AxisAnchorMode xMode, AxisAnchorMode yMode,
            float tolerance, out AnchorInsets insets, out AxisAnchorSolution x, out AxisAnchorSolution y)
        {
            insets = GetInsets(rt, parentSize);
            x = SolveAxis(parentSize.x, insets.left, insets.right, xMode, tolerance);
            y = SolveAxis(parentSize.y, insets.bottom, insets.top, yMode, tolerance);
        }

        /// <summary>
        /// 计算并写入锚点, 保证在参考尺寸下的视觉位置与大小不变。
        /// 返回 false 表示两个轴都不需要改动。
        /// </summary>
        public static bool Apply(RectTransform rt, Vector2 parentSize, AxisAnchorMode xMode, AxisAnchorMode yMode,
            float tolerance, bool recordUndo)
        {
            AnchorInsets insets;
            AxisAnchorSolution x;
            AxisAnchorSolution y;
            Compute(rt, parentSize, xMode, yMode, tolerance, out insets, out x, out y);

            if (!x.apply && !y.apply)
            {
                return false;
            }

            //先把未改动轴的原值取出来, 后面统一写入
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;

            if (x.apply)
            {
                anchorMin.x = x.anchorMin;
                anchorMax.x = x.anchorMax;
                offsetMin.x = x.offsetMin;
                offsetMax.x = x.offsetMax;
            }

            if (y.apply)
            {
                anchorMin.y = y.anchorMin;
                anchorMax.y = y.anchorMax;
                offsetMin.y = y.offsetMin;
                offsetMax.y = y.offsetMax;
            }

            if (recordUndo)
            {
                Undo.RecordObject(rt, "UI锚点适配");
            }

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            //offsetMin/offsetMax 是同一组数据的两种表达, 依次写入后两者都会生效
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            if (recordUndo)
            {
                EditorUtility.SetDirty(rt);
                if (PrefabUtility.IsPartOfPrefabInstance(rt))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
                }
            }

            return true;
        }
    }
}
