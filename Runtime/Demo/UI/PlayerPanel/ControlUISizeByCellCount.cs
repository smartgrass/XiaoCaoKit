using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 根据子节点中显示的 cell 数量调整自身 UI 高度。
    /// </summary>
    public class ControlUISizeByCellCount : MonoBehaviour
    {
        public Transform cellRoot;
        public float minHeight = 100;
        public float maxHeight = 1500;
        public float cellHeight = 20;

        public float borderHeight = 100;

        [Button("刷新高度")]
        private void UpdateSize()
        {
            if (cellRoot == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByCellCount)} 未绑定 cellRoot。", this);
                return;
            }

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByCellCount)} 必须挂载在 RectTransform 上。", this);
                return;
            }

            int cellCount = 0;
            for (int i = 0; i < cellRoot.childCount; i++)
            {
                if (cellRoot.GetChild(i).gameObject.activeSelf)
                {
                    cellCount++;
                }
            }

            float targetHeight = Mathf.Clamp(
                borderHeight + cellCount * cellHeight,
                minHeight,
                maxHeight);

            Vector2 size = rectTransform.sizeDelta;
            size.y = targetHeight;
            rectTransform.sizeDelta = size;
        }

        [Button("自动计算当前行高")]
        private void ReadCurrentConfig()
        {
            if (cellRoot == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByCellCount)} 未绑定 cellRoot。", this);
                return;
            }

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogWarning($"{nameof(ControlUISizeByCellCount)} 必须挂载在 RectTransform 上。", this);
                return;
            }

            int cellCount = 0;
            float currentCellHeight = 0;
            for (int i = 0; i < cellRoot.childCount; i++)
            {
                Transform cell = cellRoot.GetChild(i);
                if (!cell.gameObject.activeSelf)
                {
                    continue;
                }

                cellCount++;
                if (currentCellHeight <= 0 && cell is RectTransform cellRectTransform)
                {
                    currentCellHeight = cellRectTransform.rect.height;
                }
            }

            if (currentCellHeight > 0)
            {
                cellHeight = currentCellHeight;
            }

            borderHeight = rectTransform.rect.height - cellCount * cellHeight;
        }
    }
}
