using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace XiaoCao
{
    /// <summary>
    /// 循环池,适用于音效
    /// </summary>
    public class LoopPool<T> where T : Object
    {
        public T prefab;

        public int Max = 5; //大于0

        public int curIndex;

        public int genCount;

        List<T> totalList = new List<T>();

        public LoopPool(T prefab, int Max)
        {
            this.prefab = prefab;
            this.Max = Max;
        }

        public T Get()
        {
            if (genCount < Max)
            {
                T newOne = Object.Instantiate(prefab);
                totalList.Add(newOne);
                genCount++;
                curIndex++;
                return newOne;
            }

            if (curIndex >= Max)
            {
                curIndex = curIndex % Max;
            }
            return totalList[curIndex++];
        }

    }
}
