using System.Collections.Generic;
using UnityEngine;
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
            else
            {
                childCount = parent.childCount;
                for (int i = showCount; i < childCount; i++)
                {
                    GameObject go = parent.GetChild(i).gameObject;
                    go.SetActive(false);
                }
            }
        }
    }
}