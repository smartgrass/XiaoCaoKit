using NaughtyAttributes;
using System;
using System.Collections.Generic;

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
                buffInfo.buff = item;
                buffInfo.AddInfo = new[] { 1f };
                buffs.Add(buffInfo);
            }
            reward.buffs = buffs;
            list.Add(reward);
            if (this.array.Length  == 0)
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

        public List<BuffInfo> buffs;

        public BuffGroup GetBuffGroup(int level)
        {
            return BuffGroupHelper.GetBuffGroup(level, buffs);
        }
    }
}
