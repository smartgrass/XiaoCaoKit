using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class PutLayout : MonoBehaviour
{
    // 子物体的摆放模式：沿圆弧排列，或按网格排列。
    public enum LayoutMode
    {
        Arc,
        Grid
    }

    // 圆弧布局时，子物体面朝方向的控制方式。
    public enum FacingMode
    {
        Keep,
        TowardCenter,
        AwayFromCenter
    }

    [Serializable]
    public class ArcLayoutSettings
    {
        // 圆心相对当前节点局部坐标系的偏移。
        public Vector3 centerOffset = Vector3.zero;
        // 子物体距离圆心的半径。
        public float radius = 3f;
        // 第一个子物体对应的起始角度，单位为度。
        public float startAngle = 0f;
        // 总角度范围；360 表示完整一圈，负值表示反方向排布。
        [Range(-360f, 360f)] public float angleRange = 360f;
        // 圆所在平面的“横向”轴。
        public Vector3 horizontalAxis = Vector3.right;
        // 圆所在平面的“纵向”轴。
        public Vector3 verticalAxis = Vector3.forward;
        // 是否在布局后调整朝向。
        public FacingMode facingMode = FacingMode.Keep;
    }

    [Serializable]
    public class GridLayoutSettings
    {
        // 网格原点相对当前节点局部坐标系的偏移。
        public Vector3 originOffset = Vector3.zero;
        // 每行（或每列）最多放多少个，具体取决于 lineIsHorizontal。
        [Min(1)] public int countPerLine = 5;
        // true 表示优先横向换行，false 表示优先纵向换列。
        public bool lineIsHorizontal = true;
        // 是否让整个网格围绕原点居中，而不是从原点单侧展开。
        public bool center = true;
        // 网格横向间距。
        public float horizontalSpacing = 1f;
        // 网格纵向间距。
        public float verticalSpacing = 1f;
        // 网格横向使用的局部轴。
        public Vector3 horizontalAxis = Vector3.right;
        // 网格纵向使用的局部轴。
        public Vector3 verticalAxis = Vector3.forward;
    }

    // 当前启用哪种布局模式。
    [SerializeField] private LayoutMode layoutMode = LayoutMode.Arc;
    // 开启后，在启用组件或子节点变化时自动重新排布。
    [SerializeField] private bool autoLayout = true;
    // 是否把未激活的子物体也纳入布局计算。
    [SerializeField] private bool includeInactive = true;
    // 保留子物体在布局平面法线方向上的原始深度，避免全部压到同一平面。
    [SerializeField] private bool preservePlaneDepth = true;
    [SerializeField] private ArcLayoutSettings arc = new ArcLayoutSettings();
    [SerializeField] private GridLayoutSettings grid = new GridLayoutSettings();

    // 复用列表，避免每次布局时产生新的 GC。
    private readonly List<Transform> _children = new List<Transform>();

    private void OnEnable()
    {
        // 编辑器和运行时启用组件时都可以立即预览布局效果。
        if (autoLayout)
        {
            DoLayout();
        }
    }

    private void OnTransformChildrenChanged()
    {
        // 子节点数量或层级变化时自动刷新布局。
        if (autoLayout)
        {
            DoLayout();
        }
    }

    [Button]
    public void DoLayout()
    {
        // 先收集需要参与布局的直接子节点。
        CollectChildren(_children);
        if (_children.Count == 0)
        {
            return;
        }

        switch (layoutMode)
        {
            case LayoutMode.Arc:
                LayoutArc(_children);
                break;
            case LayoutMode.Grid:
                LayoutGrid(_children);
                break;
        }
    }

    private void CollectChildren(List<Transform> children)
    {
        children.Clear();

        // 这里只处理直接子节点，不递归处理更深层级。
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!includeInactive && !child.gameObject.activeSelf)
            {
                continue;
            }

            children.Add(child);
        }
    }

    private void LayoutArc(List<Transform> children)
    {
        // 根据两个轴构建布局平面的正交基，后续角度计算都在这个平面里进行。
        GetPlaneBasis(arc.horizontalAxis, arc.verticalAxis, out Vector3 horizontal, out Vector3 vertical, out Vector3 normal);

        int count = children.Count;
        // 完整圆环时首尾不重复占位；非完整圆弧时两端都要落点。
        bool fullCircle = Mathf.Abs(arc.angleRange) >= 359.999f;
        float step = fullCircle ? arc.angleRange / count : (count > 1 ? arc.angleRange / (count - 1) : 0f);
        Vector3 center = arc.centerOffset;

        for (int i = 0; i < count; i++)
        {
            Transform child = children[i];
            float angle = arc.startAngle + step * i;
            float radian = angle * Mathf.Deg2Rad;

            // 在布局平面内，用 cos/sin 把角度映射到圆周坐标。
            Vector3 planarPosition = center +
                                     horizontal * (Mathf.Cos(radian) * arc.radius) +
                                     vertical * (Mathf.Sin(radian) * arc.radius);

            // 可选地保留原本离开布局平面的深度，让布局只影响平面内位置。
            float depth = preservePlaneDepth ? Vector3.Dot(child.localPosition, normal) : 0f;
            child.localPosition = planarPosition + normal * depth;

            // 让物体朝向圆心或背离圆心；Keep 模式下不改旋转。
            ApplyFacing(child, center - planarPosition, normal, arc.facingMode);
        }
    }

    private void LayoutGrid(List<Transform> children)
    {
        // 网格布局同样基于一个自定义平面，而不是固定的 XZ 平面。
        GetPlaneBasis(grid.horizontalAxis, grid.verticalAxis, out Vector3 horizontal, out Vector3 vertical, out Vector3 normal);

        int count = children.Count;
        int countPerLine = Mathf.Max(1, grid.countPerLine);

        // 横向优先时，countPerLine 表示每行数量；纵向优先时，表示每列数量。
        int horizontalCount = grid.lineIsHorizontal
            ? Mathf.Min(countPerLine, count)
            : Mathf.CeilToInt(count / (float)countPerLine);

        int verticalCount = grid.lineIsHorizontal
            ? Mathf.CeilToInt(count / (float)countPerLine)
            : Mathf.Min(countPerLine, count);

        // 居中布局时，把索引原点平移到网格中心；否则从 0 开始向正方向展开。
        float horizontalCenter = grid.center ? (horizontalCount - 1) * 0.5f : 0f;
        float verticalCenter = grid.center ? (verticalCount - 1) * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            Transform child = children[i];

            int horizontalIndex;
            int verticalIndex;

            if (grid.lineIsHorizontal)
            {
                horizontalIndex = i % countPerLine;
                verticalIndex = i / countPerLine;
            }
            else
            {
                // 纵向优先时，先填满一列，再进入下一列。
                verticalIndex = i % countPerLine;
                horizontalIndex = i / countPerLine;
            }

            Vector3 planarPosition = grid.originOffset +
                                     horizontal * ((horizontalIndex - horizontalCenter) * grid.horizontalSpacing) +
                                     vertical * ((verticalIndex - verticalCenter) * grid.verticalSpacing);

            // 与圆弧布局一致，只调整平面内坐标，法线方向深度按需保留。
            float depth = preservePlaneDepth ? Vector3.Dot(child.localPosition, normal) : 0f;
            child.localPosition = planarPosition + normal * depth;
        }
    }

    private static void GetPlaneBasis(Vector3 horizontalAxis, Vector3 verticalAxis, out Vector3 horizontal, out Vector3 vertical, out Vector3 normal)
    {
        // 若传入轴长度过小，则退回默认轴，避免归一化出错。
        horizontal = horizontalAxis.sqrMagnitude > 0.0001f ? horizontalAxis.normalized : Vector3.right;
        vertical = verticalAxis.sqrMagnitude > 0.0001f ? verticalAxis.normalized : Vector3.forward;

        normal = Vector3.Cross(horizontal, vertical);
        if (normal.sqrMagnitude <= 0.0001f)
        {
            // 两轴几乎平行时，尝试替换一个稳定的备用轴来重建平面。
            vertical = Vector3.forward;
            normal = Vector3.Cross(horizontal, vertical);
            if (normal.sqrMagnitude <= 0.0001f)
            {
                vertical = Vector3.up;
                normal = Vector3.Cross(horizontal, vertical);
            }
        }

        normal.Normalize();
        // 重新计算一次 vertical，确保 horizontal / vertical / normal 互相正交。
        vertical = Vector3.Cross(normal, horizontal).normalized;
    }

    private static void ApplyFacing(Transform child, Vector3 towardCenter, Vector3 up, FacingMode facingMode)
    {
        if (facingMode == FacingMode.Keep)
        {
            return;
        }

        Vector3 forward = facingMode == FacingMode.TowardCenter ? towardCenter : -towardCenter;
        // 朝向只保留在布局平面内，避免因为法线分量导致物体抬头或低头。
        forward = Vector3.ProjectOnPlane(forward, up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        child.localRotation = Quaternion.LookRotation(forward.normalized, up);
    }
}
