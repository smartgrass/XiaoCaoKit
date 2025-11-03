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
        //获取Enemy等级
        public static int GetEnemyLevel(int baseLevel)
        {
            string levelName = GameDataCommon.Current.levelName;
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = "level_0_1";
                Debuger.LogWarning("---! levelName = empty");
            }

            var setting = LubanTables.GetLevelSetting(levelName);
            if (baseLevel < setting.EnemyLvList.Count)
            {
                return setting.EnemyLvList[baseLevel];
            }
            else if (setting.EnemyLvList.Count > 0)
            {
                return setting.EnemyLvList[~1];
            }
            return 0;
        }
        
        public static List<Item> GetReward(string levelName)
        {
            var setting = LubanTables.GetLevelSetting(levelName);
            return setting.Reward;
        }

    }

    public class LevelData
    {
        public static LevelData Current => BattleData.Current.levelData;
        //默认奖励等级
        public int RewardLevel { get; set; }

        //关卡分支
        public string LevelBranch = "";

        public ELevelResult levelResult;
        
        public int killCount;
        
        public float enterLevelTime;
        
        public float finishLevelTime;

        public string LevelName
        {
            get => GameDataCommon.Current.levelName;
        }

        public string GetLevelNameText => MapNames.GetLevelInfoByName(LevelName).GetLevelName();
        public LevelInfo GetLevelInfo => MapNames.GetLevelInfoByName(LevelName);

        public string GetLevelEnemyInfoKey(string groupName)
        {
            if (string.IsNullOrEmpty(LevelBranch))
            {
                return $"{LevelName}/{groupName}";
            }

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
                    PlayerSaveData.LocalSavaData.coin += item.num;
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