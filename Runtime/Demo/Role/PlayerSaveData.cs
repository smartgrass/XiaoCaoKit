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

        //技能解锁状态
        public Dictionary<string, int> skillUnlockDic = new Dictionary<string, int>();

        //ItemUI
        public Inventory inventory = new Inventory();

        public int coin;
        
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
            else if (chapter == 0 && index == 0)
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