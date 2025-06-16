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

        public string LevelName { get => GameDataCommon.Current.MapName; }

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

        public static Item GetItemWithPool(string rewardPoolId, int rewardLevel = -1)
        {
            if (rewardLevel < 0)
            {
                rewardLevel = BattleData.Current.levelData.RewardLevel;
            }

            RewardPoolSo so = ConfigMgr.enemyKillRewardSo;
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

            EBuff eBuff;
            if (item.id[0] == '#')
            {
                //根据类型抽取
                var valueString = item.id.Substring(1);
                int.TryParse(valueString, out int num);
                EBuffType eBuffType = (EBuffType)num;
                eBuff = BuffHelper.GetRandomBuff(eBuffType);
            }
            else
            {
                //直接转数字
                int.TryParse(item.id, out int num);
                eBuff = (EBuff)num;
            }

            var buffItem = BuffHelper.CreatBuffItem(eBuff);
            PlayerHelper.AddBuff(GameDataCommon.Current.LocalPlayerId, buffItem);
            Debug.Log($"--- AddBuff {buffItem.buffs[0].eBuff}");
        }
    }
}
