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
        //关卡名前缀 level_0_2
        private const string LevelNamePrefix = "level_";

        //关卡敌人配置前缀 LevelAction_0_2
        private const string LevelActionPrefix = "LevelAction_";

        /// <summary>
        /// 获取关卡使用的地图预制体名
        /// 配置 mapPrefab 时用它(多个关卡可共用同一套地图), 未配置时回退为关卡名
        /// </summary>
        public static string GetMapPrefabName(string levelName)
        {
            var setting = LubanTables.GetLevelSettingOrDefault(levelName);
            if (setting != null && !string.IsNullOrEmpty(setting.MapPrefab))
            {
                return setting.MapPrefab;
            }

            return levelName;
        }

        /// <summary>
        /// 获取关卡的敌人配置(LevelAction)预制体名
        /// 配置了 levelAction 时用配置名, 未配置时按默认格式: level_0_2 -> LevelAction_0_2
        /// </summary>
        public static string GetLevelActionName(string levelName)
        {
            var setting = LubanTables.GetLevelSettingOrDefault(levelName);
            if (setting != null && !string.IsNullOrEmpty(setting.LevelAction))
            {
                return setting.LevelAction;
            }

            return GetDefaultLevelActionName(levelName);
        }

        /// <summary>
        /// 默认的敌人配置预制体名: level_0_2 -> LevelAction_0_2
        /// </summary>
        public static string GetDefaultLevelActionName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                return levelName;
            }

            if (levelName.StartsWith(LevelNamePrefix))
            {
                return LevelActionPrefix + levelName.Substring(LevelNamePrefix.Length);
            }

            return LevelActionPrefix + levelName;
        }

        //获取Enemy等级
        public static int GetEnemyLevel(int addLevel)
        {
            string levelName = GameDataCommon.Current.levelName;
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = "level_0_1";
                Debuger.LogWarning("---! levelName = empty");
            }

            var setting = LubanTables.GetLevelSetting(levelName);
            return setting.EnemyBaseLevel + setting.EnemyLevelAdd * addLevel;
        }
        
        /// <summary>
        /// 关卡通关奖励: 配置奖励 + 关卡经验值
        /// 经验值用 <see cref="ItemType.OtherValue"/> 加 typeId <see cref="OtherValueItemId.Exp"/> 表示, 不进入背包
        /// </summary>
        public static List<Item> GetReward(string levelName)
        {
            var setting = LubanTables.GetLevelSetting(levelName);

            //配置奖励是配置表数据, 复制一份再补充经验值
            List<Item> rewardList = new List<Item>(setting.Reward);

            //经验值按关卡Id计算
            int exp = LevelRewardCalc.GetExp(setting.Id);
            if (exp > 0)
            {
                rewardList.Add(new Item(ItemType.OtherValue, OtherValueItemId.Exp, exp));
            }
            //获得金币 
            int coin = LevelRewardCalc.GetCoin(setting.Id);
            if (coin > 0)
            {
                rewardList.Add(new Item(ItemType.Coin,nameof(ItemType.Coin),coin));
            }

            //通关矿石: 只有精英关重复刷才产出
            Item oreItem = LevelRewardCalc.GetClearOre(setting.Id);
            if (oreItem != null)
            {
                rewardList.Add(oreItem);
            }

            return rewardList;
        }

        /// <summary>
        /// 关卡首通奖励: 配置奖励 + 首通经验值
        /// 经验值用 <see cref="ItemType.OtherValue"/> 加 typeId <see cref="OtherValueItemId.Exp"/> 表示, 不进入背包
        /// </summary>
        public static List<Item> GetFirstReward(string levelName)
        {
            var setting = LubanTables.GetLevelSetting(levelName);

            //配置奖励是配置表数据, 复制一份再补充首通经验值
            List<Item> rewardList = new List<Item>(setting.FirstReward);

            //首通经验值按关卡Id计算
            int exp = LevelRewardCalc.CalcFirstPassExp(setting.Id);
            if (exp > 0)
            {
                rewardList.Add(new Item(ItemType.OtherValue, OtherValueItemId.Exp, exp));
            }

            //首通矿石: 所有关卡都有(精英关按精英数值)
            Item oreItem = LevelRewardCalc.GetFirstPassOre(setting.Id);
            if (oreItem != null)
            {
                rewardList.Add(oreItem);
            }

            return rewardList;
        }

        public static int GetEnemyCoin(string levelName)
        {
            var setting = LubanTables.GetLevelSetting(levelName);
            return setting.EnemyCoin;
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

        public string GetLevelNameText => LevelInfo.ParseString(LevelName).GetLevelName();
        public LevelInfo GetLevelInfo => LevelInfo.ParseString(LevelName);

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
                    GameEvent.Send<Item>(EGameEvent.OnGetItem.ToInt(), item);
                    break;
                case ItemType.ExtraItem:
                    BattleData.Current.AddExtraItem(item);
                    GameEvent.Send<Item>(EGameEvent.OnGetItem.ToInt(), item);
                    break;
                case ItemType.HolyRelic:
                    //圣器不可堆叠, 直接加入背包(是否装备由玩家在角色面板操作)
                    PlayerSaveData.LocalSavaData.inventory.AddItem(item);
                    GameEvent.Send<Item>(EGameEvent.OnGetItem.ToInt(), item);
                    break;
                case ItemType.Coin:
                    PlayerSaveData.LocalSavaData.Coin += item.num;
                    break;
                case ItemType.Buff:
                    RewardBuff(item);
                    break;
                case ItemType.OtherValue:
                    item.RewardOtherValue();
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

            RewardPoolSo so = ConfigMgr.Inst.EnemyKillRewardSo;
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
            if (string.IsNullOrEmpty(item.typeId))
            {
                Debuger.LogError("--- id = empty");
                return;
            }

            BuffItem buffItem = BuffItem.Create(item);

            PlayerHelper.AddBuff(GameDataCommon.Current.localPlayerId, buffItem,true);

            Debug.Log($"--- AddBuff {buffItem.buffs[0].eBuff}");
        }
        //特殊的转换规则,可以取随机
    }
}
