using cfg;
using OdinSerializer;
using System;
using System.Collections.Generic;
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
        public static void RewardBuffFromPool(int roleId, string rewardPoolId, int rewardLevel = -1)
        {
            if (rewardLevel < 0)
            {
                rewardLevel = BattleData.Current.levelRewardData.RewardLevel;
            }
            Debug.Log($"--- RewardBuffFromPool {roleId} {rewardPoolId}");

            RewardPoolSo so = ConfigMgr.enemyKillRewardSo;
            //获取奖励池
            BaseRewardItemConfigSo rewardPool = so.GetOrDefault(rewardPoolId);
            //背包 pick
            Item item = rewardPool.GetRewardItem(rewardLevel);

            RewardItem(item);
        }

        public static void RewardItem(this Item item)
        {
            //暂时只有本地玩家
            switch (item.type)
            {
                case ItemType.Consumable:
                    break;
                case ItemType.Equipment:
                    break;
                case ItemType.Coin:
                    break;
                case ItemType.Buff:

                    RewardBuff(item);
                    break;
                default:
                    break;
            }

        }

        public static void RewardBuff(Item item)
        {
            if (string.IsNullOrEmpty(item.id))
            {
                Debuger.LogError("--- id = empty");
                return;
            }
            EBuff eBuff;
            if (item.id[0]=='#')
            {
                var valueString = item.id.Substring(1);
                int.TryParse(valueString, out int num);
                EBuffType eBuffType = (EBuffType)num;
                eBuff = BuffHelper.GetRandomBuff(eBuffType);
            }
            else
            {
                int.TryParse(item.id, out int num);
                eBuff = (EBuff)num;
            }

            var buffItem = BuffHelper.CreatBuffItem(eBuff);

            PlayerHelper.AddBuff(GameDataCommon.Current.LocalPlayerId, buffItem);
            Debug.Log($"--- AddBuff {buffItem.buffs[0].eBuff}");
        }
    }
}
