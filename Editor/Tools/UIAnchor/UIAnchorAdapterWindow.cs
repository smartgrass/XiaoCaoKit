using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoEditor
{
    /// <summary>
    /// UI 锚点适配工具。
    /// 把一个按绝对坐标摆好的 RectTransform 换算成"贴边 / 拉伸 / 居中 / 比例"锚点,
    /// 换算后当前参考尺寸下的视觉不变, 换到其他分辨率时才会按锚点重新排布。
    /// </summary>
    public class UIAnchorAdapterWindow : EditorWindow
    {
        private static readonly string[] ModeNames =
        {
            "保持不动", "自动判断", "贴边(尺寸不变)", "拉伸(边距固定)", "居中(尺寸不变)", "等比比例",
        };

        [SerializeField] private AxisAnchorMode xMode = AxisAnchorMode.Auto;
        [SerializeField] private AxisAnchorMode yMode = AxisAnchorMode.Auto;
        [SerializeField] private float tolerance = UIAnchorAdapter.DefaultTolerance;
        [SerializeField] private bool includeChildren;
        [SerializeField] private bool useActualParentSize = true;
        [SerializeField] private Vector2 manualParentSize = new Vector2(2160f, 1080f);

        private Vector2 scroll;
        private readonly List<TargetEntry> entries = new List<TargetEntry>();

        /// <summary>预览列表中的一行</summary>
        private class TargetEntry
        {
            public RectTransform rt;
            public Vector2 parentSize;
            public string parentName;
            public AnchorInsets insets;
            public AxisAnchorSolution x;
            public AxisAnchorSolution y;
            public string warning;
        }

        [MenuItem(XCEditorTools.UIAnchorAdapter, priority = 100)]
        public static void Open()
        {
            UIAnchorAdapterWindow window = GetWindow<UIAnchorAdapterWindow>("UI锚点适配");
            window.minSize = new Vector2(460f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawReferenceSize();
            EditorGUILayout.Space(4f);
            DrawModes();
            EditorGUILayout.Space(4f);

            CollectEntries();
            DrawTargets();

            EditorGUILayout.Space(6f);
            DrawApplyButton();
        }

        /// <summary>绘制参考尺寸设置</summary>
        private void DrawReferenceSize()
        {
            EditorGUILayout.LabelField("参考尺寸", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "锚点按父级矩形换算。参考尺寸取父级当前大小即可; 在 Prefab 编辑模式下父级尺寸为 0 时, " +
                "请改成手动填写画布参考分辨率(本项目为 2160 x 1080)。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            useActualParentSize = GUILayout.Toggle(useActualParentSize, "父级实际尺寸", "Button", GUILayout.Height(22f));
            bool manual = !useActualParentSize;
            bool newManual = GUILayout.Toggle(manual, "手动指定", "Button", GUILayout.Height(22f));
            if (newManual != manual)
            {
                useActualParentSize = !newManual;
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(useActualParentSize))
            {
                EditorGUILayout.BeginHorizontal();
                manualParentSize.x = EditorGUILayout.FloatField("宽", manualParentSize.x);
                manualParentSize.y = EditorGUILayout.FloatField("高", manualParentSize.y);
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>绘制适配方式设置</summary>
        private void DrawModes()
        {
            EditorGUILayout.LabelField("适配方式", EditorStyles.boldLabel);
            xMode = (AxisAnchorMode)EditorGUILayout.Popup("横向 X", (int)xMode, ModeNames);
            yMode = (AxisAnchorMode)EditorGUILayout.Popup("纵向 Y", (int)yMode, ModeNames);
            tolerance = EditorGUILayout.FloatField("吸附容差(像素)", tolerance);
            includeChildren = EditorGUILayout.Toggle("包含直接子级", includeChildren);

            EditorGUILayout.HelpBox(
                "自动判断: 两侧都贴边或占父级 40% 以上 -> 拉伸; 留白接近 -> 居中; 其余 -> 贴最近一侧。\n" +
                "等比比例: 位置和尺寸都按父级百分比换算, 适合需要整体缩放的元素。", MessageType.None);
        }

        /// <summary>绘制选中对象的换算结果</summary>
        private void DrawTargets()
        {
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("在 Hierarchy 里选中要适配的 UI 节点(可多选)。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(string.Format("选中 {0} 个对象", entries.Count), EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120f), GUILayout.MaxHeight(360f));
            for (int i = 0; i < entries.Count; i++)
            {
                DrawEntry(entries[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(TargetEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(entry.rt.name, EditorStyles.boldLabel);

            EditorGUILayout.LabelField(string.Format("父级 {0}  {1:0.#} x {2:0.#}", entry.parentName,
                entry.parentSize.x, entry.parentSize.y), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(string.Format("当前  左 {0:0.#}  右 {1:0.#}  下 {2:0.#}  上 {3:0.#}  尺寸 {4:0.#} x {5:0.#}",
                entry.insets.left, entry.insets.right, entry.insets.bottom, entry.insets.top,
                entry.insets.width, entry.insets.height), EditorStyles.miniLabel);

            StringBuilder builder = new StringBuilder();
            builder.Append("结果  X: ").Append(ModeNames[(int)entry.x.mode]);
            if (entry.x.apply)
            {
                builder.Append(string.Format("  anchor({0:0.###})", entry.x.anchorMin));
            }

            builder.Append("  Y: ").Append(ModeNames[(int)entry.y.mode]);
            if (entry.y.apply)
            {
                builder.Append(string.Format("  anchor({0:0.###})", entry.y.anchorMin));
            }

            EditorGUILayout.LabelField(builder.ToString(), EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(entry.warning))
            {
                EditorGUILayout.HelpBox(entry.warning, MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawApplyButton()
        {
            using (new EditorGUI.DisabledScope(entries.Count == 0))
            {
                if (GUILayout.Button("应用到选中对象", GUILayout.Height(30f)))
                {
                    ApplyAll();
                }
            }
        }

        /// <summary>收集选中对象并完成一次换算, 用于预览</summary>
        private void CollectEntries()
        {
            entries.Clear();
            foreach (Transform transform in Selection.transforms)
            {
                RectTransform rt = transform as RectTransform;
                if (rt == null)
                {
                    continue;
                }

                AddEntry(rt);

                if (!includeChildren)
                {
                    continue;
                }

                for (int i = 0; i < rt.childCount; i++)
                {
                    RectTransform child = rt.GetChild(i) as RectTransform;
                    if (child != null)
                    {
                        AddEntry(child);
                    }
                }
            }
        }

        private void AddEntry(RectTransform rt)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].rt == rt)
                {
                    return;
                }
            }

            TargetEntry entry = new TargetEntry();
            entry.rt = rt;
            entry.parentName = rt.parent != null ? rt.parent.name : "(无父节点)";
            entry.parentSize = ResolveParentSize(rt, out bool usedFallback);
            UIAnchorAdapter.Compute(rt, entry.parentSize, xMode, yMode, tolerance,
                out entry.insets, out entry.x, out entry.y);
            entry.warning = GetWarning(rt, usedFallback);
            entries.Add(entry);
        }

        /// <summary>解析对象父级的参考尺寸</summary>
        private Vector2 ResolveParentSize(RectTransform rt, out bool usedFallback)
        {
            usedFallback = false;
            if (!useActualParentSize)
            {
                return manualParentSize;
            }

            Vector2 size = GetActualParentSize(rt, out bool valid);
            if (valid)
            {
                return size;
            }

            usedFallback = true;
            return manualParentSize;
        }

        /// <summary>取父级当前矩形尺寸, 无效时返回画布参考分辨率</summary>
        public static Vector2 GetActualParentSize(RectTransform rt, out bool valid)
        {
            RectTransform parent = rt.parent as RectTransform;
            if (parent != null && parent.rect.width > 0f && parent.rect.height > 0f)
            {
                valid = true;
                return parent.rect.size;
            }

            valid = false;
            CanvasScaler scaler = FindCanvasScaler(rt);
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return scaler.referenceResolution;
            }

            return UIAnchorAdapter.DefaultReferenceSize;
        }

        private static CanvasScaler FindCanvasScaler(RectTransform rt)
        {
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        }

        /// <summary>给出会导致锚点失效的提示</summary>
        private static string GetWarning(RectTransform rt, bool usedFallback)
        {
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null)
            {
                return "没有父节点, 锚点无法生效。";
            }

            if (usedFallback)
            {
                return "父级尺寸为 0(常见于 Prefab 编辑模式), 已按手动参考尺寸换算, 请确认数值是否合理。";
            }

            //LayoutGroup 会把子节点的锚点和位置一起驱动掉, 这种情况改锚点没有意义
            if (parent.GetComponent<LayoutGroup>() != null)
            {
                return "父级挂了 LayoutGroup, 子节点的锚点和位置由它驱动, 这里改了也不生效。";
            }

            return null;
        }

        /// <summary>把预览结果写入选中的对象</summary>
        private void ApplyAll()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("UI锚点适配");

            int changed = 0;
            StringBuilder log = new StringBuilder("[UI锚点适配] 已处理:\n");
            for (int i = 0; i < entries.Count; i++)
            {
                TargetEntry entry = entries[i];
                if (UIAnchorAdapter.Apply(entry.rt, entry.parentSize, xMode, yMode, tolerance, true))
                {
                    changed++;
                    log.Append(string.Format("  {0}  X:{1}  Y:{2}\n", entry.rt.name, ModeNames[(int)entry.x.mode],
                        ModeNames[(int)entry.y.mode]));
                }
            }

            Undo.CollapseUndoOperations(group);

            if (changed > 0)
            {
                Debug.Log(log.ToString());
            }

            Repaint();
        }

        /// <summary>右键 RectTransform 的一键适配入口</summary>
        [MenuItem("CONTEXT/RectTransform/锚点适配/自动", false, 100)]
        private static void ContextAuto(MenuCommand command)
        {
            QuickApply(command.context as RectTransform, AxisAnchorMode.Auto, AxisAnchorMode.Auto);
        }

        [MenuItem("CONTEXT/RectTransform/锚点适配/等比比例", false, 101)]
        private static void ContextProportional(MenuCommand command)
        {
            QuickApply(command.context as RectTransform, AxisAnchorMode.Proportional,
                AxisAnchorMode.Proportional);
        }

        [MenuItem("CONTEXT/RectTransform/锚点适配/左右拉伸", false, 102)]
        private static void ContextStretchX(MenuCommand command)
        {
            QuickApply(command.context as RectTransform, AxisAnchorMode.Stretch, AxisAnchorMode.Pin);
        }

        private static void QuickApply(RectTransform rt, AxisAnchorMode xMode, AxisAnchorMode yMode)
        {
            if (rt == null)
            {
                return;
            }

            Vector2 parentSize = GetActualParentSize(rt, out bool valid);
            if (!valid)
            {
                Debug.LogWarning(string.Format("[UI锚点适配] {0} 的父级尺寸为 0, 请用窗口模式手动指定参考尺寸。", rt.name), rt);
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("UI锚点适配");
            bool changed = UIAnchorAdapter.Apply(rt, parentSize, xMode, yMode, UIAnchorAdapter.DefaultTolerance, true);
            Undo.CollapseUndoOperations(group);

            if (!changed)
            {
                Debug.Log(string.Format("[UI锚点适配] {0} 的锚点已符合目标方式, 未改动。", rt.name), rt);
            }
        }
    }
}
