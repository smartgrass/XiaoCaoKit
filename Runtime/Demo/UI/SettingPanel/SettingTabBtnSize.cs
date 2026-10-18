using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 按 tab 按钮数量调整 GridLayoutGroup 的 cell 高度：
    /// 数量不超过 fitCount（默认 3 个）时用 fitHeight（200），超过时用 overflowHeight（150）。
    ///
    /// 只改运行时布局，不改 prefab 资源：cell 高度写在 GridLayoutGroup 上，
    /// 如果容器上还有 LayoutElement（Tabs 的 minHeight 是 660，会把 3 个 tab 撑出空白），
    /// 顺手把它改成实际内容高度，滚动区域的高度才能跟着收回来。
    ///
    /// 用法：挂在带 GridLayoutGroup 的 Tabs 上，或在 SettingPanel.Init 里 SettingTabBtnSize.ApplyTo(TABS)。
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingTabBtnSize : MonoBehaviour
    {
        [Header("绑定")]
        [Tooltip("不填就用自己身上的 GridLayoutGroup")]
        [SerializeField] private GridLayoutGroup grid;

        [Header("高度规则")]
        [Tooltip("tab 数量不超过该值时用 Fit Height，超过时用 Overflow Height")]
        public int fitCount = 3;

        [Tooltip("3 个 tab 时的 cell 高度")]
        public float fitHeight = 200f;

        [Tooltip("超过 3 个 tab 时的 cell 高度")]
        public float overflowHeight = 150f;

        [Header("刷新")]
        [Tooltip("运行时发现 tab 数量变化后自动重新计算")]
        public bool autoUpdate = true;

        [Tooltip("在 Console 打印每次的计算结果")]
        public bool logDebug = false;

        private int _lastCount = -1;

        private void OnEnable()
        {
            _lastCount = -1;

            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            if (!autoUpdate)
            {
                return;
            }

            //数量变了才重算，平时不做事
            if (GetGrid() != null && GetTabCount() != _lastCount)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 按当前 tab 数量重新计算 cell 高度。
        /// </summary>
        [Button("刷新高度")]
        public void Refresh()
        {
            GridLayoutGroup target = GetGrid();
            if (target == null)
            {
                Debug.LogWarning($"{nameof(SettingTabBtnSize)} 没找到 GridLayoutGroup，请挂在 Tabs 上或手动绑定。", this);
                return;
            }

            int count = GetTabCount();
            _lastCount = count;

            if (count <= 0)
            {
                if (logDebug)
                {
                    Debug.LogWarning($"{nameof(SettingTabBtnSize)} 下面没有 tab 按钮，跳过计算。", this);
                }

                return;
            }

            float targetHeight = count > fitCount ? overflowHeight : fitHeight;

            //只改高度，宽度保持 prefab 里配好的值
            target.cellSize = new Vector2(target.cellSize.x, targetHeight);

            SyncContainerHeight(target, targetHeight, count);

            if (logDebug)
            {
                Debug.Log($"{nameof(SettingTabBtnSize)} 数量={count} cell高={targetHeight}", this);
            }
        }

        /// <summary>
        /// 给 Tabs 动态挂上组件并立即生效（运行时 AddComponent，不会改到 prefab）。
        /// </summary>
        public static SettingTabBtnSize ApplyTo(Transform tabsRoot)
        {
            if (tabsRoot == null)
            {
                return null;
            }

            //没配 GridLayoutGroup 就不处理，交给 prefab 自己原来的布局
            if (tabsRoot.GetComponent<GridLayoutGroup>() == null)
            {
                return null;
            }

            SettingTabBtnSize component = tabsRoot.GetComponent<SettingTabBtnSize>();
            if (component == null)
            {
                component = tabsRoot.gameObject.AddComponent<SettingTabBtnSize>();
            }

            component.Refresh();
            return component;
        }

        private GridLayoutGroup GetGrid()
        {
            if (grid == null)
            {
                grid = GetComponent<GridLayoutGroup>();
            }

            return grid;
        }

        /// <summary>
        /// 当前显示的 tab 数量（不统计隐藏的）。
        /// </summary>
        private int GetTabCount()
        {
            GridLayoutGroup target = GetGrid();
            Transform root = target != null ? target.transform : transform;

            int count = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 把容器高度同步成 行数 * cell高 + 间距，避免 LayoutElement 的旧高度把滚动内容撑高。
        /// </summary>
        private void SyncContainerHeight(GridLayoutGroup target, float cellHeight, int count)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                return;
            }

            int perLine = count;
            if (target.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                perLine = Mathf.Max(1, target.constraintCount);
            }
            else if (target.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                int rowCount = Mathf.Max(1, target.constraintCount);
                perLine = Mathf.Max(1, Mathf.CeilToInt(count / (float)rowCount));
            }

            int lineCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, perLine)));

            float contentHeight = lineCount * cellHeight
                                  + Mathf.Max(0, lineCount - 1) * target.spacing.y
                                  + target.padding.top + target.padding.bottom;

            element.minHeight = contentHeight;
            element.preferredHeight = contentHeight;

            LayoutRebuilder.ForceRebuildLayoutImmediate(target.transform as RectTransform);
        }
    }
}
