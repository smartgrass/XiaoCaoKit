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
            scaleTween?.SyncInteractableState();
            UpdateReward();
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
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