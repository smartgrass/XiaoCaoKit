using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCao.UI
{
    /// <summary>
    /// Home 场景的新手引导控制器，负责串联角色升级、固定技能升级和第二关入口引导。
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeGuideController : MonoBehaviour
    {
        private const string GuideStoryId = "story_home_guide";
        private const string GuideStepKey = "home_guide_step";
        private const string GuideWaitReturnHomeKey = "home_guide_wait_return_home";
        private const string GuideDebugResetKey = "home_guide_debug_reset";
        private const int GuideChapter = 0;
        private const int GuideFirstLevelIndex = 1;
        private const int GuideSecondLevelIndex = 2;
        private const string GuideSecondLevelKey = "level_0_2";
        private const string GuideSkillId = "3";
        private const string GuideRolePanelName = "Role";
        private const string GuideSkillPanelName = "Skill";

        private static readonly string OpenRoleUpgradeGuideText = LocalizeKey.GuideOpenGrowPanel;
        private static readonly string OpenRoleTabGuideText = LocalizeKey.GuideOpenRoleTab;
        private static readonly string RoleUpgradeGuideText = LocalizeKey.GuideRoleUpgrade;
        private static readonly string OpenSkillViewGuideText = LocalizeKey.GuideOpenSkillView;
        private static readonly string OpenSkillTabGuideText = LocalizeKey.GuideOpenSkillTab;
        private static readonly string SelectSkillGuideText = LocalizeKey.GuideSelectSkill3;
        private static readonly string CloseGrowPanelGuideText = LocalizeKey.GuideCloseGrowPanel;
        private static readonly string OpenFightPanelGuideText = LocalizeKey.GuideOpenFightPanel;
        private static readonly string SelectSecondLevelGuideText = LocalizeKey.GuideSelectSecondLevel;

        private enum GuideStep
        {
            None = 0,
            RoleUpgrade = 1,
            SkillUpgrade = 2,
            CloseGrowPanel = 3,
            EnterSecondLevel = 4,
            Completed = 5,
        }

        private HomeHud _homeHud;
        private HomeMainPanel _mainPanel;
        private HomeFightPanel _fightPanel;
        private RoleView _roleView;
        private SkillView _skillView;
        private PlayerTabPanel _playerTabPanel;
        private LevelDetailUI _levelDetailUI;
        private Canvas _rootCanvas;

        private RectTransform _guideRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _highlightRect;
        private RectTransform _proxyRect;
        private RectTransform _tipRect;
        private Image _blockerImage;
        private Button _proxyButton;
        private Button _blockerButton;
        private TextMeshProUGUI _tipText;
        private Material _blockerMaterial;

        private RectTransform _currentTarget;
        private Action _currentClickAction;
        private string _currentTargetKey = string.Empty;
        private GuideStep _currentStep;
        private int _roleLevelSnapshot;
        private int _skillLevelSnapshot;
        private float _highlightPulseTime;
        private bool _isReady;
        private bool _isGuideActive;
        private bool _forceStartFromRoleUpgrade;

        [Header("Guide Debug")]
        [SerializeField] private GuideStep _debugCurrentStep;
        [SerializeField] private bool _debugIsWaitingReturnHomeAfterSkillUpgrade;
        [SerializeField] private string _debugCurrentTargetKey = string.Empty;
        [SerializeField] private string _debugGuideMessage = string.Empty;
        [SerializeField] private RectTransform _debugNextGuideButtonTransform;

        private static readonly int HoleCenterShaderId = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleSizeShaderId = Shader.PropertyToID("_HoleSize");
        private static readonly int HoleRadiusShaderId = Shader.PropertyToID("_HoleRadius");
        private static readonly int EdgeSoftnessShaderId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int HoleEnabledShaderId = Shader.PropertyToID("_HoleEnabled");

        private void Awake()
        {
            _guideRoot = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>();
            ResolveHomeHud();
            SetGuideVisibility(false);
        }

        private IEnumerator Start()
        {
            while (!ResMgr.IsLoadBaseFinish)
            {
                yield return null;
            }

            yield return null;
            EnsureGuideView();
            HideGuideView();
            BindViews();
            _isReady = true;
            TryEnterGuide();
        }

        private void Update()
        {
            if (!_isReady)
            {
                RefreshGuideDebugInfo();
                return;
            }

            BindViews();

            if (!_isGuideActive)
            {
                TryEnterGuide();
                RefreshGuideDebugInfo();
                return;
            }

            CheckStepCompletion();
            RefreshGuideByStep();
            RefreshGuideVisual();
            RefreshGuideDebugInfo();
        }

        /// <summary>
        /// 由 HomeHud 在实例化完成后注入 Home 入口引用。
        /// </summary>
        public void Initialize(HomeHud homeHud)
        {
            _homeHud = homeHud;
            _rootCanvas = homeHud != null ? homeHud.Canvas : _rootCanvas;
            _guideRoot ??= transform as RectTransform;
            _canvasGroup ??= GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 供调试入口重置新手引导，并在 Home 中立即重新开始第一步。
        /// </summary>
        public void ResetGuideForDebug()
        {
            if (!ResetGuideProgressOnly())
            {
                return;
            }

            ResetRuntimeGuideState();
            _forceStartFromRoleUpgrade = true;
            TryEnterGuide();
        }

        /// <summary>
        /// 判断当前新手引导存档是否已经处于“重置后待重新开始”的状态。
        /// </summary>
        public static bool IsGuideResetForDebug()
        {
            if (!TryGetGuideStoryProgressForDebug(out StoryProgress storyProgress))
            {
                return false;
            }

            return !storyProgress.IsStoryCompleted(GuideStoryId) &&
                   storyProgress.GetStoryVariable(GuideDebugResetKey) > 0;
        }

        /// <summary>
        /// 供调试入口强制标记新手引导已完成，并立即结束当前 Home 中的引导显示。
        /// </summary>
        public void CompleteGuideForDebug()
        {
            if (!CompleteGuideProgressOnly())
            {
                return;
            }

            MarkGuideCompleted();
        }

        /// <summary>
        /// 只清理新手引导存档标记，不直接操作当前 Home 界面。
        /// </summary>
        public static bool ResetGuideProgressOnly()
        {
            if (!TryGetGuideStoryProgressForDebug(out StoryProgress storyProgress))
            {
                return false;
            }

            storyProgress.completedStoryIds.Remove(GuideStoryId);
            storyProgress.SetStoryVariable(GuideStepKey, (int)GuideStep.None);
            storyProgress.SetStoryVariable(GuideWaitReturnHomeKey, 0);
            storyProgress.SetStoryVariable(GuideDebugResetKey, 1);
            PlayerSaveData.SavaData();
            return true;
        }

        /// <summary>
        /// 只标记新手引导存档为已完成，不直接操作当前 Home 界面。
        /// </summary>
        public static bool CompleteGuideProgressOnly()
        {
            if (!TryGetGuideStoryProgressForDebug(out StoryProgress storyProgress))
            {
                return false;
            }

            storyProgress.SetStoryVariable(GuideStepKey, (int)GuideStep.Completed);
            storyProgress.SetStoryVariable(GuideWaitReturnHomeKey, 0);
            storyProgress.SetStoryVariable(GuideDebugResetKey, 0);
            storyProgress.AddCompletedStory(GuideStoryId);
            PlayerSaveData.SavaData();
            return true;
        }

        private static bool TryGetGuideStoryProgressForDebug(out StoryProgress storyProgress)
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null)
            {
                Debug.LogWarning("当前运行时没有可用的玩家存档，无法修改新手引导。");
                storyProgress = null;
                return false;
            }

            playerSaveData.storyProgress ??= new StoryProgress();
            playerSaveData.storyProgress.CheckNull();
            storyProgress = playerSaveData.storyProgress;
            return true;
        }

        /// <summary>
        /// 创建引导遮罩、高亮和提示面板。
        /// </summary>
        private void EnsureGuideView()
        {
            _guideRoot ??= transform as RectTransform;
            _canvasGroup ??= GetComponent<CanvasGroup>();
            if (_guideRoot == null)
            {
                Debug.LogError("新手引导 HomeGuideRoot 缺少 RectTransform。");
                return;
            }

            if (_highlightRect != null &&
                _proxyRect != null &&
                _tipRect != null &&
                _blockerImage != null &&
                _proxyButton != null &&
                _blockerButton != null &&
                _tipText != null)
            {
                _guideRoot.SetAsLastSibling();
                return;
            }

            StretchFullScreen(_guideRoot);
            _guideRoot.SetAsLastSibling();

            _highlightRect = FindGuideRect("Highlight");
            _proxyRect = FindGuideRect("ProxyButton");
            _tipRect = FindGuideRect("Tip");
            _blockerImage = FindGuideComponent<Image>("Blocker");
            _blockerButton = FindGuideComponent<Button>("Blocker");
            _proxyButton = FindGuideComponent<Button>("ProxyButton");
            _tipText = FindGuideComponent<TextMeshProUGUI>("Tip/TipText");

            CacheBlockerMaterial();
            _blockerButton?.onClick.AddListener(OnBlockerClick);
            _proxyButton?.onClick.AddListener(OnProxyClick);
        }

        /// <summary>
        /// 绑定 Home 场景中需要被引导系统使用的页面和控件。
        /// </summary>
        private void BindViews()
        {
            ResolveHomeHud();
            if (_homeHud == null)
            {
                return;
            }

            _mainPanel ??= _homeHud.GetComponentInChildren<HomeMainPanel>(true);
            _fightPanel ??= _homeHud.GetComponentInChildren<HomeFightPanel>(true);
            _roleView ??= _homeHud.GetComponentInChildren<RoleView>(true);
            _skillView ??= _homeHud.GetComponentInChildren<SkillView>(true);
            _playerTabPanel ??= _homeHud.GetComponentInChildren<PlayerTabPanel>(true);
            _levelDetailUI ??= _fightPanel != null
                ? _fightPanel.levelDetailUI
                : _homeHud.GetComponentInChildren<LevelDetailUI>(true);
        }

        /// <summary>
        /// 判断是否应该进入或恢复新手引导。
        /// </summary>
        private void TryEnterGuide()
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null || playerSaveData.storyProgress == null)
            {
                return;
            }

            if (_forceStartFromRoleUpgrade)
            {
                EnsureMainRoleView();
                if (!IsRoleUpgradeGuideReady())
                {
                    HideGuideView();
                    return;
                }

                _forceStartFromRoleUpgrade = false;
                ApplyGuideStep(GuideStep.RoleUpgrade);
                return;
            }

            if (playerSaveData.storyProgress.IsStoryCompleted(GuideStoryId))
            {
                HideGuideView();
                return;
            }

            GuideStep savedStep = (GuideStep)playerSaveData.storyProgress.GetStoryVariable(GuideStepKey);
            if (savedStep >= GuideStep.Completed)
            {
                MarkGuideCompleted();
                return;
            }

            if (savedStep == GuideStep.CloseGrowPanel || savedStep == GuideStep.EnterSecondLevel)
            {
                SetWaitingReturnHomeAfterSkillUpgrade(false);
            }

            if (savedStep == GuideStep.CloseGrowPanel && IsReadyToResumeEnterSecondLevelGuide())
            {
                ApplyGuideStep(GuideStep.EnterSecondLevel);
                return;
            }

            if (savedStep > GuideStep.None && savedStep < GuideStep.Completed)
            {
                if (!CanEnterGuideStep(savedStep))
                {
                    HideGuideView();
                    return;
                }

                ApplyGuideStep(savedStep);
                return;
            }

            if (!HasPassedGuideFirstLevel())
            {
                HideGuideView();
                return;
            }

            EnsureMainRoleView();
            if (!IsRoleUpgradeGuideReady())
            {
                HideGuideView();
                return;
            }

            ApplyGuideStep(GuideStep.RoleUpgrade);
        }

        /// <summary>
        /// 应用并保存当前引导步骤。
        /// </summary>
        private void ApplyGuideStep(GuideStep step)
        {
            _currentStep = step;
            _isGuideActive = step > GuideStep.None && step < GuideStep.Completed;

            if (!_isGuideActive)
            {
                HideGuideView();
                return;
            }

            SaveGuideStep(step);
            PrepareStep(step);
            ShowGuideView();
        }

        /// <summary>
        /// 进入某一步前做页面准备，并记录完成判定所需快照。
        /// </summary>
        private void PrepareStep(GuideStep step)
        {
            switch (step)
            {
                case GuideStep.RoleUpgrade:
                    EnsureMainRoleView();
                    _roleLevelSnapshot = PlayerSaveData.LocalSavaData.lv;
                    break;
                case GuideStep.SkillUpgrade:
                    EnsureMainPanel();
                    _skillLevelSnapshot = GetGuideSkillLevel();
                    break;
                case GuideStep.CloseGrowPanel:
                    EnsureMainPanel();
                    SetWaitingReturnHomeAfterSkillUpgrade(false);
                    break;
                case GuideStep.EnterSecondLevel:
                    EnsureMainPanel();
                    SetWaitingReturnHomeAfterSkillUpgrade(false);
                    if (_levelDetailUI != null && _levelDetailUI.gameObject.activeSelf)
                    {
                        _levelDetailUI.Hide();
                    }

                    break;
            }

            _currentTarget = null;
            _currentClickAction = null;
            _currentTargetKey = string.Empty;
            _highlightPulseTime = 0f;
            SetBlockerHoleEnabled(false);
        }

        /// <summary>
        /// 检查当前步骤是否已经完成。
        /// </summary>
        private void CheckStepCompletion()
        {
            switch (_currentStep)
            {
                case GuideStep.RoleUpgrade:
                    if (PlayerSaveData.LocalSavaData.lv > _roleLevelSnapshot)
                    {
                        ApplyGuideStep(GuideStep.SkillUpgrade);
                    }

                    break;
                case GuideStep.SkillUpgrade:
                    if (GetGuideSkillLevel() > _skillLevelSnapshot)
                    {
                        EnterCloseGrowPanelGuideAfterSkillUpgrade();
                    }

                    break;
                case GuideStep.CloseGrowPanel:
                    if (IsReadyToResumeEnterSecondLevelGuide())
                    {
                        ApplyGuideStep(GuideStep.EnterSecondLevel);
                    }

                    break;
            }
        }

        /// <summary>
        /// 根据当前步骤刷新高亮目标。
        /// </summary>
        private void RefreshGuideByStep()
        {
            switch (_currentStep)
            {
                case GuideStep.RoleUpgrade:
                    RefreshRoleUpgradeGuide();
                    break;
                case GuideStep.SkillUpgrade:
                    RefreshSkillUpgradeGuide();
                    break;
                case GuideStep.CloseGrowPanel:
                    RefreshCloseGrowPanelGuide();
                    break;
                case GuideStep.EnterSecondLevel:
                    RefreshEnterLevelGuide();
                    break;
            }
        }

        /// <summary>
        /// 刷新角色升级步骤的高亮目标。
        /// </summary>
        private void RefreshRoleUpgradeGuide()
        {
            EnsureMainRoleView();
            if (_mainPanel == null)
            {
                return;
            }

            if (!_mainPanel.IsSubViewActive(EHomeSubView.Skill))
            {
                SetGuideTarget(
                    _mainPanel.growBtn.transform as RectTransform,
                    OpenRoleUpgradeGuideText,
                    () => _mainPanel.growBtn.onClick.Invoke(),
                    "open_role_upgrade_view");
                return;
            }

            Button roleTabButton = GetGuideTabButton(GuideRolePanelName);
            if (!IsGuideTabActive(GuideRolePanelName) && roleTabButton != null)
            {
                SetGuideTarget(
                    roleTabButton.transform as RectTransform,
                    OpenRoleTabGuideText,
                    () => roleTabButton.onClick.Invoke(),
                    "open_role_tab");
                return;
            }

            if (_roleView == null || _roleView.upgradeBtn == null)
            {
                return;
            }

            _roleView.RefreshGuideUI();
            SetGuideTarget(
                _roleView.upgradeBtn.transform as RectTransform,
                RoleUpgradeGuideText,
                OnRoleUpgradeByGuide,
                "role_upgrade");
        }

        /// <summary>
        /// 刷新技能升级步骤的高亮目标，只允许升级技能 3。
        /// </summary>
        private void RefreshSkillUpgradeGuide()
        {
            EnsureMainPanel();
            if (_mainPanel == null || _skillView == null)
            {
                return;
            }

            if (!_mainPanel.IsSubViewActive(EHomeSubView.Skill))
            {
                SetGuideTarget(
                    _mainPanel.growBtn.transform as RectTransform,
                    OpenSkillViewGuideText,
                    () => _mainPanel.growBtn.onClick.Invoke(),
                    "open_skill_view");
                return;
            }

            Button skillTabButton = GetGuideTabButton(GuideSkillPanelName);
            if (!IsGuideTabActive(GuideSkillPanelName) && skillTabButton != null)
            {
                SetGuideTarget(
                    skillTabButton.transform as RectTransform,
                    OpenSkillTabGuideText,
                    () => skillTabButton.onClick.Invoke(),
                    "open_skill_tab");
                return;
            }

            _skillView.RefreshGuideUI();
            SkillDetailUI skillDetailUI = _skillView.GetSkillDetailUI();

            if (skillDetailUI != null && skillDetailUI.gameObject.activeSelf)
            {
                if (skillDetailUI.skillId == GuideSkillId)
                {
                    SetGuideTarget(
                        skillDetailUI.upgradeBtn.transform as RectTransform,
                        string.Empty,
                        OnSkillUpgradeByGuide,
                        "skill_upgrade_btn");
                    return;
                }

                skillDetailUI.OnHide();
            }

            SkillItemCell skillCell = _skillView.GetSkillCell(GuideSkillId);
            if (skillCell == null)
            {
                return;
            }

            SetGuideTarget(
                skillCell.transform as RectTransform,
                SelectSkillGuideText,
                () => skillCell.clickAct?.Invoke(),
                $"skill_cell_{GuideSkillId}");
        }

        /// <summary>
        /// 刷新技能升级后的返回首页步骤，引导玩家点击成长面板关闭按钮。
        /// </summary>
        private void RefreshCloseGrowPanelGuide()
        {
            EnsureMainPanel();
            if (_mainPanel == null || _playerTabPanel == null || _playerTabPanel.closeBtn == null)
            {
                return;
            }

            if (_mainPanel.IsSubViewActive(EHomeSubView.Main))
            {
                return;
            }

            SetGuideTarget(
                _playerTabPanel.closeBtn.transform as RectTransform,
                CloseGrowPanelGuideText,
                OnCloseGrowPanelByGuide,
                "close_grow_panel");
        }

        /// <summary>
        /// 刷新第二关入口步骤的高亮目标，保持 Fight -> 第二关 的入口节奏。
        /// </summary>
        private void RefreshEnterLevelGuide()
        {
            if (_mainPanel == null || _fightPanel == null)
            {
                return;
            }

            if (!_homeHud.IsPanelActive(EHomePanel.FightPanel))
            {
                SetGuideTarget(
                    _mainPanel.fightBtn.transform as RectTransform,
                    OpenFightPanelGuideText,
                    () => _mainPanel.fightBtn.onClick.Invoke(),
                    "open_fight_panel");
                return;
            }

            if (_fightPanel.curChapter != GuideChapter)
            {
                _fightPanel.ShowChapter(GuideChapter);
            }

            if (_levelDetailUI != null && _levelDetailUI.gameObject.activeSelf)
            {
                if (_levelDetailUI.IsShowingLevel(GuideSecondLevelKey))
                {
                    MarkGuideCompleted();
                    return;
                }

                _levelDetailUI.Hide();
            }

            LevelBtn levelBtn = _fightPanel.GetLevelButton(GuideChapter, GuideSecondLevelIndex);
            if (levelBtn == null)
            {
                return;
            }

            SetGuideTarget(
                levelBtn.transform as RectTransform,
                SelectSecondLevelGuideText,
                () => OnSelectSecondLevelByGuide(levelBtn),
                "select_second_level");
        }

        /// <summary>
        /// 点击第二关按钮时立即记录引导完成，并继续执行原始关卡打开逻辑。
        /// </summary>
        private void OnSelectSecondLevelByGuide(LevelBtn levelBtn)
        {
            MarkGuideCompleted();
            levelBtn?.onClick?.Invoke();
        }

        /// <summary>
        /// 技能 3 升级完成后进入关闭成长面板的下一步引导。
        /// </summary>
        private void EnterCloseGrowPanelGuideAfterSkillUpgrade()
        {
            SetWaitingReturnHomeAfterSkillUpgrade(false);
            ApplyGuideStep(GuideStep.CloseGrowPanel);
        }

        /// <summary>
        /// 新手引导点击角色升级按钮时，无论材料是否足够都推进到技能升级步骤。
        /// </summary>
        private void OnRoleUpgradeByGuide()
        {
            bool shouldAdvanceToSkillStep = _currentStep == GuideStep.RoleUpgrade;
            if (_roleView != null && _roleView.upgradeBtn != null)
            {
                _roleView.upgradeBtn.onClick.Invoke();
            }

            if (shouldAdvanceToSkillStep)
            {
                ApplyGuideStep(GuideStep.SkillUpgrade);
            }
        }

        /// <summary>
        /// 新手引导点击技能升级按钮时，无论材料是否足够都进入“关闭成长面板”的下一阶段。
        /// </summary>
        private void OnSkillUpgradeByGuide()
        {
            bool shouldAdvanceToCloseGrowPanelStep = _currentStep == GuideStep.SkillUpgrade;
            SkillDetailUI skillDetailUI = _skillView != null ? _skillView.GetSkillDetailUI() : null;
            if (skillDetailUI != null && skillDetailUI.upgradeBtn != null)
            {
                skillDetailUI.upgradeBtn.onClick.Invoke();
            }

            if (shouldAdvanceToCloseGrowPanelStep)
            {
                EnterCloseGrowPanelGuideAfterSkillUpgrade();
            }
        }

        /// <summary>
        /// 新手引导点击成长面板返回按钮时，回到首页主视图并继续第二关入口引导。
        /// </summary>
        private void OnCloseGrowPanelByGuide()
        {
            bool shouldAdvanceToEnterSecondLevel = _currentStep == GuideStep.CloseGrowPanel;
            if (_playerTabPanel != null && _playerTabPanel.closeBtn != null)
            {
                _playerTabPanel.closeBtn.onClick.Invoke();
            }

            if (shouldAdvanceToEnterSecondLevel && IsReadyToResumeEnterSecondLevelGuide())
            {
                ApplyGuideStep(GuideStep.EnterSecondLevel);
            }
        }

        /// <summary>
        /// 保存当前引导步骤到存档。
        /// </summary>
        private void SaveGuideStep(GuideStep step)
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            playerSaveData.storyProgress.SetStoryVariable(GuideStepKey, (int)step);
            PlayerSaveData.SavaData();
        }

        /// <summary>
        /// 标记技能升级后是否要等待玩家回到首页主视图，再恢复第二关入口引导。
        /// </summary>
        private void SetWaitingReturnHomeAfterSkillUpgrade(bool waiting)
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null || playerSaveData.storyProgress == null)
            {
                return;
            }

            playerSaveData.storyProgress.SetStoryVariable(GuideWaitReturnHomeKey, waiting ? 1 : 0);
            PlayerSaveData.SavaData();
        }

        /// <summary>
        /// 判断当前是否处于“技能升级后等待回到首页”的隐藏阶段。
        /// </summary>
        private bool IsWaitingReturnHomeAfterSkillUpgrade()
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null || playerSaveData.storyProgress == null)
            {
                return false;
            }

            return playerSaveData.storyProgress.GetStoryVariable(GuideWaitReturnHomeKey) > 0;
        }

        /// <summary>
        /// 标记整套新手引导已完成。
        /// </summary>
        private void MarkGuideCompleted()
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            playerSaveData.storyProgress.SetStoryVariable(GuideStepKey, (int)GuideStep.Completed);
            playerSaveData.storyProgress.SetStoryVariable(GuideWaitReturnHomeKey, 0);
            playerSaveData.storyProgress.SetStoryVariable(GuideDebugResetKey, 0);
            playerSaveData.storyProgress.AddCompletedStory(GuideStoryId);
            PlayerSaveData.SavaData();

            _currentStep = GuideStep.Completed;
            _isGuideActive = false;
            HideGuideView();
        }

        /// <summary>
        /// 确保当前停留在 Home 主面板。
        /// </summary>
        private void EnsureMainPanel()
        {
            if (_homeHud != null && !_homeHud.IsPanelActive(EHomePanel.MainPanel))
            {
                _homeHud.SwitchPanel(EHomePanel.MainPanel);
            }
        }

        /// <summary>
        /// 确保当前停留在角色升级所在的主子页。
        /// </summary>
        private void EnsureMainRoleView()
        {
            EnsureMainPanel();
            _roleView?.RefreshGuideUI();
        }

        /// <summary>
        /// 判断指定引导步骤在当前 Home 状态下是否已经具备进入条件。
        /// </summary>
        private bool CanEnterGuideStep(GuideStep step)
        {
            switch (step)
            {
                case GuideStep.RoleUpgrade:
                    EnsureMainRoleView();
                    return IsRoleUpgradeGuideReady();
                case GuideStep.CloseGrowPanel:
                    return IsCloseGrowPanelGuideReady();
                case GuideStep.EnterSecondLevel:
                    return IsReadyToResumeEnterSecondLevelGuide();
                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断角色升级步骤的入口是否已经具备显示条件。
        /// </summary>
        private bool IsRoleUpgradeGuideReady()
        {
            if (_homeHud == null || _mainPanel == null)
            {
                return false;
            }

            if (!_homeHud.IsPanelActive(EHomePanel.MainPanel))
            {
                return false;
            }

            if (!_mainPanel.IsSubViewActive(EHomeSubView.Skill))
            {
                return _mainPanel.growBtn != null && _mainPanel.growBtn.gameObject.activeInHierarchy;
            }

            if (!IsGuideTabActive(GuideRolePanelName))
            {
                Button roleTabButton = GetGuideTabButton(GuideRolePanelName);
                return roleTabButton != null && roleTabButton.gameObject.activeInHierarchy;
            }

            return _roleView != null &&
                   _roleView.upgradeBtn != null &&
                   _roleView.upgradeBtn.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 判断是否已经回到 Home 首页主视图，可以恢复 Fight -> 第二关 的引导。
        /// </summary>
        private bool IsReadyToResumeEnterSecondLevelGuide()
        {
            return _homeHud != null &&
                   _mainPanel != null &&
                   _homeHud.IsPanelActive(EHomePanel.MainPanel) &&
                   _mainPanel.IsSubViewActive(EHomeSubView.Main);
        }

        /// <summary>
        /// 判断关闭成长面板步骤当前是否已经具备显示条件。
        /// </summary>
        private bool IsCloseGrowPanelGuideReady()
        {
            if (_homeHud == null || _mainPanel == null || _playerTabPanel == null || _playerTabPanel.closeBtn == null)
            {
                return false;
            }

            if (!_homeHud.IsPanelActive(EHomePanel.MainPanel))
            {
                return false;
            }

            if (_mainPanel.IsSubViewActive(EHomeSubView.Main))
            {
                return true;
            }

            return _mainPanel.IsSubViewActive(EHomeSubView.Skill) &&
                   _playerTabPanel.closeBtn.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 获取成长面板内指定页签的按钮。
        /// </summary>
        private Button GetGuideTabButton(string panelName)
        {
            if (_playerTabPanel == null || _playerTabPanel.groups == null)
            {
                return null;
            }

            for (int i = 0; i < _playerTabPanel.groups.Count; i++)
            {
                TabPanelGroup group = _playerTabPanel.groups[i];
                if (group != null && string.Equals(group.panelName, panelName, StringComparison.Ordinal))
                {
                    return group.tabBtn;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断成长面板当前是否已经切到指定页签。
        /// </summary>
        private bool IsGuideTabActive(string panelName)
        {
            if (_playerTabPanel == null || _playerTabPanel.groups == null)
            {
                return false;
            }

            for (int i = 0; i < _playerTabPanel.groups.Count; i++)
            {
                TabPanelGroup group = _playerTabPanel.groups[i];
                if (group != null &&
                    string.Equals(group.panelName, panelName, StringComparison.Ordinal) &&
                    group.panel != null)
                {
                    return group.panel.activeInHierarchy;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取引导指定技能的当前等级。
        /// </summary>
        private int GetGuideSkillLevel()
        {
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null)
            {
                return 0;
            }

            return playerSaveData.GetSkillLevel(GuideSkillId);
        }

        /// <summary>
        /// 判断玩家是否已经通关引导第一关。
        /// </summary>
        private bool HasPassedGuideFirstLevel()
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(GuideChapter, GuideFirstLevelIndex) ==
                   LevelPassState.Pass;
        }

        /// <summary>
        /// 设置当前步骤唯一可点击的引导目标。
        /// </summary>
        private void SetGuideTarget(RectTransform target, string messageKey, Action clickAction, string targetKey)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            _currentTarget = target;
            _currentClickAction = clickAction;

            if (_currentTargetKey != targetKey)
            {
                _highlightPulseTime = 0f;
                _currentTargetKey = targetKey;
            }

            string displayMessage = messageKey.ToLocalizeStr();
            if (_tipRect != null)
            {
                _tipRect.gameObject.SetActive(!string.IsNullOrEmpty(displayMessage));
            }

            if (_tipText != null)
            {
                _tipText.text = displayMessage;
            }

            _debugGuideMessage = displayMessage;
            _debugNextGuideButtonTransform = target;

            RefreshBlockerDisplayState();
            ShowGuideView();
            RefreshGuideVisual();
        }

        /// <summary>
        /// 刷新高亮框、代理点击区域和提示框的位置。
        /// </summary>
        private void RefreshGuideVisual()
        {
            if (!_isGuideActive || _currentTarget == null || _guideRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (!TryGetTargetRect(_currentTarget, out Vector2 center, out Vector2 size))
            {
                return;
            }

            Vector2 highlightSize = size + new Vector2(28f, 28f);
            _highlightRect.anchoredPosition = center;
            _highlightRect.sizeDelta = highlightSize;

            _proxyRect.anchoredPosition = center;
            _proxyRect.sizeDelta = size;

            if (ShouldShowBlockerForCurrentStep())
            {
                RefreshBlockerHole(center, highlightSize);
            }
            else
            {
                SetBlockerHoleEnabled(false);
            }

            _highlightPulseTime += Time.unscaledDeltaTime * 4f;
            float scale = 1f + Mathf.Sin(_highlightPulseTime) * 0.03f;
            _highlightRect.localScale = Vector3.one * scale;

            Vector2 tipSize = _tipRect.sizeDelta;
            float topEdge = center.y + size.y * 0.5f;
            float bottomEdge = center.y - size.y * 0.5f;
            float rootHalfHeight = _guideRoot.rect.height * 0.5f;
            float targetTopTipY = topEdge + tipSize.y * 0.5f + 36f;
            float targetBottomTipY = bottomEdge - tipSize.y * 0.5f - 36f;
            bool placeBelow = targetTopTipY > rootHalfHeight - 12f;
            float tipY = placeBelow ? targetBottomTipY : targetTopTipY;
            float rootHalfWidth = _guideRoot.rect.width * 0.5f;
            float tipX = Mathf.Clamp(
                center.x,
                -rootHalfWidth + tipSize.x * 0.5f + 12f,
                rootHalfWidth - tipSize.x * 0.5f - 12f);
            tipY = Mathf.Clamp(
                tipY,
                -rootHalfHeight + tipSize.y * 0.5f + 12f,
                rootHalfHeight - tipSize.y * 0.5f - 12f);
            _tipRect.anchoredPosition = new Vector2(tipX, tipY);
        }

        /// <summary>
        /// 计算目标控件在引导遮罩上的局部矩形。
        /// </summary>
        private bool TryGetTargetRect(RectTransform target, out Vector2 center, out Vector2 size)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            if (target == null || _guideRoot == null)
            {
                return false;
            }

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector3 localCorner = _guideRoot.InverseTransformPoint(worldCorners[i]);
                Vector2 localPoint = new Vector2(localCorner.x, localCorner.y);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            center = (min + max) * 0.5f;
            size = max - min;
            return true;
        }

        /// <summary>
        /// 显示引导层。
        /// </summary>
        private void ShowGuideView()
        {
            if (_guideRoot != null)
            {
                _guideRoot.SetAsLastSibling();
            }

            RefreshBlockerDisplayState();
            SetGuideVisibility(true);
        }

        /// <summary>
        /// 隐藏引导层。
        /// </summary>
        private void HideGuideView()
        {
            _currentTarget = null;
            _currentClickAction = null;
            _currentTargetKey = string.Empty;
            _debugGuideMessage = string.Empty;
            _debugNextGuideButtonTransform = null;
            if (_tipRect != null)
            {
                _tipRect.gameObject.SetActive(false);
            }

            SetBlockerHoleEnabled(false);
            SetGuideVisibility(false);
        }

        private void OnProxyClick()
        {
            _currentClickAction?.Invoke();

            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            RefreshGuideAfterProxyClick();
        }

        private void OnBlockerClick()
        {
        }

        /// <summary>
        /// 代理点击后立即重刷引导目标，避免切页后需要再等一帧才更新高亮。
        /// </summary>
        private void RefreshGuideAfterProxyClick()
        {
            if (!_isGuideActive)
            {
                return;
            }

            BindViews();
            RefreshGuideByStep();
            RefreshGuideVisual();
        }

        /// <summary>
        /// 判断当前步骤是否需要显示全屏 Blocker。
        /// </summary>
        private bool ShouldShowBlockerForCurrentStep()
        {
            return _currentStep == GuideStep.RoleUpgrade || _currentStep == GuideStep.SkillUpgrade;
        }

        /// <summary>
        /// 根据当前步骤切换 Blocker 的显隐，技能升级后的后续步骤只保留高亮与提示。
        /// </summary>
        private void RefreshBlockerDisplayState()
        {
            bool shouldShowBlocker = ShouldShowBlockerForCurrentStep();
            if (_blockerImage != null && _blockerImage.gameObject.activeSelf != shouldShowBlocker)
            {
                _blockerImage.gameObject.SetActive(shouldShowBlocker);
            }

            if (!shouldShowBlocker)
            {
                SetBlockerHoleEnabled(false);
            }
        }

        /// <summary>
        /// 缓存 HomeGuideRoot 上预先配置好的遮罩材质。
        /// </summary>
        private void CacheBlockerMaterial()
        {
            if (_blockerImage == null)
            {
                return;
            }

            _blockerMaterial = _blockerImage.material;
            if (_blockerMaterial == null)
            {
                Debug.LogError("新手引导 Blocker 缺少遮罩材质，请在 HomeGuideRoot.prefab 中预先配置。");
                return;
            }

            if (_blockerMaterial.shader == null || _blockerMaterial.shader.name != "UI/Guide Hole Mask")
            {
                Debug.LogError("新手引导 Blocker 材质未使用 UI/Guide Hole Mask shader，请检查 HomeGuideMask.mat。");
                _blockerMaterial = null;
                return;
            }

            SetBlockerHoleEnabled(false);
        }

        /// <summary>
        /// 刷新遮罩 shader 的镂空位置与尺寸。
        /// </summary>
        private void RefreshBlockerHole(Vector2 center, Vector2 size)
        {
            if (_blockerMaterial == null)
            {
                return;
            }

            _blockerMaterial.SetVector(HoleCenterShaderId, new Vector4(center.x, center.y, 0f, 0f));
            _blockerMaterial.SetVector(HoleSizeShaderId, new Vector4(size.x, size.y, 0f, 0f));
            _blockerMaterial.SetFloat(HoleRadiusShaderId, 18f);
            _blockerMaterial.SetFloat(EdgeSoftnessShaderId, 8f);
            SetBlockerHoleEnabled(true);
        }

        /// <summary>
        /// 控制遮罩 shader 是否启用镂空区域。
        /// </summary>
        private void SetBlockerHoleEnabled(bool enabled)
        {
            if (_blockerMaterial == null)
            {
                return;
            }

            _blockerMaterial.SetFloat(HoleEnabledShaderId, enabled ? 1f : 0f);
            _blockerImage?.SetMaterialDirty();
        }

        /// <summary>
        /// 从 HomeGuideRoot 中查找指定节点。
        /// </summary>
        private RectTransform FindGuideRect(string path)
        {
            Transform target = _guideRoot != null ? _guideRoot.Find(path) : null;
            if (target == null)
            {
                Debug.LogError($"新手引导 HomeGuideRoot 缺少节点: {path}");
                return null;
            }

            return target as RectTransform;
        }

        /// <summary>
        /// 从 HomeGuideRoot 的指定节点上获取组件。
        /// </summary>
        private T FindGuideComponent<T>(string path) where T : Component
        {
            RectTransform rectTransform = FindGuideRect(path);
            if (rectTransform == null)
            {
                return null;
            }

            T component = rectTransform.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"新手引导 HomeGuideRoot 节点缺少组件 {typeof(T).Name}: {path}");
            }

            return component;
        }

        /// <summary>
        /// 尝试解析当前 HomeGuideRoot 对应的 HomeHud 引用。
        /// </summary>
        private void ResolveHomeHud()
        {
            if (_homeHud != null)
            {
                return;
            }

            _homeHud = HomeHud.Inst;
            if (_homeHud == null && _rootCanvas != null)
            {
                _homeHud = _rootCanvas.GetComponentInChildren<HomeHud>(true);
            }

            if (_rootCanvas == null && _homeHud != null)
            {
                _rootCanvas = _homeHud.Canvas;
            }
        }

        /// <summary>
        /// 通过 CanvasGroup 控制引导层显隐，避免控制器随根节点失活后停止运行。
        /// </summary>
        private void SetGuideVisibility(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private static void StretchFullScreen(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 重置运行时中的引导状态，并在 Home 内立即准备重新进入引导。
        /// </summary>
        private void ResetRuntimeGuideState()
        {
            EnsureGuideView();
            HideGuideView();
            BindViews();
            _currentStep = GuideStep.None;
            _roleLevelSnapshot = 0;
            _skillLevelSnapshot = 0;
            _highlightPulseTime = 0f;
            _isGuideActive = false;
            _isReady = true;
            _forceStartFromRoleUpgrade = false;
            RefreshGuideDebugInfo();
        }

        /// <summary>
        /// 刷新 Inspector 上显示的引导调试信息。
        /// </summary>
        private void RefreshGuideDebugInfo()
        {
            _debugCurrentStep = _currentStep;
            _debugIsWaitingReturnHomeAfterSkillUpgrade = IsWaitingReturnHomeAfterSkillUpgrade();
            _debugCurrentTargetKey = _currentTargetKey;
            _debugNextGuideButtonTransform = _currentTarget;

            if (_tipText != null && !string.IsNullOrEmpty(_tipText.text))
            {
                _debugGuideMessage = _tipText.text;
            }
        }
    }
}
