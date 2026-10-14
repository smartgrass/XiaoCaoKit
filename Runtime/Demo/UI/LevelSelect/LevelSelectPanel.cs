using System.Collections.Generic;
using cfg;
using DG.Tweening;
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

        //方向键切换关卡时列表滚动的时间
        private const float ScrollDuration = 0.15f;

        //与玩家移动一致的垂直轴: 上(w/↑)为负方向, 下(s/↓)为正方向
        private const string VerticalAxis = "Vertical";
        private const float AxisDeadZone = 0.5f;
        //长按方向键的重复节奏: 首次间隔后转为持续重复
        private const float FirstRepeatDelay = 0.35f;
        private const float RepeatInterval = 0.12f;

        private float _axisRepeatTimer;
        private int _axisHeldStep;

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
            //打开面板时仍按住的方向键视为已消费, 避免立刻切换选中关卡
            _axisHeldStep = GetAxisStep();
            _axisRepeatTimer = FirstRepeatDelay;
        }

        /// <summary>
        /// 隐藏面板。
        /// </summary>
        public override void Hide()
        {
            //中断可能还在进行的列表滚动
            levelListScrollRect.DOKill();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 读取玩家的垂直轴输入, 按上下键(或 W/S)切换选中的关卡。
        /// </summary>
        private void Update()
        {
            //章节列表展开时, 关卡列表不响应键盘
            if (!levelView.activeSelf)
            {
                _axisHeldStep = 0;
                _axisRepeatTimer = 0f;
                return;
            }

            int step = GetAxisStep();
            if (step == 0)
            {
                //松开后重置, 下一次按下立即响应
                _axisHeldStep = 0;
                _axisRepeatTimer = 0f;
                return;
            }

            if (step != _axisHeldStep)
            {
                //新按下(含反向): 立即切换一次
                _axisHeldStep = step;
                _axisRepeatTimer = FirstRepeatDelay;
                SelectLevelByStep(step);
                return;
            }

            _axisRepeatTimer -= Time.unscaledDeltaTime;
            if (_axisRepeatTimer <= 0f)
            {
                _axisRepeatTimer = RepeatInterval;
                SelectLevelByStep(step);
            }
        }

        /// <summary>
        /// 读取垂直轴方向: 上为 -1(选中上一关), 下为 1(选中下一关), 未按下为 0。
        /// </summary>
        private static int GetAxisStep()
        {
            float axis = Input.GetAxisRaw(VerticalAxis);
            return axis >= AxisDeadZone ? -1 : axis <= -AxisDeadZone ? 1 : 0;
        }

        /// <summary>
        /// 空格键等同于点击挑战按钮。
        /// </summary>
        public override void InputKeyCode(KeyCode key)
        {
            if (key == XCInputSetting.Space && levelView.activeSelf && enterBtn.interactable)
            {
                OnClickEnter();
            }
        }

        /// <summary>
        /// 按步进切换选中的关卡, 已在首尾时保持当前选中。
        /// </summary>
        private void SelectLevelByStep(int step)
        {
            if (_currentChapterSetting == null || _currentChapterSetting.MaxLevel <= 0)
            {
                return;
            }

            int targetLevelIndex = Mathf.Clamp(_selectedLevelIndex + step, 1, _currentChapterSetting.MaxLevel);
            if (targetLevelIndex == _selectedLevelIndex)
            {
                return;
            }

            OnSelectLevel(targetLevelIndex);
            ScrollToSelectedLevel(true);
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
            RefreshEnterBtnState();
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
            if (!CacheLevelListRoot())
            {
                return;
            }

            UITool.SetCellListCount(levelListRoot, _currentChapterSetting.MaxLevel);
            LevelBtn[] levelBtns = levelListRoot.GetComponentsInChildren<LevelBtn>(true);
            for (int i = 0; i < _currentChapterSetting.MaxLevel && i < levelBtns.Length; i++)
            {
                int levelIndex = i + 1;
                LevelBtn levelBtn = levelBtns[i];
                levelBtn.Show(_currentChapter, levelIndex);
                levelBtn.SetSelected(false);
                levelBtn.onClick = () => { OnSelectLevel(levelIndex); };
            }

            SetLevelBtnSelected(_selectedLevelIndex);
            ScrollToSelectedLevel(false);
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
            if (_currentChapterSetting.MaxLevel == 0)
            {
                ClearRewardPanel();
                return;
            }

            string levelKey = LevelInfo.GetLevelKey(_currentChapter, _selectedLevelIndex);

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
                cells[i].clickMode = EItemClickMode.ShowDetail;
                cells[i].SetItem(rewards[i]);
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
            bool hasFirstReward =
                PlayerSaveData.LocalSavaData.levelPassData.HasGetFirstReward(levelInfo.chapter, levelInfo.index);
            var cells = firstRewardRoot.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < firstRewards.Count && i < cells.Length; i++)
            {
                Item reward = firstRewards[i];
                cells[i].SetItem(reward);
                cells[i].clickMode = EItemClickMode.ShowDetail;
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
        /// 读取章节展示图。
        /// </summary>
        private static Sprite LoadStageSprite(ChapterSetting chapterSetting)
        {
            string levelKey = LevelInfo.GetLevelKey(chapterSetting.Id, 1);
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
            string levelKey = LevelInfo.GetLevelKey(chapterSetting.Id, 1);
            LevelSetting setting = LubanTables.GetLevelSetting(levelKey);
            return setting == null ? 0 : setting.EnemyBaseLevel * 100;
        }

        /// <summary>
        /// 统计当前章节已通关关卡数。
        /// </summary>
        private static int GetPassCount(ChapterSetting chapterSetting)
        {
            int count = 0;
            for (int i = 0; i < chapterSetting.MaxLevel; i++)
            {
                int levelIndex = i + 1;
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
            RefreshEnterBtnState();
            SetLevelBtnSelected(levelIndex);
        }

        /// <summary>
        /// 选中未解锁关卡时关闭挑战按钮的点击功能。
        /// </summary>
        private void RefreshEnterBtnState()
        {
            LevelPassState passState =
                PlayerSaveData.LocalSavaData.levelPassData.GetPassState(_currentChapter, _selectedLevelIndex);
            enterBtn.interactable = passState != LevelPassState.Lock;
        }

        /// <summary>
        /// 获取当前章节中编号最大的可挑战关卡。
        /// </summary>
        private static int GetLatestChallengeableLevel(ChapterSetting chapterSetting)
        {
            int latestLevelIndex = int.MinValue;
            for (int i = 0; i < chapterSetting.MaxLevel; i++)
            {
                int levelIndex = i + 1;
                if (PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapterSetting.Id, levelIndex) !=
                    LevelPassState.Lock)
                {
                    latestLevelIndex = Mathf.Max(latestLevelIndex, levelIndex);
                }
            }

            return latestLevelIndex == int.MinValue ? 1 : latestLevelIndex;
        }

        /// <summary>
        /// 刷新关卡列表中的选中状态。
        /// </summary>
        private void SetLevelBtnSelected(int levelIndex)
        {
            if (levelListRoot == null)
            {
                return;
            }

            LevelBtn[] levelBtns = levelListRoot.GetComponentsInChildren<LevelBtn>(true);
            for (int i = 0; i < _currentChapterSetting.MaxLevel && i < levelBtns.Length; i++)
            {
                var levelBtn = levelBtns[i];
                //默认从1开始算
                levelBtn.SetSelected(i + 1 == levelIndex);
            }
        }

        /// <summary>
        /// 滚动关卡列表, 让选中的关卡处于纵向滚动视口中间; animated 为 true 时使用缓动。
        /// </summary>
        private void ScrollToSelectedLevel(bool animated)
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

            levelListScrollRect.velocity = Vector2.zero;
            normalizedPosition = Mathf.Clamp01(normalizedPosition);
            if (!animated)
            {
                levelListScrollRect.verticalNormalizedPosition = normalizedPosition;
                return;
            }

            //键盘切换时平滑滚动, 避免列表位置突变
            levelListScrollRect.DOKill();
            DOTween.To(
                    () => levelListScrollRect.verticalNormalizedPosition,
                    value => levelListScrollRect.verticalNormalizedPosition = value,
                    normalizedPosition, ScrollDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(levelListScrollRect);
        }

        /// <summary>
        /// 进入当前章节的首关。
        /// </summary>
        private void OnClickEnter()
        {
            string levelKey = LevelInfo.GetLevelKey(_currentChapter, _selectedLevelIndex);
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
            if (chapterSetting == null)
            {
                return false;
            }

            int firstLevelIndex = 1;
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
