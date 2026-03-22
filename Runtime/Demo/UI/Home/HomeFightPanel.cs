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
        public Transform chapters;
        public LevelDetailUI levelDetailUI;
        public Button backBtn;
        public NorBtn chapterSwitchBtn;
        public NorBtn modeSwitchBtn;
        public RectTransform chapterSelectView;
        public RectTransform modesScrollView;
        public GameObject levelsScrollView;

        public int curChapter = 0;

        const string LevelModeName = "关卡模式";
        static readonly Vector2 ModeSwitchBtnSize = new Vector2(320f, 130f);
        static readonly Vector2 ModeSwitchBtnPos = new Vector2(120f, -75f);

        private readonly List<int> _allChapters = new List<int>();
        private bool _isChapterSelectOpen;
        private bool _isModeSelectOpen;

        private void Start()
        {
            chapterSelectView ??= chapters?.parent?.parent as RectTransform;
            levelsScrollView ??= levels?.parent?.parent?.gameObject;

            backBtn.onClick.AddListener(OnClickBack);

            InitChapterSelectUI();
            InitModeSelectUI();
            ShowChapterView(GetCurrentChapter());
            SetChapterSelectVisible(false);
            SetModeSelectVisible(false);
        }

        void InitChapterSelectUI()
        {
            _allChapters.Clear();
            _allChapters.AddRange(LubanTables.GetAllChapters());
            _allChapters.Sort();

            InitCurrentChapterBtn();
            InitModeSwitchBtn();
            InitChapterBtn();
        }

        void InitCurrentChapterBtn()
        {
            if (chapterSwitchBtn == null)
            {
                chapterSwitchBtn = GetComponentInChildren<NorBtn>(true);
            }

            if (chapterSwitchBtn == null)
            {
                return;
            }

            chapterSwitchBtn.onClick += OpenChapterSelectView;
        }

        void InitChapterBtn()
        {
            //获取tabs,所有章节名
            if (chapters == null || _allChapters.Count == 0)
            {
                return;
            }

            UITool.SetCellListCount(chapters, _allChapters.Count);
            ChapterBtn[] chapterBtns = chapters.GetComponentsInChildren<ChapterBtn>(true);
            for (int i = 0; i < _allChapters.Count && i < chapterBtns.Length; i++)
            {
                int chapter = _allChapters[i];
                var chapterBtn = chapterBtns[i];
                chapterBtn.Show(chapter);
                chapterBtn.onClick = () => { OnSelectChapter(chapter); };
            }
        }

        void InitModeSwitchBtn()
        {
            if (modeSwitchBtn == null && chapterSwitchBtn != null)
            {
                GameObject modeButtonObject =
                    Instantiate(chapterSwitchBtn.gameObject, chapterSwitchBtn.transform.parent);
                modeButtonObject.name = "ModeSwitchBtn";
                modeSwitchBtn = modeButtonObject.GetComponent<NorBtn>();
            }

            if (modeSwitchBtn == null)
            {
                return;
            }

            if (modeSwitchBtn.titleText != null)
            {
                modeSwitchBtn.titleText.text = LevelModeName;
            }

            if (modeSwitchBtn.btn != null)
            {
                modeSwitchBtn.btn.interactable = true;
            }

            modeSwitchBtn.onClick = OpenModeSelectView;
        }

        void InitModeSelectUI()
        {
            ModeBtn templateBtn = modesScrollView.GetComponentInChildren<ModeBtn>(true);
            if (templateBtn == null)
            {
                return;
            }

            Transform modeTabs = templateBtn.transform.parent;

            EPlayMode[] modeList = new[] { EPlayMode.Nor };

            UITool.SetCellListCount(modeTabs, modeList.Length);


            ModeBtn[] modeBtns = modeTabs.GetComponentsInChildren<ModeBtn>(true);
            for (int i = 0; i < modeList.Length; i++)
            {
                ModeBtn modeBtn = modeBtns[i];
                modeBtn.onClick = OnSelectLevelMode;
                modeBtn.SetMode(modeList[i]);
                modesScrollView.gameObject.SetActive(false);
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
                levelBtn.onClick += () => { levelDetailUI.Show(levelBtn.curChapter, levelBtn.LevelIndex); };
            }

            RefreshCurrentChapterBtn();
        }

        void OpenChapterSelectView()
        {
            InitChapterBtn();
            SetModeSelectVisible(false);
            SetChapterSelectVisible(true);
        }

        void OpenModeSelectView()
        {
            InitModeSelectUI();
            SetChapterSelectVisible(false);
            SetModeSelectVisible(true);
        }

        void OnSelectChapter(int chapter)
        {
            ShowChapterView(chapter);
            SetChapterSelectVisible(false);
        }

        void SetChapterSelectVisible(bool visible)
        {
            _isChapterSelectOpen = visible;
            RefreshSelectionViewState();
        }

        void SetModeSelectVisible(bool visible)
        {
            _isModeSelectOpen = visible;
            RefreshSelectionViewState();
        }

        void RefreshSelectionViewState()
        {
            chapterSelectView.gameObject.SetActive(_isChapterSelectOpen);
            modesScrollView.gameObject.SetActive(_isModeSelectOpen);

            bool showMainSelectView = !_isChapterSelectOpen && !_isModeSelectOpen;

            levelsScrollView.SetActive(showMainSelectView);
        }

        void RefreshCurrentChapterBtn()
        {
            if (chapterSwitchBtn == null)
            {
                return;
            }

            chapterSwitchBtn.titleText.text = $"{LocalizeKey.GetChapterName(curChapter)}";
            chapterSwitchBtn.onClick = OpenChapterSelectView;
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
            LevelPassState passState =
                PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, firstLevelIndex);
            return passState != LevelPassState.Lock;
        }

        void OnClickBack()
        {
            if (_isChapterSelectOpen)
            {
                SetChapterSelectVisible(false);
                return;
            }

            if (_isModeSelectOpen)
            {
                SetModeSelectVisible(false);
                return;
            }

            HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel);
        }

        void OnSelectLevelMode()
        {
            SetModeSelectVisible(false);
        }
    }
}