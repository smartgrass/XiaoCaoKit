using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 抽卡/抽奖随机
    /// </summary>
    ///<see cref="PowerModel"/>
    public static class RandomHelper
    {
        public static T GetRandom<T>(this List<T> powerList, out int index) where T : PowerModel
        {
            index = 0;
            int total = 0;
            foreach (var item in powerList)
            {
                total += item.power;
            }
            if (powerList.Count == 0)
            {
                index = -1;
                return null;
            }
            if (powerList.Count == 1 || total == 0)
            {
                if (total == 0)
                {
                    Debug.LogWarning("total power = 0");
                }
                return powerList[0];
            }

            int random = UnityEngine.Random.Range(0, total);
            int rangeMax = 0;

            int length = powerList.Count;
            for (int i = 0; i < length; i++)
            {
                rangeMax += powerList[i].power;
                //当随机数小于 rangeMax 说明在范围内
                if (rangeMax > random && powerList[i].power > 0)
                {
                    index = i;
                    return powerList[i];
                }
            }
            Debug.LogError("??? power = 0");
            return null;
        }

        public static List<T> GetRandomList<T>(this List<T> powerList,int count) where T : PowerModel
        {
            List<T> list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                GetRandom(powerList,out int index);
                list.Add(powerList[index]);
            }
            return list;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="p">0~1</param>
        /// <returns></returns>
        public static bool GetRandom(float p = 0.5f)
        {
            if (p == 1f)
            {
                return true;
            }

            float random = UnityEngine.Random.Range(0f, 1f);
            return random < p;
        }

        public static float RangeFloat(float from, float to)
        {
            return UnityEngine.Random.Range(from, to);
        }

        public static int Range(int from, int to)
        {
            return UnityEngine.Random.Range(from, to);
        }
    }

    /// <summary>
    /// 对于需要配置概率的物品推荐继承它
    /// </summary>
    [System.Serializable]
    public class PowerModel
    {
        public int power = 1; //权重
    }
}
