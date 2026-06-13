using System;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class LevelBtn : NorBtn
    {
        public UIStateChange stateChange;
        public Transform rewardParent;
        public Transform outlineFlow;
        public Transform outline;
        public TMP_Text passTimeText;
        public TMP_Text enemyLevelText;

        public int curChapter;

        public int LevelIndex
        {
            get => curIndex;
            set => curIndex = value;
        }

        [SerializeField] private BtnScaleTween scaleTween;

        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
        }

        private void OnEnable()
        {
            EnsureScaleTween();
            scaleTween?.SyncInteractableState();
        }

        public void Show(int chapter, int index)
        {
            curChapter = chapter;
            LevelIndex = index;
            titleText.text = LocalizeKey.GetLevelName(chapter, index);

            LevelPassState passState = GetPassState(chapter, index);
            btn.interactable = passState != LevelPassState.Lock;
            stateChange.SetState((int)passState);
            UpdateOutlineState(passState);
            UpdatePassTimeText(passState);
            UpdateEnemyLevelText();
            scaleTween?.SyncInteractableState();
            UpdateReward();
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }

        /// <summary>
        /// 根据通关状态刷新关卡耗时文本。
        /// </summary>
        private void UpdatePassTimeText(LevelPassState passState)
        {
            if (!passTimeText)
            {
                return;
            }

            string passTimeStr = "";
            if (passState != LevelPassState.Pass ||
                !PlayerSaveData.LocalSavaData.levelPassData.TryGetPassTime(curChapter, LevelIndex, out float passTime))
            {
                passTimeStr = "--";
            }
            else
            {
                passTimeStr = FormatPassTime(passTime);
            }


            passTimeText.text = $"{"PassLevelTime".ToLocalizeStr()}: {passTimeStr}";
        }

        /// <summary>
        /// 根据关卡配置刷新敌人等级文本。
        /// </summary>
        private void UpdateEnemyLevelText()
        {
            if (!enemyLevelText)
            {
                return;
            }

            string levelKey = MapNames.GetLevelKey(curChapter, LevelIndex);
            int enemyLevel = LubanTables.GetLevelSetting(levelKey).EnemyBaseLevel;
            enemyLevelText.text = $"{LocalizeKey.EnemyLevel.ToLocalizeStr()}: Lv{enemyLevel}";
        }

        /// <summary>
        /// 将秒数格式化为关卡耗时文本。
        /// </summary>
        private static string FormatPassTime(float passTime)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(passTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        private void UpdateOutlineState(LevelPassState passState)
        {
            bool showOutlineFlow = passState != LevelPassState.Pass && IsLatestUnlockedLevel(curChapter, LevelIndex);

            if (outlineFlow != null)
            {
                outlineFlow.gameObject.SetActive(showOutlineFlow);
            }

            if (outline != null)
            {
                outline.gameObject.SetActive(!showOutlineFlow);
            }
        }

        private bool IsLatestUnlockedLevel(int chapter, int index)
        {
            var chapterSetting = LubanTables.GetChapterSetting(chapter);
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return false;
            }

            int latestUnlockedLevel = int.MinValue;
            for (int i = 0; i < chapterSetting.Levels.Count; i++)
            {
                int level = chapterSetting.Levels[i];
                if (GetPassState(chapter, level) != LevelPassState.Lock)
                {
                    latestUnlockedLevel = Mathf.Max(latestUnlockedLevel, level);
                }
            }

            return latestUnlockedLevel == index;
        }

        private void UpdateReward()
        {
            if (rewardParent == null)
            {
                return;
            }

            string levelKey = MapNames.GetLevelKey(curChapter, LevelIndex);
            var rewards = LevelSettingHelper.GetReward(levelKey);
            if (rewards == null)
            {
                UITool.SetCellListCount(rewardParent, 0);
                return;
            }

            UITool.SetCellListCount(rewardParent, rewards.Count);
            var cells = rewardParent.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < rewards.Count && i < cells.Length; i++)
            {
                cells[i].SetItem(rewards[i], UIItemTextType.NoNum);
            }
        }

        private void EnsureScaleTween()
        {
            scaleTween ??= GetComponent<BtnScaleTween>();
            if (scaleTween == null)
            {
                scaleTween = gameObject.AddComponent<BtnScaleTween>();
            }
        }
    }
}
