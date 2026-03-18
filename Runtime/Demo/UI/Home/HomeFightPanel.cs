using cfg;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    public class HomeFightPanel : HomePanelBase
    {
        public Transform levels;
        public Transform tabs;
        public LevelDetailUI levelDetailUI;
        public Button backBtn;
        public ChapterBtn currentChapterBtn;
        public RectTransform chapterSelectView;
        public GameObject levelsScrollView;

        public int curChapter = 0;

        private readonly List<int> _allChapters = new List<int>();
        private bool _isChapterSelectOpen;

        private void Start()
        {
            chapterSelectView ??= tabs?.parent?.parent as RectTransform;
            levelsScrollView ??= levels?.parent?.parent?.gameObject;

            backBtn.onClick.AddListener(OnClickBack);

            InitChapterSelectUI();
            ShowChapterView(GetCurrentChapter());
            SetChapterSelectVisible(false);
        }

        void InitChapterSelectUI()
        {
            _allChapters.Clear();
            _allChapters.AddRange(LubanTables.GetAllChapters());
            _allChapters.Sort();

            InitCurrentChapterBtn();
            InitChapterBtn();
        }

        void InitCurrentChapterBtn()
        {
            if (currentChapterBtn == null)
            {
                currentChapterBtn = GetComponentInChildren<ChapterBtn>(true);
            }

            if (currentChapterBtn == null)
            {
                return;
            }

            currentChapterBtn.onClick = OpenChapterSelectView;
        }

        void InitChapterBtn()
        {
            //获取tabs,所有章节名
            if (tabs == null || _allChapters.Count == 0)
            {
                return;
            }

            UITool.SetCellListCount(tabs, _allChapters.Count);
            ChapterBtn[] chapterBtns = tabs.GetComponentsInChildren<ChapterBtn>(true);
            for (int i = 0; i < _allChapters.Count && i < chapterBtns.Length; i++)
            {
                int chapter = _allChapters[i];
                var chapterBtn = chapterBtns[i];
                chapterBtn.Show(chapter);
                chapterBtn.onClick = () => { OnSelectChapter(chapter); };
            }
        }

        void ShowChapterView(int chapter)
        {
            curChapter = chapter;
            var chapterSetting = LubanTables.GetChapterSetting(chapter);
            if (chapterSetting?.Levels == null)
            {
                return;
            }

            UITool.SetCellListCount(levels, chapterSetting.Levels.Count);
            LevelBtn[] levelBtns = levels.GetComponentsInChildren<LevelBtn>(true);
            for (int i = 0; i < chapterSetting.Levels.Count && i < levelBtns.Length; i++)
            {
                var levelBtn = levelBtns[i];
                levelBtn.Show(chapterSetting.Id, chapterSetting.Levels[i]);
                levelBtn.onClick = null;
                levelBtn.onClick += () =>
                {
                    levelDetailUI.Show(levelBtn.curChapter, levelBtn.levelIndex);
                };
            }

            RefreshCurrentChapterBtn();
        }

        void OpenChapterSelectView()
        {
            InitChapterBtn();
            SetChapterSelectVisible(true);
        }

        void OnSelectChapter(int chapter)
        {
            ShowChapterView(chapter);
            SetChapterSelectVisible(false);
        }

        void SetChapterSelectVisible(bool visible)
        {
            _isChapterSelectOpen = visible;

            if (chapterSelectView != null)
            {
                chapterSelectView.gameObject.SetActive(visible);
            }

            if (levelsScrollView != null)
            {
                levelsScrollView.SetActive(!visible);
            }

            if (currentChapterBtn != null)
            {
                currentChapterBtn.gameObject.SetActive(!visible);
            }
        }

        void RefreshCurrentChapterBtn()
        {
            if (currentChapterBtn == null)
            {
                return;
            }

            currentChapterBtn.Show(curChapter);
            currentChapterBtn.onClick = OpenChapterSelectView;
        }

        int GetCurrentChapter()
        {
            if (_allChapters.Count == 0)
            {
                return 0;
            }

            int latestChapter = _allChapters[0];
            for (int i = 0; i < _allChapters.Count; i++)
            {
                int chapter = _allChapters[i];
                if (!IsChapterUnlocked(chapter))
                {
                    break;
                }

                latestChapter = chapter;
            }

            return latestChapter;
        }

        bool IsChapterUnlocked(int chapter)
        {
            var chapterSetting = LubanTables.GetChapterSetting(chapter);
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return false;
            }

            int firstLevelIndex = chapterSetting.Levels[0];
            LevelPassState passState = PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, firstLevelIndex);
            return passState != LevelPassState.Lock;
        }

        void OnClickBack()
        {
            if (_isChapterSelectOpen)
            {
                SetChapterSelectVisible(false);
                return;
            }

            HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel);
        }

    }
}
