using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XiaoCao
{
    [Serializable]
    public class PlayerSaveData
    {
        public static PlayerSaveData LocalSavaData => GameAllData.playerSaveData;

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

        //ItemUI
        public Inventory inventory = new Inventory();

        public int Coin
        {
            get => inventory.GetItemCount("Coin");
            set => inventory.AddItem(ItemType.Coin, "Coin", value);
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

            if (string.IsNullOrEmpty(prefabId))
            {
                prefabId = "P_0";
            }

            // 确保剧情进度不为空
            if (storyProgress == null)
            {
                storyProgress = new StoryProgress();
            }

            if (levelPassData == null)
            {
                levelPassData = new LevelPassData();
            }

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

        public PlayerAttr GetPlayerAttr()
        {
            PlayerAttr attr = new PlayerAttr();
            AttrSetting setting = ConfigMgr.Inst.AttrSettingSo.GetOrDefault(0, 0);
            attr.Init(0, lv, setting);
            return attr;
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
    }

    public enum LevelPassState
    {
        Lock,
        Unlock,
        Pass
    }
}