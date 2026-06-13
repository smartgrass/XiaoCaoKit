using System;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    [Serializable]
    public class PlayerSaveData
    {
        public static PlayerSaveData LocalSavaData => GameAllData.playerSaveData;

        [NonSerialized] private int cachedInitPlayerAttrLv = -1;
        [NonSerialized] private PlayerAttr cachedInitPlayerAttr;

        public int lv;

        public int raceId = 0;

        public string prefabId;

        // 剧情进度记录
        public StoryProgress storyProgress = new StoryProgress();

        // 记录关卡通过状态
        public LevelPassData levelPassData = new LevelPassData();

        //技能解锁状态, 等级
        public Dictionary<string, int> skillUnlockDic = new Dictionary<string, int>();

        public bool saveSkillBar;

        // 技能栏设置
        public List<string> skillBarSetting = new List<string>();

        public const int MaxSkillBarSetting = 6;

        // 祝福等级存档
        public Dictionary<EBlessing, int> blessingLevelDic = new Dictionary<EBlessing, int>();

        //ItemUI
        public Inventory inventory = new Inventory();

        public int Coin
        {
            get => inventory.GetItemCount("Coin");
            set
            {
                int curCoin = inventory.GetItemCount("Coin");
                int delta = value - curCoin;
                if (delta > 0)
                {
                    inventory.AddItem(ItemType.Coin, "Coin", delta);
                }
                else if (delta < 0)
                {
                    inventory.ConsumeItem(nameof(ItemType.Coin), -delta);
                }
            }
        }

        public void RewardCoin(int coin, bool showUI = true)
        {
            if (coin == 0)
            {
                return;
            }

            Coin += coin;
            GameEvent.Send<int>(EGameEvent.OnCoinChange.ToInt(), coin);
            if (showUI)
            {
                Item item = new Item(ItemType.Coin, "Coin", coin);
                GameEvent.Send<Item>(EGameEvent.OnGetItem.ToInt(), item);
            }
        }

        public bool TryConsumeCoin(int coin, bool saveData = true)
        {
            if (coin <= 0)
            {
                return true;
            }

            if (!inventory.ConsumeItem(nameof(ItemType.Coin), coin))
            {
                return false;
            }

            GameEvent.Send<int>(EGameEvent.OnCoinChange.ToInt(), -coin);
            if (saveData)
            {
                SavaData();
            }

            return true;
        }

        //持有物
        public List<Item> holdItems = new List<Item>();

        public List<Item> equippedHolyRelics = new List<Item>();

        // 如果没有完成任何剧情，则认为是新玩家
        public bool IsNewPlayer => !levelPassData.chapterPassDic.ContainsKey(0);

        //反序列化读取的数据, 可能会出现空的现象
        internal void CheckNull()
        {
            // ConfigMgr.LocalSetting.GetBoolValue 暂时不用

            if (inventory == null)
            {
                inventory = new Inventory();
            }

            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<string, int>();
            }

            CheckBlessingNull();

            if (string.IsNullOrEmpty(prefabId))
            {
                prefabId = "P_0";
            }

            // 确保剧情进度不为空
            if (storyProgress == null)
            {
                storyProgress = new StoryProgress();
            }

            storyProgress.CheckNull();

            if (levelPassData == null)
            {
                levelPassData = new LevelPassData();
            }
            
            levelPassData.CheckNull();
            
            if (skillBarSetting == null)
            {
                skillBarSetting = new List<string>();
            }

            // 初始化默认技能栏设置（如果为空）
            if (!saveSkillBar)
            {
                var playerSetting = ConfigMgr.Inst.PlayerSettingSo.GetOrDefault(raceId, 0);
                skillBarSetting.Clear();
                skillBarSetting.AddRange(playerSetting.defaultSkillList);

                //默认解锁1,2技能
                foreach (var skillId in playerSetting.defaultSkillList)
                {
                    if (!skillUnlockDic.ContainsKey(skillId))
                    {
                        skillUnlockDic[skillId] = 1;
                    }
                }

                // AiSkillCmdSetting aiCmdSetting =
                //     ConfigMgr.LoadSoConfig<AiCmdSettingSo>().GetOrDefault(raceId, 0);
                //
                // if (aiCmdSetting != null && aiCmdSetting.cmdSkillList != null)
                // {
                //     skillBarSetting = new List<string>(aiCmdSetting.cmdSkillList);
                // }
            }
        }

        /// <summary>
        /// 确保祝福存档字段可用，并兼容旧存档。
        /// </summary>
        public void CheckBlessingNull()
        {
            blessingLevelDic ??= new Dictionary<EBlessing, int>();
            foreach (EBlessing blessing in BlessingRule.AllBlessings)
            {
                if (!blessingLevelDic.ContainsKey(blessing))
                {
                    blessingLevelDic[blessing] = 0;
                }
            }
        }

        /// <summary>
        /// 获取指定祝福当前等级。
        /// </summary>
        public int GetBlessingLevel(EBlessing blessing)
        {
            CheckBlessingNull();
            blessingLevelDic.TryGetValue(blessing, out int level);
            return Mathf.Clamp(level, 0, BlessingRule.MaxLevel);
        }

        /// <summary>
        /// 获取指定祝福碎片的持有数量。
        /// </summary>
        public int GetBlessingFragmentCount(EBlessing blessing)
        {
            if (inventory == null)
            {
                inventory = new Inventory();
            }

            return inventory.GetItemCount(BlessingRule.GetFragmentItemKey(blessing));
        }

        /// <summary>
        /// 获取指定祝福升到下一级所需碎片数量。
        /// </summary>
        public int GetBlessingNextCost(EBlessing blessing)
        {
            int currentLevel = GetBlessingLevel(blessing);
            if (BlessingRule.IsMaxLevel(currentLevel))
            {
                return 0;
            }

            return BlessingRule.GetUpgradeCost(blessing, currentLevel + 1);
        }

        /// <summary>
        /// 判断指定祝福当前是否可以升级。
        /// </summary>
        public bool CanUpgradeBlessing(EBlessing blessing)
        {
            int currentLevel = GetBlessingLevel(blessing);
            if (BlessingRule.IsMaxLevel(currentLevel))
            {
                return false;
            }

            int cost = GetBlessingNextCost(blessing);
            return GetBlessingFragmentCount(blessing) >= cost;
        }

        /// <summary>
        /// 消耗碎片并提升指定祝福等级。
        /// </summary>
        public bool TryUpgradeBlessing(EBlessing blessing)
        {
            CheckBlessingNull();
            if (inventory == null)
            {
                inventory = new Inventory();
            }

            int currentLevel = GetBlessingLevel(blessing);
            if (BlessingRule.IsMaxLevel(currentLevel))
            {
                return false;
            }

            int cost = BlessingRule.GetUpgradeCost(blessing, currentLevel + 1);
            string itemKey = BlessingRule.GetFragmentItemKey(blessing);
            if (!inventory.CheckEnoughItem(itemKey, cost))
            {
                return false;
            }

            if (!inventory.ConsumeItem(itemKey, cost))
            {
                return false;
            }

            blessingLevelDic[blessing] = currentLevel + 1;
            ReLoadPlayerAttr();
            SavaData();
            return true;
        }

        public void AddSkillLevel(string skillId)
        {
            if (!skillUnlockDic.ContainsKey(skillId))
            {
                skillUnlockDic[skillId] = 1;
            }
            else
            {
                skillUnlockDic[skillId] = skillUnlockDic[skillId] + 1;
            }
        }

        //PlayerHelper
        public int GetSkillLevel(string skillId)
        {
            skillUnlockDic.TryGetValue(skillId, out int level);
            return level;
        }

        // 存档时调用
        public void SaveEquippedHolyRelics(Dictionary<string, Item> equippedDict)
        {
            equippedHolyRelics = equippedDict.Values.ToList();
        }

        // 读档时调用
        public Dictionary<string, Item> LoadEquippedHolyRelics()
        {
            var dict = new Dictionary<string, Item>();
            foreach (var item in equippedHolyRelics)
            {
                var relic = HolyRelicItem.Create(item);
                dict[relic.modName] = item;
            }

            return dict;
        }
        /// <summary>
        /// 获取初始化版本的PlayerAttr，包含存档养成加成。
        /// </summary>
        /// <param name="clone">是否返回缓存副本，true 时避免外部修改污染缓存。</param>
        /// <returns></returns>
        public PlayerAttr GetInitPlayerAttr(bool clone = true)
        {
            int currentLv = Mathf.Max(1, lv);
            if (cachedInitPlayerAttr == null || cachedInitPlayerAttrLv != currentLv)
            {
                cachedInitPlayerAttrLv = currentLv;
                cachedInitPlayerAttr = new PlayerAttr();
                AttrSetting setting = ConfigMgr.Inst.AttrSettingSo.GetOrDefault(0, 0);
                cachedInitPlayerAttr.Init(0, currentLv, setting);
                ApplyBlessingAttr(cachedInitPlayerAttr);
            }

            return clone ? cachedInitPlayerAttr.Clone() : cachedInitPlayerAttr;
        }

        /// <summary>
        /// 将祝福等级折算为玩家基础属性加成。
        /// </summary>
        private void ApplyBlessingAttr(PlayerAttr attr)
        {
            CheckBlessingNull();
            foreach (EBlessing blessing in BlessingRule.AllBlessings)
            {
                int level = GetBlessingLevel(blessing);
                if (level <= 0)
                {
                    continue;
                }

                attr.ChangeAttrValue(
                    BlessingRule.GetPrimaryAttr(blessing),
                    BlessingRule.GetModifierKey(blessing),
                    BlessingRule.GetPrimaryModifier(blessing, level));

                if (blessing == EBlessing.Crit)
                {
                    attr.ChangeAttrValue(
                        EAttr.CritDamage,
                        BlessingRule.GetCritDamageModifierKey(),
                        new AttributeModifier { Add = BlessingRule.GetCritDamageBonus(level) });
                }
            }
        }

        public void ReLoadPlayerAttr()
        {
            cachedInitPlayerAttr = null;
            GetInitPlayerAttr();
        }

        public static void SavaData()
        {
            SaveMgr.SaveData(PlayerSaveData.LocalSavaData);
        }
    }

    [Serializable]
    public class LevelPassData
    {
        //记录章节通过状态 maxChapter,maxIndex
        [NaughtyAttributes.ShowNonSerializedField]
        public Dictionary<int, int> chapterPassDic = new Dictionary<int, int>();

        public Dictionary<string, LevelOtherState> otherStates = new Dictionary<string, LevelOtherState>();

        public void CheckNull()
        {
            Debug.Log($"-- CheckNull {otherStates}");
            chapterPassDic ??= new Dictionary<int, int>();
            otherStates ??= new Dictionary<string, LevelOtherState>();
        }

        public LevelPassState GetPassState(int chapter, int index)
        {
            //判断章节是否解锁
            if (chapterPassDic.TryGetValue(chapter, out int maxIndex))
            {
                if (index <= maxIndex)
                {
                    return LevelPassState.Pass;
                }
                //后一关解锁
                else if (index - 1 == maxIndex)
                {
                    return LevelPassState.Unlock;
                }
            }

            if (chapter == 0 && index == 1)
            {
                // 默认第一章第一节是解锁的
                return LevelPassState.Unlock;
            }

            return LevelPassState.Lock;
        }

        public void SetPassState(int chapter, int index)
        {
            if (!chapterPassDic.ContainsKey(chapter))
            {
                chapterPassDic[chapter] = index;
            }
            else
            {
                chapterPassDic[chapter] = Mathf.Max(chapterPassDic[chapter], index);
            }
        }

        public bool HasGetFirstReward(int chapter, int index)
        {
            string key = GetLevelKey(chapter, index);
            if (!otherStates.TryGetValue(key, out var state))
            {
                return false;
            }

            return state.hasGetFirstReward;
        }

        public void SetHasGetFirstReward(int chapter, int index, bool isOn = true)
        {
            string key = GetLevelKey(chapter, index);
            if (!otherStates.ContainsKey(key))
            {
                otherStates[key] = new LevelOtherState();
            }

            var state = otherStates[key];
            state.hasGetFirstReward = isOn;
            otherStates[key] = state;
        }

        /// <summary>
        /// 获取指定关卡已记录的通关耗时。
        /// </summary>
        public bool TryGetPassTime(int chapter, int index, out float passTime)
        {
            passTime = 0;
            string key = GetLevelKey(chapter, index);
            if (!otherStates.TryGetValue(key, out var state) || state.passTime <= 0)
            {
                return false;
            }

            passTime = state.passTime;
            return true;
        }

        /// <summary>
        /// 记录指定关卡的通关耗时。
        /// </summary>
        public void SetPassTime(int chapter, int index, float passTime)
        {
            if (passTime <= 0)
            {
                return;
            }

            string key = GetLevelKey(chapter, index);
            if (!otherStates.ContainsKey(key))
            {
                otherStates[key] = new LevelOtherState();
            }

            var state = otherStates[key];
            state.passTime = passTime;
            otherStates[key] = state;
        }

        private static string GetLevelKey(int chapter, int index)
        {
            return chapter + "_" + index;
        }
    }

    public struct LevelOtherState
    {
        //是否获取首通奖励
        public bool hasGetFirstReward;

        //通关耗时,单位秒; <=0 表示未记录
        public float passTime;
    }

    public enum LevelPassState
    {
        Lock,
        Unlock,
        Pass
    }
}
