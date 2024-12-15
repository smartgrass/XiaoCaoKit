using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class EnemyKillRewardSo : KeyMapSo<EnemyKillBuffReward>
    {
        [Button]
        void AddEach()
        {
            List<EnemyKillBuffReward> list = new List<EnemyKillBuffReward>();
            EnemyKillBuffReward reward = new EnemyKillBuffReward();
            reward.key = "0";

            List<BuffInfo> buffs = new List<BuffInfo>();
            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (item == EBuff.None)
                {
                    continue;
                }
                BuffInfo buffInfo = new BuffInfo();
                buffInfo.eBuff = item;
                buffInfo.power = 1;
                buffInfo.addInfo = new[] { 1f };
                buffs.Add(buffInfo);
            }
            reward.buffs = buffs;
            list.Add(reward);
            if (this.array.Length == 0)
            {
                this.array = list.ToArray();
            }
            else
            {
                array[0] = reward;
            }
            Debuger.Log($"--- Do");
        }
    }


    //非运行时数据
    [Serializable]
    public class EnemyKillBuffReward : IKey
    {
        public string key;
        public string Key => key;

        [SerializeField]
        public List<BuffInfo> buffs;

        /// <summary>
        /// 抽取随机BuffItem
        /// </summary>
        /// <param name="level">决定词条数量</param>
        public BuffItem GenRandomBuffItem(int level)
        {
            //0级为1个
            return BuffItemHelper.GenRandomBuffItem(level, buffs);
        }
    }
}
