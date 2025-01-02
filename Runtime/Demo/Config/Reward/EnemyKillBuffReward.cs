using System;
using UnityEngine;

namespace XiaoCao
{
    //虚假的配置文件,
    [Serializable]
    public class BaseEnemyKillBuffReward : ScriptableObject, IKey
    {
        public string key;
        public string Key => key;


        public Item GetRewardItem(int level)
        {
            EBuffType type = GetRandomBuffType(level);
            string id = $"#{((int)type)}";
            Item item = new Item(ItemType.Buff, id);
            return item;
        }

        public EBuffType GetRandomBuffType(int level)
        {
            float p = 0.1f;
            if (level == 1)
            {
                p = 0.5f;
            }
            else if (level > 1)
            {
                p = 1;
            }
            bool isOther = RandomHelper.GetRandom(p);
            return isOther ? EBuffType.Other : EBuffType.Nor;
        }
    }
}
