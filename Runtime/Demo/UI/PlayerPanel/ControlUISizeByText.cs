using NaughtyAttributes;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCao
{
    /// <summary>
    /// 让自身高度跟随文本内容：先把每个文本撑到自身期望高度（避免文本互相重叠），
    /// 再把文本高度求和后加上固定高度，得到自身高度，且不低于 minHeight。
    /// 挂在需要随文本变高的背景节点上。
    /// </summary>
    public class ControlUISizeByText : MonoBehaviour
    {
        [Tooltip("勾选后每帧自动刷新；不勾选时交给外部（如 NorItemDetailUI）在文本改完后调用 Refresh")]
        public bool isUpdateUI = true;

        [Tooltip("参与高度计算的文本，按从上到下的顺序排列")]
        public TMP_Text[] texts;

        [Tooltip("除文本外的固定高度，如标题区与上下留白")]
        public float extraHeight = 200;

        [Tooltip("文本之间的间距")]
        public float spacing = 10;

        [Tooltip("高度下限，文本很短时高度也不会小于该值")]
        public float minHeight = 330;

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            if (!isUpdateUI)
            {
                return;
            }

            //文本内容变化后高度自动跟随，不需要外部通知；未绑定文本时不处理，避免重复提示
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            Refresh();
        }

        /// <summary>
        /// 重新计算并应用高度：先修正每个文本自身的高度，再取固定高度 + 文本高度之和，
        /// 且不低于 minHeight。
        /// </summary>
        [Button("刷新高度")]
        public void Refresh()
        {
            if (!CheckTextsBound())
            {
                return;
            }

            bool changed = FitTextHeights();
            changed |= ApplyHeight((RectTransform)transform, Mathf.Max(minHeight, extraHeight + GetTextTotalHeight()));

            if (changed)
            {
                //高度变了要让父级布局组重新排一次，两个文本才会按新高度依次排开，不会重叠
                for (int i = 0; i < texts.Length; i++)
                {
                    UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(texts[i].rectTransform);
                }
            }
        }

        /// <summary>
        /// 按当前高度反推固定高度，方便在编辑器里摆好位置后直接取值。
        /// </summary>
        [Button("反推固定高度")]
        private void ReadExtraHeight()
        {
            if (!CheckTextsBound())
            {
                return;
            }

            RectTransform rectTransform = (RectTransform)transform;
            extraHeight = rectTransform.rect.height - GetTextTotalHeight();
        }

        /// <summary>
        /// 检查文本是否已绑定，没绑定时只提示不写兜底高度。
        /// </summary>
        private bool CheckTextsBound()
        {
            if (texts == null || texts.Length == 0)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByText)} 未绑定 texts。", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 文本高度之和，含文本之间的间距，不含固定高度。
        /// </summary>
        private float GetTextTotalHeight()
        {
            float textHeight = spacing * (texts.Length - 1);
            for (int i = 0; i < texts.Length; i++)
            {
                //按文本自身的期望高度计算，文本变长或换行时都能跟随
                textHeight += UnityEngine.UI.LayoutUtility.GetPreferredHeight(texts[i].rectTransform);
            }

            return textHeight;
        }

        /// <summary>
        /// 把每个文本撑到自身期望高度，文字变长或换行后不会溢出自己的矩形去压到相邻文本。
        /// </summary>
        private bool FitTextHeights()
        {
            bool changed = false;
            for (int i = 0; i < texts.Length; i++)
            {
                RectTransform textRect = texts[i].rectTransform;
                changed |= ApplyHeight(textRect, UnityEngine.UI.LayoutUtility.GetPreferredHeight(textRect));
            }

            return changed;
        }

        /// <summary>
        /// 把目标高度写到指定 RectTransform 上：高度按差值调整，并保持顶边不动，
        /// 所以高度变化是往下生长；拉伸锚点与非拉伸锚点都适用。返回是否真的改了高度。
        /// </summary>
        private bool ApplyHeight(RectTransform rectTransform, float targetHeight)
        {
            float delta = targetHeight - rectTransform.rect.height;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return false;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //编辑器下点按钮改高度时支持撤销
                Undo.RecordObject(rectTransform, "Control UI Size By Text");
            }
#endif

            //高度变化时同步挪动坐标，抵消 pivot 带来的偏移，顶边位置保持不变
            rectTransform.anchoredPosition += new Vector2(0, -delta * (1 - rectTransform.pivot.y));
            rectTransform.sizeDelta += new Vector2(0, delta);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //记录成预制体覆盖，方便直接把高度结果存进预制体
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
                EditorUtility.SetDirty(rectTransform);
            }
#endif

            return true;
        }
    }
}
