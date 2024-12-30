using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    //TODO 不需要这个脚本,只需要配置类型的概率  
    public class EnemyKillRewardSo : KeyMapSo<EnemyKillBuffReward>
    {

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
            return BuffHelper.GenRandomBuffItem(level, buffs);
        }
    }
}
