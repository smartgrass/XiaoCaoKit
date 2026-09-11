using System.Collections.Generic;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 独立的关卡入口面板。
    /// </summary>
    public class LevelSelectPanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.LevelSelectPanel;

        public Button backBtn;
        public RectTransform rewardRoot;
        public RectTransform firstRewardRoot;
        public TMP_Text levelTitle;
        public TMP_Text descText;
        public TMP_Text powerText;
        public TMP_Text rewardTitleText;
        public Button enterBtn;

        public Image stageImage;
        public Image stageFrame;
        public RectTransform levelListRoot;
        public ScrollRect levelListScrollRect;
        public Button chapterBtn;
        public ChaptersView chaptersView;
        public GameObject levelView;

        private readonly List<int> _allChapters = new List<int>();
        private int _currentChapter;
        private int _selectedLevelIndex;
        private ChapterSetting _currentChapterSetting;

        private void Start()
        {
            backBtn?.onClick.AddListener(OnClickBack);
            enterBtn?.onClick.AddListener(OnClickEnter);
            chapterBtn?.onClick.AddListener(OnClickChapter);
            chaptersView.onChapterSelected += OnChapterSelected;
            RefreshChapterData();
            CacheLevelListRoot();
            ShowChapter(GetCurrentChapter());
        }

        /// <summary>
        /// 显示面板并刷新当前章节。
        /// </summary>
        public override void Show(IUIData data = null)
        {
            gameObject.SetActive(true);
            RefreshChapterData();
            CacheLevelListRoot();
            ShowChapter(GetCurrentChapter());
        }

        /// <summary>
        /// 隐藏面板。
        /// </summary>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 切换并刷新指定章节。
        /// </summary>
        public void ShowChapter(int chapter)
        {
            _currentChapter = chapter;
            _currentChapterSetting = LubanTables.GetChapterSetting(chapter);
            if (_currentChapterSetting == null)
            {
                return;
            }

            _selectedLevelIndex = GetLatestChallengeableLevel(_currentChapterSetting);

            RefreshLevelList();
            RefreshStageHeader();
            RefreshRewardPanel();
            RefreshBottomInfo();
        }

        /// <summary>
        /// 刷新章节数据缓存。
        /// </summary>
        private void RefreshChapterData()
        {
            _allChapters.Clear();
            _allChapters.AddRange(LubanTables.GetAllChapters());
            _allChapters.Sort();
        }

        /// <summary>
        /// 刷新当前章节的关卡列表展示。
        /// </summary>
        private void RefreshLevelList()
        {
            if (_currentChapterSetting?.Levels == null || _currentChapterSetting.Levels.Count == 0)
            {
                return;
            }

            if (!CacheLevelListRoot())
            {
                return;
            }

            UITool.SetCellListCount(levelListRoot, _currentChapterSetting.Levels.Count);
            LevelBtn[] levelBtns = levelListRoot.GetComponentsInChildren<LevelBtn>(true);
            for (int i = 0; i < _currentChapterSetting.Levels.Count && i < levelBtns.Length; i++)
            {
                int levelIndex = _currentChapterSetting.Levels[i];
                LevelBtn levelBtn = levelBtns[i];
                levelBtn.Show(_currentChapter, levelIndex);
                levelBtn.SetSelected(false);
                levelBtn.onClick = () =>
                {
                    OnSelectLevel(levelIndex);
                };
            }

            SetLevelBtnSelected(_selectedLevelIndex);
            ScrollToSelectedLevelWithoutAnimation();
        }

        /// <summary>
        /// 刷新关卡标题与描述。
        /// </summary>
        private void RefreshStageHeader()
        {
            string chapterName = LocalizeKey.GetChapterName(_currentChapter);
            if (levelTitle != null)
            {
                levelTitle.text = $"{chapterName}-{_selectedLevelIndex}";
            }
            
            if (stageImage != null)
            {
                stageImage.sprite = LoadStageSprite(_currentChapterSetting);
                stageImage.enabled = stageImage.sprite != null;
            }

            if (stageFrame != null)
            {
                stageFrame.enabled = stageImage != null && stageImage.sprite != null;
            }
        }

        /// <summary>
        /// 刷新奖励区域。
        /// </summary>
        private void RefreshRewardPanel()
        {
            if (_currentChapterSetting?.Levels == null || _currentChapterSetting.Levels.Count == 0)
            {
                ClearRewardPanel();
                return;
            }

            string levelKey = MapNames.GetLevelKey(_currentChapter, _selectedLevelIndex);

            if (rewardTitleText != null)
            {
                rewardTitleText.text = "关卡奖励";
            }

            RefreshRewardCells(rewardRoot, LevelSettingHelper.GetReward(levelKey));
            RefreshFirstRewardPanel(levelKey);
        }

        /// <summary>
        /// 清空奖励预览区域。
        /// </summary>
        private void ClearRewardPanel()
        {
            if (rewardRoot != null)
            {
                UITool.SetCellListCount(rewardRoot, 0);
            }

            if (firstRewardRoot != null)
            {
                UITool.SetCellListCount(firstRewardRoot, 0);
            }
        }

        /// <summary>
        /// 刷新通关奖励列表。
        /// </summary>
        private void RefreshRewardCells(Transform parent, List<Item> rewards)
        {
            if (parent == null)
            {
                return;
            }

            UITool.SetCellListCount(parent, rewards?.Count ?? 0);
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }

            var cells = parent.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < rewards.Count && i < cells.Length; i++)
            {
                cells[i].SetItem(rewards[i], UIItemTextType.Default);
            }
        }

        /// <summary>
        /// 刷新首次通关奖励预览。
        /// </summary>
        private void RefreshFirstRewardPanel(string levelKey)
        {
            if (firstRewardRoot == null)
            {
                return;
            }

            var firstRewards = LevelSettingHelper.GetFirstReward(levelKey);
            UITool.SetCellListCount(firstRewardRoot, firstRewards?.Count ?? 0);
            if (firstRewards == null || firstRewards.Count == 0)
            {
                return;
            }

            var levelInfo = LevelInfo.ParseString(levelKey);
            bool hasFirstReward = PlayerSaveData.LocalSavaData.levelPassData.HasGetFirstReward(levelInfo.chapter, levelInfo.index);
            var cells = firstRewardRoot.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < firstRewards.Count && i < cells.Length; i++)
            {
                Item reward = firstRewards[i];
                cells[i].SetItem(reward, UIItemTextType.NoNum);
                Transform maskTf = cells[i].transform.Find("hasGet");
                if (maskTf != null)
                {
                    maskTf.gameObject.SetActive(hasFirstReward);
                }
            }
        }

        /// <summary>
        /// 刷新底部战力与次数信息。
        /// </summary>
        private void RefreshBottomInfo()
        {
            if (powerText != null)
            {
                int recommendedPower = GetRecommendedPower(_currentChapterSetting);
                powerText.text = $"推荐战力：{recommendedPower}";
            }
        }

        /// <summary>
        /// 生成章节简介文本。
        /// </summary>
        private static string BuildChapterDesc(ChapterSetting chapterSetting, int selectedLevelIndex)
        {
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return "暂无章节信息。";
            }

            string levelKey = MapNames.GetLevelKey(chapterSetting.Id, selectedLevelIndex);
            LevelSetting selected = LubanTables.GetLevelSetting(levelKey);
            string levelTitle = selected != null ? selected.Title : "当前关卡";
            return $"{levelTitle}\n当前章节包含 {chapterSetting.Levels.Count} 个关卡，适合继续推进主线进度。";
        }

        /// <summary>
        /// 读取章节展示图。
        /// </summary>
        private static Sprite LoadStageSprite(ChapterSetting chapterSetting)
        {
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return null;
            }

            string levelKey = MapNames.GetLevelKey(chapterSetting.Id, chapterSetting.Levels[0]);
            var setting = LubanTables.GetLevelSetting(levelKey);
            if (setting == null || string.IsNullOrEmpty(setting.ImgName))
            {
                return null;
            }

            return ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/Map/{setting.ImgName}.png");
        }

        /// <summary>
        /// 估算推荐战力。
        /// </summary>
        private static int GetRecommendedPower(ChapterSetting chapterSetting)
        {
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return 0;
            }

            string levelKey = MapNames.GetLevelKey(chapterSetting.Id, chapterSetting.Levels[0]);
            LevelSetting setting = LubanTables.GetLevelSetting(levelKey);
            return setting == null ? 0 : setting.EnemyBaseLevel * 100;
        }

        /// <summary>
        /// 统计当前章节已通关关卡数。
        /// </summary>
        private static int GetPassCount(ChapterSetting chapterSetting)
        {
            if (chapterSetting?.Levels == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < chapterSetting.Levels.Count; i++)
            {
                int levelIndex = chapterSetting.Levels[i];
                if (PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapterSetting.Id, levelIndex) ==
                    LevelPassState.Pass)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 选择某个关卡并刷新右侧详情。
        /// </summary>
        private void OnSelectLevel(int levelIndex)
        {
            if (_currentChapterSetting == null)
            {
                return;
            }

            _selectedLevelIndex = levelIndex;
            RefreshStageHeader();
            RefreshRewardPanel();
            RefreshBottomInfo();
            SetLevelBtnSelected(levelIndex);
        }

        /// <summary>
        /// 获取当前章节中编号最大的可挑战关卡。
        /// </summary>
        private static int GetLatestChallengeableLevel(ChapterSetting chapterSetting)
        {
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return 0;
            }

            int latestLevelIndex = int.MinValue;
            for (int i = 0; i < chapterSetting.Levels.Count; i++)
            {
                int levelIndex = chapterSetting.Levels[i];
                if (PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapterSetting.Id, levelIndex) !=
                    LevelPassState.Lock)
                {
                    latestLevelIndex = Mathf.Max(latestLevelIndex, levelIndex);
                }
            }

            return latestLevelIndex == int.MinValue ? chapterSetting.Levels[0] : latestLevelIndex;
        }

        /// <summary>
        /// 刷新关卡列表中的选中状态。
        /// </summary>
        private void SetLevelBtnSelected(int levelIndex)
        {
            if (levelListRoot == null || _currentChapterSetting?.Levels == null)
            {
                return;
            }

            LevelBtn[] levelBtns = levelListRoot.GetComponentsInChildren<LevelBtn>(true);
            for (int i = 0; i < _currentChapterSetting.Levels.Count && i < levelBtns.Length; i++)
            {
                var levelBtn = levelBtns[i];
                levelBtn.SetSelected(_currentChapterSetting.Levels[i] == levelIndex);
            }
        }

        /// <summary>
        /// 将自动选中的关卡直接移动到纵向滚动视口中间。
        /// </summary>
        private void ScrollToSelectedLevelWithoutAnimation()
        {
            if (levelListScrollRect == null || levelListRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelListRoot);

            LevelBtn[] levelBtns = levelListRoot.GetComponentsInChildren<LevelBtn>(true);
            RectTransform selectedLevel = null;
            for (int i = 0; i < levelBtns.Length; i++)
            {
                LevelBtn levelBtn = levelBtns[i];
                if (levelBtn.gameObject.activeSelf && levelBtn.LevelIndex == _selectedLevelIndex)
                {
                    selectedLevel = levelBtn.transform as RectTransform;
                    break;
                }
            }

            RectTransform viewport = levelListScrollRect.viewport;
            RectTransform content = levelListScrollRect.content != null
                ? levelListScrollRect.content
                : levelListRoot;
            if (selectedLevel == null || viewport == null || content == null)
            {
                return;
            }

            Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, content);
            float hiddenLength = contentBounds.size.y - viewport.rect.height;
            if (hiddenLength <= 0f)
            {
                levelListScrollRect.verticalNormalizedPosition = 1f;
                levelListScrollRect.velocity = Vector2.zero;
                return;
            }

            Bounds selectedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selectedLevel);
            float targetShift = viewport.rect.center.y - selectedBounds.center.y;
            float normalizedPosition =
                levelListScrollRect.verticalNormalizedPosition - targetShift / hiddenLength;

            levelListScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            levelListScrollRect.velocity = Vector2.zero;
        }

        /// <summary>
        /// 进入当前章节的首关。
        /// </summary>
        private void OnClickEnter()
        {
            if (_currentChapterSetting?.Levels == null || _currentChapterSetting.Levels.Count == 0)
            {
                return;
            }

            string levelKey = MapNames.GetLevelKey(_currentChapter, _selectedLevelIndex);
            GameMgr.Inst.LoadLevelScene(levelKey);
        }

        private void OnClickChapter()
        {
            chaptersView.Show(_currentChapter);
            levelView.gameObject.SetActive(false);
        }

        void OnChapterSelected(int chapterIndex)
        {
            levelView.SetActive(true);
            ShowChapter(chapterIndex);
        }
        
        /// <summary>
        /// 返回上一层界面。
        /// </summary>
        private void OnClickBack()
        {
            UIMgr.Inst.HideView(PanelType);
        }

        /// <summary>
        /// 获取当前应展示的章节。
        /// </summary>
        private int GetCurrentChapter()
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

        /// <summary>
        /// 判断章节是否已解锁。
        /// </summary>
        private static bool IsChapterUnlocked(int chapter)
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

        /// <summary>
        /// 缓存关卡列表根节点和滚动组件。
        /// </summary>
        private bool CacheLevelListRoot()
        {
            if (levelListRoot == null)
            {
                Transform root = transform.Find("LeftUI/Sliding Area/Viewport/Content");
                if (root == null)
                {
                    root = transform.Find("Sliding Area/Viewport/Content");
                }

                levelListRoot = root as RectTransform;
            }

            if (levelListScrollRect == null && levelListRoot != null)
            {
                levelListScrollRect = levelListRoot.GetComponentInParent<ScrollRect>();
            }

            return levelListRoot != null;
        }
    }
}
