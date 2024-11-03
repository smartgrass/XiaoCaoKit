using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XiaoCao
{
    /// <summary>
    /// 循环池,适用于音效
    /// </summary>
    public class LoopPool<T> where T : Object
    {
        public Transform parent;

        public T prefab;

        public int Max = 5; //大于0

        public int curIndex;

        public int CurCount; //当前生成数

        List<T> totalList = new List<T>();

        public LoopPool(T prefab, int Max, Transform parent)
        {
            this.prefab = prefab;
            this.Max = Max;
            this.parent = parent;
        }

        public T Get()
        {
            if (CurCount < Max)
            {
                T newOne = Object.Instantiate(prefab,parent);
                totalList.Add(newOne);
                CurCount++;
                curIndex++;
                return newOne;
            }

            if (curIndex >= Max)
            {
                curIndex = curIndex % Max;
            }
            return totalList[curIndex++];
        }

        public T CheckGet(out bool isNew)
        {
            isNew = CurCount < Max;
            var get = Get();
            if (get == null)
            {
                Debug.LogError("--- loop pool get null ");
            }
            return Get();
        }


        internal void ClearNull()
        {
            List<int> indexList = new();

            for (int i = 0; i < totalList.Count; i++)
            {
                var element = totalList[i];
                if (element == null)
                {
                    indexList.Add(i);
                }
            }

            for (int i = indexList.Count - 1; i >= 0; i--)
            {
                indexList.RemoveAt(i);
            }

            CurCount = CurCount - indexList.Count;
        }
    }
}
