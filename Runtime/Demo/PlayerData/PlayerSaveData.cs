using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    [Serializable]
    public class PlayerSaveData
    {
        //当默认构建方法
        public static PlayerSaveData CreateAndCheckNull()
        {
            var data = new PlayerSaveData();
            data.CheckNull();
            return data;
        }
        public static PlayerSaveData LocalSavaData => GameAllData.playerSaveData;

        [NonSerialized] private int cachedInitPlayerAttrLv = -1;
        [NonSerialized] private RoleAttr _cachedInitRoleAttr;

        public int lv;

        //角色经验值, 只累计在角色身上, 不进入背包
        public int exp;

        public int raceId = 0;

        // 剧情进度记录
        public StoryProgress storyProgress = new StoryProgress();

        // 记录关卡通过状态
        public LevelPassData levelPassData = new LevelPassData();

        //技能解锁状态, 等级
        public Dictionary<string, int> skillUnlockDic = new Dictionary<string, int>();

        //技能学习红点已读记录: skillId -> 已查看过的技能等级(等级变化后会重新提示)
        public Dictionary<string, int> skillRedDotReadLv = new Dictionary<string, int>();

        public bool saveSkillBar;

        // 技能栏设置
        public List<string> skillBarSetting = new List<string>();

        public const int MaxSkillBarSetting = 6;

        // 祝福等级存档
        public Dictionary<EBlessing, int> blessingLevelDic = new Dictionary<EBlessing, int>();

        // 支援角色三项养成等级(1~SupportRoleSetting.MaxGrowLevel), 规则见 支援角色设定.md
        public int supportAtkLv = SupportRoleSetting.DefaultGrowLevel;
        public int supportHpLv = SupportRoleSetting.DefaultGrowLevel;
        public int supportSkillLv = SupportRoleSetting.DefaultGrowLevel;

        // 当前羁绊等级内已累计的经验, 满级后清零且不再累计
        public int bondExp;

        //ItemUI
        public Inventory inventory = new Inventory();

        //任务进度记录
        public TaskData taskData = new TaskData();

        //新手引导进度记录
        public GuideData guideData = new GuideData();

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

        // 如果没有完成任何剧情，则认为是新玩家
        public bool IsNewPlayer => !levelPassData.chapterPassDic.ContainsKey(0);

        //反序列化读取的数据, 可能会出现空的现象
        public void CheckNull()
        {
            //角色等级最低 1 级, 修正旧存档中可能存在的 0 级
            lv = Mathf.Max(GameSetting.MinRoleLevel, lv);

            //羁绊经验不会为负; 已满羁绊的存档不再累计经验(旧存档没有该字段, 默认 0)
            bondExp = Mathf.Max(0, bondExp);
            if (SupportRoleSetting.GetBondLevel(supportAtkLv, supportHpLv, supportSkillLv) >=
                SupportRoleSetting.MaxBondLevel)
            {
                bondExp = 0;
            }

            // ConfigMgr.LocalSetting.GetBoolValue 暂时不用

            if (inventory == null)
            {
                inventory = new Inventory();
            }

            inventory.items ??= new List<Item>();
            inventory.equippedHolyRelics ??= new List<Item>();

            //旧存档没有实例 id, 读档后统一补发, 让同名同等级的圣器能被区分开
            EnsureItemInstanceIds(inventory.items);
            EnsureItemInstanceIds(inventory.equippedHolyRelics);

            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<string, int>();
            }

            //旧存档没有技能红点已读记录
            skillRedDotReadLv ??= new Dictionary<string, int>();

            //确保任务进度不为空
            if (taskData == null)
            {
                taskData = new TaskData();
            }
            taskData.CheckNull();

            //确保引导进度不为空(旧存档没有该字段)
            if (guideData == null)
            {
                guideData = new GuideData();
            }
            guideData.CheckNull();

            CheckBlessingNull();

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
                //注意: PlayerSettingSo 由 ConfigMgr.Load() 在游戏流程里填, 编辑器里(没跑游戏)是 null。
                //"玩家存档面板-重置"会在编辑模式直接走到这里, 不判空就会空引用把窗口的 GUI 布局打断
                PlayerSettingSo settingSo = ConfigMgr.Inst.PlayerSettingSo;
                skillBarSetting.Clear();

                PlayerSetting playerSetting = settingSo != null ? settingSo.GetOrDefault(raceId, 0) : null;
                if (playerSetting != null && playerSetting.defaultSkillList != null)
                {
                    skillBarSetting.AddRange(playerSetting.defaultSkillList);

                    //默认解锁1,2技能
                    foreach (var skillId in playerSetting.defaultSkillList)
                    {
                        if (!skillUnlockDic.ContainsKey(skillId))
                        {
                            skillUnlockDic[skillId] = 1;
                        }
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
        /// 校准物品实例 id: 缺 id 的补发, 并让分配器跳过存档里已有的 id。
        /// </summary>
        private static void EnsureItemInstanceIds(List<Item> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item == null)
                {
                    continue;
                }

                item.EnsureInstanceId();
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

        /// <summary>
        /// 获取支援角色指定养成项的当前等级(1~SupportRoleSetting.MaxGrowLevel)。
        /// </summary>
        public int GetSupportGrowLevel(SupportRoleSetting.SupportGrowType kind)
        {
            int level;
            switch (kind)
            {
                case SupportRoleSetting.SupportGrowType.Atk: level = supportAtkLv; break;
                case SupportRoleSetting.SupportGrowType.Hp: level = supportHpLv; break;
                default: level = supportSkillLv; break;
            }
            return Mathf.Clamp(level, 1, SupportRoleSetting.MaxGrowLevel);
        }

        /// <summary>
        /// 设置支援角色指定养成项的等级(供养成界面或调试使用), 越界会被钳制。
        /// </summary>
        public void SetSupportGrowLevel(SupportRoleSetting.SupportGrowType kind, int level)
        {
            level = Mathf.Clamp(level, 1, SupportRoleSetting.MaxGrowLevel);
            switch (kind)
            {
                case SupportRoleSetting.SupportGrowType.Atk: supportAtkLv = level; break;
                case SupportRoleSetting.SupportGrowType.Hp: supportHpLv = level; break;
                default: supportSkillLv = level; break;
            }
            SavaData();
        }

        /// <summary>
        /// 增加羁绊经验: 经验满自动提升羁绊等级, 并把等级换算回三项养成等级; 满级后不再累计。
        /// </summary>
        /// <param name="value">增加的经验值</param>
        /// <param name="saveData">是否在本方法内立即存盘</param>
        /// <returns>是否发生了羁绊升级</returns>
        public bool AddBondExp(int value, bool saveData = true)
        {
            if (value <= 0)
            {
                return false;
            }

            int bondLevel = SupportRoleSetting.GetBondLevel(supportAtkLv, supportHpLv, supportSkillLv);
            if (bondLevel >= SupportRoleSetting.MaxBondLevel)
            {
                bondExp = 0;
                return false;
            }

            int levelFrom = bondLevel;
            bondExp += value;

            //连续结算升级, 直到经验不够下一级或到达羁绊上限
            while (bondLevel < SupportRoleSetting.MaxBondLevel &&
                   bondExp >= SupportRoleSetting.GetNextLevelExp(bondLevel))
            {
                bondExp -= SupportRoleSetting.GetNextLevelExp(bondLevel);
                bondLevel++;
                SupportRoleSetting.GetGrowLevels(bondLevel, out supportAtkLv, out supportHpLv, out supportSkillLv);
            }

            //满级后经验条不再累计, 避免显示成还能继续升
            if (bondLevel >= SupportRoleSetting.MaxBondLevel)
            {
                bondExp = 0;
            }

            if (saveData)
            {
                SavaData();
            }

            return bondLevel > levelFrom;
        }

        /// <summary>
        /// 结算 <see cref="ItemType.OtherValue"/> 类道具: 这类道具不进入背包, 按 typeId 直接加到存档数值上。
        /// 经验值加角色经验; 心之结晶 1 个 = 1 点羁绊经验。
        /// </summary>
        /// <param name="typeId">道具的 <see cref="OtherValueItemId"/></param>
        /// <param name="value">数量(即要加的数值)</param>
        /// <param name="saveData">是否在本方法内立即存盘</param>
        public void AddOtherValue(string typeId, int value, bool saveData = true)
        {
            switch (typeId)
            {
                case OtherValueItemId.Exp:
                    AddExp(value, saveData);
                    break;
                case OtherValueItemId.HeartCrystal:
                    AddBondExp(value, saveData);
                    break;
                default:
                    Debug.LogWarning($"--- no OtherValue handle, typeId: {typeId}");
                    break;
            }
        }

        public void AddSkillLevel(string skillId)
        {
            skillUnlockDic.TryGetValue(skillId, out int level);

            if (level <= 0)
            {
                //0 级升到 1 级是学习(解锁), 不算升级, 也不推进累计升级任务
                skillUnlockDic[skillId] = 1;
                return;
            }

            skillUnlockDic[skillId] = level + 1;

            //累计技能升级次数, 驱动任务进度: 只有已学会的技能继续升级才计入
            TaskMgr.AddCondProgress(cfg.task.TaskCondType.SkillUpgradeCount, 1);
        }

        //PlayerHelper
        public int GetSkillLevel(string skillId)
        {
            skillUnlockDic.TryGetValue(skillId, out int level);
            return level;
        }

        /// <summary>
        /// 获取初始化版本的PlayerAttr，包含存档养成加成。
        /// </summary>
        /// <param name="clone">是否返回缓存副本，true 时避免外部修改污染缓存。</param>
        /// <returns></returns>
        public RoleAttr GetInitPlayerAttr(bool clone = true)
        {
            int currentLv = Mathf.Max(GameSetting.MinRoleLevel, lv);
            if (_cachedInitRoleAttr == null || cachedInitPlayerAttrLv != currentLv)
            {
                cachedInitPlayerAttrLv = currentLv;
                _cachedInitRoleAttr = new RoleAttr();
                AttrSetting setting = ConfigMgr.Inst.AttrSettingSo.GetOrDefault(0, 0);
                _cachedInitRoleAttr.Init(0, currentLv, setting);
                ApplyBlessingAttr(_cachedInitRoleAttr);
                ApplyEquippedRelicAttr(_cachedInitRoleAttr);
                _cachedInitRoleAttr.hp = _cachedInitRoleAttr.MaxHp;
            }

            return clone ? _cachedInitRoleAttr.Clone() : _cachedInitRoleAttr;
        }

        /// <summary>
        /// 将祝福等级折算为玩家基础属性加成。
        /// </summary>
        private void ApplyBlessingAttr(RoleAttr attr)
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

        /// <summary>
        /// 将当前已装备圣器的数值加成到玩家基础属性。
        /// </summary>
        private void ApplyEquippedRelicAttr(RoleAttr attr)
        {
            if (inventory?.equippedHolyRelics == null)
            {
                return;
            }

            for (int i = 0; i < inventory.equippedHolyRelics.Count; i++)
            {
                Item item = inventory.equippedHolyRelics[i];
                if (item == null || item.type != ItemType.HolyRelic || string.IsNullOrEmpty(item.typeId))
                {
                    continue;
                }

                string modifierKey = $"RelicAttr_{i}_{item.ItemKey}";
                foreach (var (attrType, value) in RelicValueCalc.GetRelicValue(item))
                {
                    attr.ChangeAttrValue(attrType, modifierKey, new AttributeModifier { Add = value });
                }
            }
        }

        /// <summary>
        /// 重新计算存档养成后的玩家初始化属性。
        /// </summary>
        public void ReLoadPlayerAttr()
        {
            _cachedInitRoleAttr = null;
            GetInitPlayerAttr();
        }

        /// <summary>
        /// 增加角色经验值: 经验值累计在角色身上, 不进入背包; 经验足够时自动结算升级
        /// </summary>
        /// <param name="value">增加的经验值</param>
        /// <param name="saveData">是否在本方法内立即存盘; 编辑器调试批量加经验时传 false, 由调用方统一存盘</param>
        public void AddExp(int value, bool saveData = true)
        {
            if (value <= 0)
            {
                return;
            }

            exp += value;
            int levelFrom = lv;

            //连续结算升级, 直到经验不够下一级或到达等级上限
            while (lv < GameSetting.MaxRoleLevel &&
                   exp >= CharacterAttrCalc.GetRoleNextLevelExp(lv))
            {
                exp -= CharacterAttrCalc.GetRoleNextLevelExp(lv);
                lv++;
            }

            if (lv > levelFrom)
            {
                //有升级: 存盘并广播升级事件
                if (saveData)
                {
                    SaveAndNotifyLevelUp(levelFrom);
                }
            }
            else if (saveData)
            {
                SavaData();
            }
        }

        /// <summary>
        /// 提升角色等级: 等级会被限制在上下限之间, 写入存档并广播升级事件(参数: 原等级, 新等级)
        /// </summary>
        public void UpgradeLevel(int addLevel = 1)
        {
            int levelFrom = lv;
            lv = Mathf.Clamp(levelFrom + addLevel, GameSetting.MinRoleLevel, GameSetting.MaxRoleLevel);
            if (lv <= levelFrom)
            {
                return;
            }

            SaveAndNotifyLevelUp(levelFrom);
        }

        /// <summary>
        /// 写入存档并广播升级事件, 由表现层播放升级提示
        /// </summary>
        private void SaveAndNotifyLevelUp(int levelFrom)
        {
            SavaData();
            GameEvent.Send<int, int>(EGameEvent.RoleLevelUp.ToInt(), levelFrom, lv);
        }

        public void SavaThisData()
        {
            SaveMgr.SaveData(this);
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

        /// <summary>
        /// 覆盖式设置章节通关进度(可前进也可回退), 供编辑器调试用; 正常游戏流程用 <see cref="SetPassState"/>
        /// </summary>
        public void SetPassProgress(int chapter, int index)
        {
            chapterPassDic[chapter] = index;
        }

        /// <summary>
        /// 获取指定章节已通关到的关卡序号, 该章节无记录时返回 0
        /// </summary>
        public int GetPassedLevelIndex(int chapter)
        {
            return chapterPassDic.TryGetValue(chapter, out int index) ? index : 0;
        }

        /// <summary>
        /// 清空所有未通关关卡残留的星星记录(无死亡通关与最快耗时)
        /// </summary>
        /// <returns>被清理记录的关卡数量</returns>
        public int ClearStarStatesOfUnpassLevels()
        {
            List<string> keys = new List<string>(otherStates.Keys);
            int clearCount = 0;
            foreach (string key in keys)
            {
                if (!TryParseLevelKey(key, out int chapter, out int index) ||
                    GetPassState(chapter, index) == LevelPassState.Pass)
                {
                    continue;
                }

                LevelOtherState state = otherStates[key];
                if (state.hasNoDeathPass || state.passTime > 0)
                {
                    state.hasNoDeathPass = false;
                    state.passTime = 0;
                    otherStates[key] = state;
                    clearCount++;
                }
            }

            return clearCount;
        }

        /// <summary>
        /// 解析关卡存档键("章节_序号"), 解析失败返回 false
        /// </summary>
        private static bool TryParseLevelKey(string key, out int chapter, out int index)
        {
            chapter = 0;
            index = 0;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string[] parts = key.Split('_');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], out chapter) &&
                   int.TryParse(parts[1], out index);
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
        /// 判断指定关卡是否已经记录无死亡通关。
        /// </summary>
        public bool HasNoDeathPass(int chapter, int index)
        {
            if (GetPassState(chapter, index) != LevelPassState.Pass)
            {
                return false;
            }

            string key = GetLevelKey(chapter, index);
            return otherStates.TryGetValue(key, out var state) && state.hasNoDeathPass;
        }

        /// <summary>
        /// 记录指定关卡的无死亡通关状态。
        /// </summary>
        public void SetHasNoDeathPass(int chapter, int index, bool isOn = true)
        {
            if (!isOn)
            {
                return;
            }

            string key = GetLevelKey(chapter, index);
            if (!otherStates.ContainsKey(key))
            {
                otherStates[key] = new LevelOtherState();
            }

            var state = otherStates[key];
            state.hasNoDeathPass = true;
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
            if (state.passTime <= 0 || passTime < state.passTime)
            {
                state.passTime = passTime;
            }
            otherStates[key] = state;
        }

        /// <summary>
        /// 获取指定关卡当前记录的星星数量。
        /// </summary>
        public int GetLevelStarCount(int chapter, int index)
        {
            if (GetPassState(chapter, index) != LevelPassState.Pass)
            {
                return 0;
            }

            int starCount = 1;
            if (HasNoDeathPass(chapter, index))
            {
                starCount++;
            }

            if (TryGetPassTime(chapter, index, out float passTime) &&
                passTime <= GetStarPassTimeLimit(chapter, index))
            {
                starCount++;
            }

            return Mathf.Clamp(starCount, 0, 3);
        }

        /// <summary>
        /// 获取三星通关的时间阈值，默认 8 分钟，后续可按关卡单独扩展。
        /// </summary>
        public int GetStarPassTimeLimit(int chapter, int index)
        {
            return 8 * 60;
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

        // 是否记录无死亡通关
        public bool hasNoDeathPass;

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
