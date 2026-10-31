using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCaoKit
{
    public static class UITool
    {
        /// <summary>
        /// 复制任意数量的子物体
        /// 少的多生成, 多的隐藏
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="showCount"></param>
        public static void SetCellListCount(Transform parent, int showCount)
        {
            int childCount = parent.childCount;
            if (childCount == 0)
            {
                Debug.LogError($"{parent.name} 缺少用于复制的子物体模板");
                return;
            }

            GameObject prefab = parent.GetChild(0).gameObject;
            int deltaCount = showCount - childCount;
            if (deltaCount > 0)
            {
                for (int i = 0; i < deltaCount; i++)
                {
                    GameObject go = Object.Instantiate(prefab, parent);
                    go.SetActive(true);
                }
            }

            childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject go = parent.GetChild(i).gameObject;
                go.SetActive(i < showCount);
            }
        }

        /// <summary>
        /// 把进度条的填充图归成"左锚点 + 水平零偏移", 这样 anchorMax.x 就等于填充比例。
        /// Sliced 图的 sizeDelta.x 常带着美术调过的缩进值, 不清掉比例会算偏。
        /// </summary>
        public static void NormalizeFillRect(Image fillImage)
        {
            RectTransform rect = fillImage.rectTransform;

            Vector2 anchorMin = rect.anchorMin;
            if (!Mathf.Approximately(anchorMin.x, 0f))
            {
                anchorMin.x = 0f;
                rect.anchorMin = anchorMin;
            }

            Vector2 sizeDelta = rect.sizeDelta;
            if (!Mathf.Approximately(sizeDelta.x, 0f))
            {
                sizeDelta.x = 0f;
                rect.sizeDelta = sizeDelta;
            }
        }

        /// <summary>
        /// 按比例(0~1)设置进度条填充宽度: 改 RectTransform 的 anchorMax.x, 宽度自动跟随父级。
        /// 相比 Image.fillAmount, 这种方式不要求图是 Filled 类型, Sliced/Simple 的九宫格图也能保住两侧圆角。
        /// 注意: 图自身要画满(Filled 类型请把 fillAmount 置 1), 否则 fillAmount 会再乘一次。
        /// </summary>
        public static void SetFillWidth(Image fillImage, float ratio)
        {
            NormalizeFillRect(fillImage);

            RectTransform rect = fillImage.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = Mathf.Clamp01(ratio);
            rect.anchorMax = anchorMax;
        }
    }
}
