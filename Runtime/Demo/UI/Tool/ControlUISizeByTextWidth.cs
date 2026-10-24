using NaughtyAttributes;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCao
{
    /// <summary>
    /// 让目标宽度跟随文本内容: 取文本的自然宽度(按当前字号、不换行计算)加上额外留白，
    /// 结果不小于 minWidth、不超过 maxWidth，再写到目标 RectTransform 上。
    /// 宽度按目标 pivot 生长: 左对齐向右长、居中向两边长、右对齐向左长。
    /// 与 ControlUISizeByText(高度) 配对使用，适合标签、气泡、按钮这类随文字变长的底框。
    /// </summary>
    public class ControlUISizeByTextWidth : MonoBehaviour
    {
        [Tooltip("勾选后每帧自动刷新；不勾选时交给外部在文本改完后调用 Refresh")]
        public bool isUpdateUI = true;

        [Tooltip("提供文本宽度的文本")]
        public TMP_Text text;

        [Tooltip("要调整宽度的目标节点，留空则调整自身")]
        public RectTransform target;

        [Tooltip("文本之外的固定宽度，如左右留白、图标")]
        public float extraWidth = 40;

        [Tooltip("宽度下限，文本很短时也不会小于该值")]
        public float minWidth = 100;

        [Tooltip("宽度上限，0 表示不限制(超出时文本可能溢出目标)")]
        public float maxWidth = 0;

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            if (!isUpdateUI || text == null)
            {
                return;
            }

            //文本内容或字号变化后宽度自动跟随，不需要外部通知
            Refresh();
        }

        /// <summary>
        /// 重新计算并应用宽度: 文本自然宽度 + 固定宽度，再按最小/最大宽度收敛。
        /// 文本没绑定或宽度未变化时直接返回，避免每帧重复写入布局。
        /// </summary>
        [Button("刷新宽度")]
        public void Refresh()
        {
            if (text == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByTextWidth)} 未绑定 text。", this);
                return;
            }

            RectTransform rectTransform = GetTarget();
            if (!ApplyWidth(rectTransform, GetTargetWidth()))
            {
                return;
            }

            //宽度变了要让父级布局组重新排一次，目标后面的内容才会跟着挪开
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// 按当前宽度反推固定宽度，方便在编辑器里摆好底框后直接取值。
        /// </summary>
        [Button("反推固定宽度")]
        private void ReadExtraWidth()
        {
            if (text == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByTextWidth)} 未绑定 text。", this);
                return;
            }

            extraWidth = GetTarget().rect.width - text.preferredWidth;
        }

        /// <summary>
        /// 实际被调整宽度的节点: 未指定 target 时用自身。
        /// </summary>
        private RectTransform GetTarget()
        {
            return target != null ? target : (RectTransform)transform;
        }

        /// <summary>
        /// 目标宽度 = 文本自然宽度 + 固定宽度，夹在 [minWidth, maxWidth] 之间。
        /// preferredWidth 由 TMP 按当前文本/字号缓存，文本变化时才会重新计算。
        /// </summary>
        private float GetTargetWidth()
        {
            float width = Mathf.Max(minWidth, text.preferredWidth + extraWidth);
            if (maxWidth > 0)
            {
                width = Mathf.Min(width, maxWidth);
            }

            return width;
        }

        /// <summary>
        /// 把目标宽度写到指定 RectTransform 上: 宽度按差值调整，并抵消 pivot 带来的偏移，
        /// 让 pivot 所在的那条竖边保持不动(左对齐向右长、居中向两边长)。返回是否真的改了宽度。
        /// </summary>
        private bool ApplyWidth(RectTransform rectTransform, float targetWidth)
        {
            float delta = targetWidth - rectTransform.rect.width;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return false;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //编辑器下点按钮改宽度时支持撤销
                Undo.RecordObject(rectTransform, "Control UI Size By Text Width");
            }
#endif

            rectTransform.anchoredPosition += new Vector2(delta * rectTransform.pivot.x, 0);
            rectTransform.sizeDelta += new Vector2(delta, 0);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //记录成预制体覆盖，方便直接把宽度结果存进预制体
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
                EditorUtility.SetDirty(rectTransform);
            }
#endif

            return true;
        }
    }
}
