using cfg;
using OdinSerializer;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    [XCHelper]
    public class LevelSettingHelper
    {
        public static int GetEnemyLevel(int baseLevel, string levelName = "")
        {
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = BattleData.Current.levelData.LevelName;
            }

            var setting = LubanTables.GetLevelSetting(levelName);
            if (baseLevel < setting.EnemyLvList.Count)
            {
                return setting.EnemyLvList[baseLevel];
            }
            else
            {
                return setting.EnemyLvList[~1];
            }
        }
        internal static string GetText(string levelName)
        {
            return LubanTables.GetLevelSetting(levelName).Title;
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

    public class LevelData
    {
        //默认奖励等级
        public int RewardLevel { get; set; }

        //关卡分支 默认0
        public string LevelBranch = "0";

        public string LevelName { get => GameDataCommon.Current.mapName; }

        public string GetLevelEnemyInfoKey(string groupName)
        {
            return $"{LevelName}/{groupName}/{LevelBranch}";
        }
    }

    [XCHelper]
    public static class RewardHelper
    {
        public static void RewardItem(this Item item)
        {
            //暂时只有本地玩家
            switch (item.type)
            {
                case ItemType.Consumable:
                    break;
                case ItemType.HolyRelic:
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

        public static Item GetItemWithPool(string rewardPoolId, int rewardLevel = -1)
        {
            if (rewardLevel < 0)
            {
                rewardLevel = BattleData.Current.levelData.RewardLevel;
            }

            RewardPoolSo so = ConfigMgr.EnemyKillRewardSo;
            //获取奖励池
            BaseRewardItemConfigSo rewardPool = so.GetOrDefault(rewardPoolId);
            //背包 pick
            Item item = rewardPool.GetRewardItem(rewardLevel);

            return item;
        }

        /// <summary>
        /// item.id 的数字直接对应buff
        /// 如果是#开头,则是根据EBuffType类型抽取
        /// </summary>
        /// <param name="item"></param>
        public static void RewardBuff(Item item)
        {
            if (string.IsNullOrEmpty(item.id))
            {
                Debuger.LogError("--- id = empty");
                return;
            }

            BuffItem buffItem = BuffItem.Create(item);

            PlayerHelper.AddBuff(GameDataCommon.Current.localPlayerId, buffItem);

            Debug.Log($"--- AddBuff {buffItem.buffs[0].eBuff}");

            //Item item = new Item { type = ItemType.Coin, id = "金币", count = 10 };
            //

            GameEvent.Send<Item>(EGameEvent.OnGetItem.Int(), buffItem.ToItem());
        }
        //特殊的转换规则,可以取随机
    }
}
