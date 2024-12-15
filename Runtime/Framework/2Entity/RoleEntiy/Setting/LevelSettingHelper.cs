using cfg;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    [XCHelper]
    public class LevelSettingHelper
    {

        internal static string GetText(int v)
        {
            return LubanTables.GetLevelSetting(v).Title;
            //return $"level{v}";
        }

        /// <summary>
        /// 章节就是关卡号的百位数
        /// </summary>
        /// <returns></returns>
        public static int GetChapterByLevel(int level)
        {
            return level % 100 + 1;
        }

    }

    public class LevelRewardData
    {
        //默认奖励等级
        public int RewardLevel { get; set; }



    }

    public static class RewardHelper
    {
        public static void RewardBuffFromPool(int killerId, string rewardPoolId, int rewardLevel = -1)
        {
            if (rewardLevel < 0)
            {
                rewardLevel = BattleData.Current.levelRewardData.RewardLevel;
            }
            Debug.Log($"--- RewardBuffFromPool {killerId} {rewardPoolId}");

            EnemyKillRewardSo enemyKillRewardSo = ConfigMgr.enemyKillRewardSo;
            //获取奖励池
            var rewardPool = enemyKillRewardSo.GetOrDefault(rewardPoolId);
            //获取一个buff
            BuffItem buffItem = rewardPool.GenRandomBuffItem(rewardLevel);
            //添加到角色上->BattleData
            PlayerHelper.AddBuff(killerId, buffItem);
        }
    }
}
