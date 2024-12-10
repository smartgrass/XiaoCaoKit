using System.Collections.Generic;

namespace XiaoCao
{
    public class EnemyKillRewardSo : KeyMapSo<EnemyKillBuffReward>
    {
        
    }


    //非运行时数据
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
