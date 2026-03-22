using System;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    public class ChapterBtn : NorBtn
    {
        public void Show(int chapter)
        {
            var chapterSetting = LubanTables.GetChapterSetting(chapter);
            LevelPassState passState = GetChapterPassState(chapter, chapterSetting);

            int totalLevelCount = chapterSetting?.Levels?.Count ?? 0;
            int completedLevelCount = 0;
            if (chapterSetting != null)
            {
                foreach (int levelIndex in chapterSetting.Levels)
                {
                    if (GetPassState(chapter, levelIndex) == LevelPassState.Pass)
                    {
                        completedLevelCount++;
                    }
                }
            }

            titleText.text = $"{LocalizeKey.GetChapterName(chapter)}";
            btn.interactable = passState != LevelPassState.Lock;
            curIndex = chapter;
        }

        private LevelPassState GetChapterPassState(int chapter, ChapterSetting chapterSetting)
        {
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return LevelPassState.Lock;
            }

            int firstLevelIndex = chapterSetting.Levels[0];
            return GetPassState(chapter, firstLevelIndex);
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }
    }
}
