using System;
using System.Collections.Generic;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit.Runtime.Demo.Item;

namespace XiaoCao.UI
{
    /// <summary>
    /// 主城新手引导: 按顺序引导玩家学会撞击(技能2)、凌空连击(技能1)。
    /// 引导1(技能2 撞击): 遮罩 + 高亮 + 点击代理逐步教学, 升级后自动收起详情页并引导点返回,
    /// 玩家关闭角色界面后用 PopToast 提示学习完成并立即结束引导(无需再点按钮);
    /// 引导2(技能1 凌空连击): 只用发光提示, 不强制点击, 玩家点开该技能详情即结束;
    /// 引导2需先通关第2关且已完成引导1。
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeGuideController : MonoBehaviour
    {
        public const string GuidePrefabPath = "Assets/_Res/UI/HomeUI/HomeGuideRoot.prefab";

        //引导1: 第1个上手技能(技能id=2, 撞击)
        public const string Guide1SkillId = "2";

        //引导2: 第2个技能(技能id=1, 凌空连击)
        public const string Guide2SkillId = "3";

        //引导2触发条件: 通关第2关(章节0, 序号2)
        private const int Guide2Chapter = 0;
        private const int Guide2LevelIndex = 2;

        //技能子面板在 PlayerTabPanel.groups 里的名字
        public const string SkillSubPanelName = "Skill";

        public const int StepOpenPanel = 0;
        public const int StepSkillTab = 1;
        public const int StepSkillCell = 2;
        public const int StepUpgrade = 3;
        //升级完成后: 已学习成功, 等玩家关闭技能详情页, 引导点返回按钮
        public const int StepBack = 4;
        //学习完成: 只弹 PopToast 提示, 当帧即结束引导, 不再等玩家点击
        public const int StepFinish = 5;
        public const int StepDone = 99;

        private const float HolePadding = 12f;
        private const float TipGap = 16f;
        //返回按钮在窗口右上角, 气泡用右上角锚点贴在按钮下方
        private const float TipGapBack = 48f;
        //完成提示(PopToast)的停留时长
        private const float FinishToastTime = 2.5f;

        private static readonly int HoleEnabledId = Shader.PropertyToID("_HoleEnabled");
        private static readonly int HoleCenterId = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleSizeId = Shader.PropertyToID("_HoleSize");
        private static readonly int HoleRadiusId = Shader.PropertyToID("_HoleRadius");
        private const float HoleRadius = 18f;

        private static HomeGuideController _inst;

        //调试工具用: 本次是否通过"重置新手教程"重置过(再点一次走完成)
        private static bool debugResetFlag;

        /// <summary>引导遮罩流程中: 需要屏蔽方向键切换页签</summary>
        public static bool IsBlockingInput => _inst != null && _inst.overlayVisible;

        //当前跑的是引导2(发光提示流程, 不用遮罩)
        private bool IsGuide2Active => _targetSkillId == Guide2SkillId;

        [Header("引导节点(留空按名称自动查找)")]
        public RectTransform highlight;

        [Header("引导1完成后的导航路线(Assets/_Res/UI/HomeUI/GuideNavRoute.prefab)")]
        public GuideNavRoute navRoutePrefab;

        public Button proxyButton;

        public Image blocker;

        public RectTransform tip;

        public TMP_Text tipText;

        private CanvasGroup _canvasGroup;
        private RectTransform _root;
        private Material _blockerMat;
        private GuideData _guide;

        //当前正在跑的那条引导(引导1/引导2共用同一套流程)
        private GuideTask _task;
        private string _targetSkillId = Guide1SkillId;

        //引导2的发光提示节点(可挂到页签/技能格上)
        private GameObject _hintGlow;
        private RectTransform _hintTarget;

        private UIOpenBtn _openBtn;
        private ActiveSkillView _skillView;
        private GameObject _skillViewGo;

        private int _step = StepOpenPanel;
        private bool overlayVisible;
        private Action proxyAction;
        private float missingTimer;
        private float findBtnTimer = 999f;
        private bool refreshTried;

        private bool navSaved;
        private bool prevNavigationEvents;
        private string lastTip;

        #region 生命周期

        private void Awake()
        {
            _inst = this;
            _root = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (highlight == null)
            {
                highlight = _root.Find("Highlight") as RectTransform;
            }
            if (blocker == null)
            {
                Transform blockerTf = _root.Find("Blocker");
                blocker = blockerTf != null ? blockerTf.GetComponent<Image>() : null;
            }
            if (tip == null)
            {
                tip = _root.Find("Tip") as RectTransform;
            }
            if (tipText == null)
            {
                Transform tipTf = _root.Find("Tip/TipText");
                tipText = tipTf != null ? tipTf.GetComponent<TMP_Text>() : null;
            }
            if (proxyButton == null)
            {
                Transform proxyTf = _root.Find("ProxyButton");
                proxyButton = proxyTf != null ? proxyTf.GetComponent<Button>() : null;
            }

            if (proxyButton != null)
            {
                proxyButton.onClick.AddListener(OnProxyClick);
            }

            //遮罩材质是共享资源, 复制一份再改参数, 避免影响其他界面
            if (blocker != null && blocker.material != null)
            {
                _blockerMat = new Material(blocker.material);
                blocker.material = _blockerMat;
            }

            SetGuideVisible(false);
        }

        private void Start()
        {
            Prepare();
        }

        private void OnDestroy()
        {
            ClearHintGlow();
            SetNavigation(true);
            if (_inst == this)
            {
                _inst = null;
            }
        }

        private void Update()
        {
            if (_guide == null || _step == StepDone)
            {
                return;
            }
            if (GameAllData.playerSaveData == null)
            {
                return;
            }

            //玩家用别的方式学会了技能: 直接结束引导
            if (_step < StepUpgrade && PlayerHelper.GetSkillLevel(_targetSkillId) > 0)
            {
                FinishGuide();
                return;
            }

            switch (_step)
            {
                case StepOpenPanel:
                    if (IsGuide2Active) TickHintOpenPanel();
                    else TickOpenPanel();
                    break;
                case StepSkillTab:
                    if (IsGuide2Active) TickHintSkillTab();
                    else TickSkillTab();
                    break;
                case StepSkillCell:
                    if (IsGuide2Active) TickHintSkillCell();
                    else TickSkillCell();
                    break;
                case StepUpgrade:
                    TickUpgrade();
                    break;
                case StepBack:
                    TickBack();
                    break;
                case StepFinish:
                    TickFinish();
                    break;
            }
        }

        #endregion

        #region 引导链

        /// <summary>
        /// 一条引导的进度读写入口: 同一套流程复用到多条引导
        /// </summary>
        private class GuideTask
        {
            public string skillId;
            public Func<int> getStep;
            public Action<int> setStep;
            public Func<bool> getDone;
            public Action<bool> setDone;
        }

        /// <summary>
        /// 引导顺序: 先技能2(撞击), 再技能1(凌空连击)
        /// </summary>
        private static List<GuideTask> BuildTasks(GuideData guide)
        {
            return new List<GuideTask>
            {
                new GuideTask
                {
                    skillId = Guide1SkillId,
                    getStep = () => guide.homeSkillStep,
                    setStep = v => guide.homeSkillStep = v,
                    getDone = () => guide.homeSkillDone,
                    setDone = v => guide.homeSkillDone = v,
                },
                new GuideTask
                {
                    skillId = Guide2SkillId,
                    getStep = () => guide.homeSkill2Step,
                    setStep = v => guide.homeSkill2Step = v,
                    getDone = () => guide.homeSkill2Done,
                    setDone = v => guide.homeSkill2Done = v,
                },
            };
        }

        /// <summary>
        /// 引导是否已满足触发条件
        /// </summary>
        private static bool IsTaskUnlocked(GuideTask task)
        {
            if (task.skillId == Guide2SkillId)
            {
                return IsGuide2Unlocked();
            }
            return true;
        }

        /// <summary>
        /// 引导2: 通关第2关后才提示学习技能1(凌空连击)
        /// </summary>
        private static bool IsGuide2Unlocked()
        {
            PlayerSaveData data = GameAllData.playerSaveData;
            if (data == null || data.levelPassData == null)
            {
                return false;
            }

            return data.levelPassData.GetPassState(Guide2Chapter, Guide2LevelIndex) == LevelPassState.Pass;
        }

        #endregion

        #region 创建

        /// <summary>
        /// 资源包是否已初始化完成(YooAsset 的 manifest 激活后才能安全调用 CheckLocationValid)
        /// </summary>
        public static bool IsResReady()
        {
            return ResMgr.Loader != null && ResMgr.IsLoadBaseFinish;
        }

        /// <summary>
        /// 主城加载后调用: 挂载引导根节点(已经存在时复用)。返回是否已就绪
        /// </summary>
        public static bool EnsureCreated()
        {
            if (_inst != null)
            {
                return true;
            }

            HomeGuideController exist = FindFirstObjectByType<HomeGuideController>(FindObjectsInactive.Include);
            if (exist != null)
            {
                _inst = exist;
                return true;
            }

            if (UIMgr.Inst == null || UIMgr.Inst.topCanvas == null)
            {
                Debuger.LogError("[Guide] UIMgr 未就绪, 引导未创建");
                return false;
            }

            //资源包还在初始化: 交给调用方等资源就绪后重试
            if (!IsResReady())
            {
                return false;
            }

            GameObject prefab = ResMgr.LoadAseet<GameObject>(GuidePrefabPath);
            if (prefab == null)
            {
                Debuger.LogError($"[Guide] 加载引导预制体失败 {GuidePrefabPath}");
                return false;
            }

            GameObject go = Instantiate(prefab, UIMgr.Inst.topCanvas.transform);
            go.name = "HomeGuideRoot";
            //保证盖在所有面板之上
            go.transform.SetAsLastSibling();
            Debug.Log("[Guide] created");
            return true;
        }

        /// <summary>
        /// 读取存档决定从哪一步开始
        /// </summary>
        public void Prepare()
        {
            PlayerSaveData data = GameAllData.playerSaveData;
            if (data == null)
            {
                enabled = false;
                return;
            }
            if (data.guideData == null)
            {
                data.guideData = new GuideData();
            }
            _guide = data.guideData;

            //按顺序找第一条未完成的引导; 技能已学会的引导直接标记完成并跳过
            List<GuideTask> tasks = BuildTasks(_guide);
            GuideTask next = null;
            for (int i = 0; i < tasks.Count; i++)
            {
                GuideTask task = tasks[i];
                if (task.getDone())
                {
                    continue;
                }

                if (PlayerHelper.GetSkillLevel(task.skillId) > 0)
                {
                    task.setDone(true);
                    task.setStep(StepDone);
                    PlayerSaveData.SavaData();
                    continue;
                }

                //后面的引导还没满足触发条件: 本次不触发
                if (!IsTaskUnlocked(task))
                {
                    break;
                }

                next = task;
                break;
            }

            //全部完成: 不再打扰
            if (next == null)
            {
                SetGuideVisible(false);
                gameObject.SetActive(false);
                return;
            }

            _task = next;
            _targetSkillId = next.skillId;
            _step = Mathf.Clamp(next.getStep(), StepOpenPanel, StepFinish);
            missingTimer = 0f;
            refreshTried = false;
            Debug.Log($"[Guide] prepare skill={_targetSkillId} step={_step}");
        }

        #endregion

        #region step 实现

        private void TickOpenPanel()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null)
            {
                return;
            }

            //玩家打开过角色界面: 关掉发光, 正式进入引导
            if (panel.IsShowing)
            {
                HideGlow();
                GoToStep(StepSkillTab);
                return;
            }

            //玩家在浏览其他界面时先收起气泡, 只保留按钮发光
            if (UIMgr.Inst.showingPanels != null && UIMgr.Inst.showingPanels.Count > 0)
            {
                SetGuideVisible(false);
                return;
            }

            ShowOpenPanelTip(LocalizeKey.GuideOpenPlayerPanel.ToLocalizeStr());
        }

        private void TickSkillTab()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null || !panel.IsShowing)
            {
                //中途退出界面: 收起遮罩, 等玩家重新打开
                HideOverlay();
                return;
            }

            Button tabBtn = GetSkillTabButton(panel);
            if (tabBtn == null)
            {
                missingTimer += Time.unscaledDeltaTime;
                if (missingTimer > 2f)
                {
                    missingTimer = 0f;
                    GoToStep(StepSkillCell);
                }
                return;
            }
            missingTimer = 0f;

            ShowOverlay();
            FocusTarget(tabBtn.transform as RectTransform, LocalizeKey.GuideClickSkillTab.ToLocalizeStr());
            proxyAction = () => tabBtn.onClick.Invoke();

            if (IsSkillTabActive(panel))
            {
                GoToStep(StepSkillCell);
            }
        }

        private void TickSkillCell()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null || !panel.IsShowing)
            {
                HideOverlay();
                return;
            }
            if (!IsSkillTabActive(panel))
            {
                GoToStep(StepSkillTab);
                return;
            }

            ActiveSkillView view = GetActiveSkillView(panel);
            if (view == null)
            {
                HideOverlay();
                return;
            }

            SkillItemCell cell = view.GetSkillCell(_targetSkillId);
            if (cell == null && !refreshTried)
            {
                //首次进来技能列表可能还没构建
                refreshTried = true;
                view.RefreshGuideUI();
                cell = view.GetSkillCell(_targetSkillId);
            }
            if (cell == null)
            {
                HideOverlay();
                return;
            }

            ShowOverlay();
            FocusTarget(cell.transform as RectTransform, GetSkillTipText());
            proxyAction = () => cell.clickAct?.Invoke();

            if (IsSkillDetailShown(view))
            {
                GoToStep(StepUpgrade);
            }
        }

        private void TickUpgrade()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null || !panel.IsShowing)
            {
                HideOverlay();
                return;
            }

            ActiveSkillView view = GetActiveSkillView(panel);
            SkillDetailUI detail = view != null ? view.GetSkillDetailUI() : null;
            if (detail == null || !detail.gameObject.activeInHierarchy || detail.skillId != _targetSkillId)
            {
                //详情被关掉: 退回上一步重新选技能
                GoToStep(StepSkillCell);
                return;
            }

            ShowOverlay();
            FocusTarget(detail.upgradeBtn.transform as RectTransform, LocalizeKey.GuideClickUpgrade.ToLocalizeStr());
            proxyAction = OnUpgradeClick;

            if (PlayerHelper.GetSkillLevel(_targetSkillId) > 0)
            {
                //升级成功: 自动收起技能详情页, 接着引导玩家点返回按钮
                HideSkillDetail(detail);
                GoToStep(StepBack);
            }
        }

        /// <summary>
        /// step4: 技能已学会, 引导点击面板返回按钮关闭角色界面
        /// </summary>
        private void TickBack()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null)
            {
                return;
            }

            //玩家已经关闭界面(返回按钮/ Esc 都可): 进入完成提示
            if (!panel.IsShowing)
            {
                GoToStep(StepFinish);
                return;
            }

            Button backBtn = panel.closeBtn;
            if (backBtn == null || !backBtn.gameObject.activeInHierarchy)
            {
                return;
            }

            ShowOverlay();
            FocusTarget(backBtn.transform as RectTransform, LocalizeKey.GuideClickBack.ToLocalizeStr(), TipGapBack, true);
            proxyAction = () => backBtn.onClick.Invoke();
        }

        /// <summary>
        /// 收起技能详情页(升级完成后自动关闭, 让玩家回到技能列表)
        /// </summary>
        private static void HideSkillDetail(SkillDetailUI detail)
        {
            if (detail != null && detail.gameObject.activeSelf)
            {
                detail.OnHide();
            }
        }

        /// <summary>
        /// step5: 学习完成提示改用 PopToast 弹出, 玩家不需要再点一次按钮, 弹出后立即结束引导
        /// </summary>
        private void TickFinish()
        {
            UIMgr.PopToastKey(LocalizeKey.GuideSkillLearned, FinishToastTime);
            FinishGuide();
        }

        #region 引导2: 发光提示

        /// <summary>
        /// 引导2 step0: 主城入口按钮发光 + 气泡, 不拦截点击
        /// </summary>
        private void TickHintOpenPanel()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null)
            {
                return;
            }

            if (panel.IsShowing)
            {
                HideGlow();
                ClearHintGlow();
                GoToStep(StepSkillTab);
                return;
            }

            if (UIMgr.Inst.showingPanels != null && UIMgr.Inst.showingPanels.Count > 0)
            {
                SetGuideVisible(false);
                ClearHintGlow();
                return;
            }

            ShowOpenPanelTip(LocalizeKey.GuideLearnSkillOpen.ToLocalizeStr());
        }

        /// <summary>
        /// 引导2 step1: 「技能」页签发光, 不强制点击
        /// </summary>
        private void TickHintSkillTab()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null)
            {
                return;
            }

            if (!panel.IsShowing)
            {
                //界面关了: 回到第一步继续提示
                ClearHintGlow();
                GoToStep(StepOpenPanel);
                return;
            }

            Button tabBtn = GetSkillTabButton(panel);
            if (tabBtn == null)
            {
                return;
            }

            ShowHintOnly();
            //这一步只要发光, 不显示气泡
            if (tip != null && tip.gameObject.activeSelf)
            {
                tip.gameObject.SetActive(false);
            }
            ShowHintGlow(tabBtn.transform as RectTransform);

            if (IsSkillTabActive(panel))
            {
                GoToStep(StepSkillCell);
            }
        }

        /// <summary>
        /// 引导2 step2: 凌空连击技能格发光, 点开详情即完成引导
        /// </summary>
        private void TickHintSkillCell()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            if (panel == null)
            {
                return;
            }

            if (!panel.IsShowing)
            {
                ClearHintGlow();
                GoToStep(StepOpenPanel);
                return;
            }

            if (!IsSkillTabActive(panel))
            {
                GoToStep(StepSkillTab);
                return;
            }

            ActiveSkillView view = GetActiveSkillView(panel);
            if (view == null)
            {
                return;
            }

            //点开技能详情: 引导完成
            if (IsSkillDetailShown(view))
            {
                FinishGuide();
                return;
            }

            SkillItemCell cell = view.GetSkillCell(_targetSkillId);
            if (cell == null && !refreshTried)
            {
                //首次进来技能列表可能还没构建
                refreshTried = true;
                view.RefreshGuideUI();
                cell = view.GetSkillCell(_targetSkillId);
            }
            if (cell == null)
            {
                ClearHintGlow();
                return;
            }

            ShowHintOnly();
            ShowHintGlow(cell.transform as RectTransform);
            ShowHintTip(cell.transform as RectTransform, LocalizeKey.GuideLearnSkillClick.ToLocalizeStr());
        }

        /// <summary>
        /// 只显示气泡/发光: 不显示遮罩, 也不拦截点击
        /// </summary>
        private void ShowHintOnly()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            overlayVisible = false;
            SetNavigation(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (blocker != null && blocker.gameObject.activeSelf)
            {
                blocker.gameObject.SetActive(false);
            }
            if (proxyButton != null && proxyButton.gameObject.activeSelf)
            {
                proxyButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 给指定目标挂发光: 同一目标复用同一个节点, 并跟随布局刷新
        /// </summary>
        private void ShowHintGlow(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            if (_hintGlow != null && _hintTarget == target)
            {
                GuideGlowHelper.ApplyRect(target, _hintGlow);
                if (!_hintGlow.activeSelf)
                {
                    _hintGlow.SetActive(true);
                }
                return;
            }

            ClearHintGlow();
            _hintTarget = target;
            _hintGlow = GuideGlowHelper.Create(target);
        }

        private void ClearHintGlow()
        {
            GuideGlowHelper.Destroy(_hintGlow);
            _hintGlow = null;
            _hintTarget = null;
        }

        /// <summary>
        /// 在目标下方显示气泡(无遮罩流程用)
        /// </summary>
        private void ShowHintTip(RectTransform target, string tipStr)
        {
            if (target == null || tip == null)
            {
                return;
            }

            tip.gameObject.SetActive(true);
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            PlaceTipBelow(corners, TipGap);
            SetTip(tipStr);
        }

        #endregion

        /// <summary>
        /// 点击代理: 只能触发当前高亮目标的真实按钮
        /// </summary>
        private void OnProxyClick()
        {
            Action action = proxyAction;
            if (action == null)
            {
                return;
            }
            Debug.Log($"[Guide] proxy click step={_step}");
            action.Invoke();
        }

        /// <summary>
        /// 点击升级: 金币不足时补足差额, 保证引导不会卡死
        /// </summary>
        private void OnUpgradeClick()
        {
            PlayerTabPanel panel = UIMgr.Inst != null ? UIMgr.Inst.playerPanel : null;
            ActiveSkillView view = GetActiveSkillView(panel);
            SkillDetailUI detail = view != null ? view.GetSkillDetailUI() : null;
            if (detail == null || detail.skillId != _targetSkillId)
            {
                return;
            }

            EnsureUpgradeCoin();
            detail.upgradeBtn.onClick.Invoke();
        }

        private void EnsureUpgradeCoin()
        {
            PlayerSaveData data = GameAllData.playerSaveData;
            if (data == null)
            {
                return;
            }
            var setting = LubanTables.GetSkillUpgradeSetting(_targetSkillId);
            if (setting == null || setting.Cost == null || setting.Cost.Count == 0)
            {
                return;
            }

            int need = setting.Cost[0];
            int have = data.Coin;
            if (have >= need)
            {
                return;
            }

            int add = need - have;
            data.RewardCoin(add, false);
            PlayerSaveData.SavaData();
            Debug.Log($"[Guide] grant coin +{add}");
        }

        /// <summary>
        /// 进入指定步骤并落存档
        /// </summary>
        private void GoToStep(int step)
        {
            _step = step;
            missingTimer = 0f;
            refreshTried = false;
            lastTip = null;

            if (_task != null)
            {
                _task.setStep(step);
                PlayerSaveData.SavaData();
            }
            Debug.Log($"[Guide] step -> {step}");
        }

        #endregion

        #region step0 发光提示

        private void ShowOpenPanelTip(string tipText)
        {
            if (_openBtn == null)
            {
                //按钮可能还没被 UIMgr 初始化, 控制查找频率
                findBtnTimer += Time.unscaledDeltaTime;
                if (findBtnTimer < 0.5f)
                {
                    return;
                }
                findBtnTimer = 0f;

                _openBtn = UIOpenBtn.Find(UIPanelType.PlayerPanel);
                if (_openBtn == null)
                {
                    return;
                }
                _openBtn.SetGlow(true);
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            //不拦截点击, 只显示气泡
            overlayVisible = false;
            SetNavigation(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            if (blocker != null) blocker.gameObject.SetActive(false);
            if (highlight != null) highlight.gameObject.SetActive(false);
            if (proxyButton != null) proxyButton.gameObject.SetActive(false);
            if (tip != null) tip.gameObject.SetActive(true);

            SetTip(tipText);

            RectTransform btnRt = _openBtn.transform as RectTransform;
            if (btnRt != null && tip != null)
            {
                Vector3[] corners = new Vector3[4];
                btnRt.GetWorldCorners(corners);
                PlaceTipBelow(corners, TipGap);
            }
        }

        private void HideGlow()
        {
            if (_openBtn != null)
            {
                _openBtn.SetGlow(false);
            }
            _openBtn = null;
        }

        #endregion

        #region 遮罩/高亮

        private void ShowOverlay()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (!overlayVisible)
            {
                overlayVisible = true;
                SetNavigation(false);
            }

            if (blocker != null) blocker.gameObject.SetActive(true);
            if (proxyButton != null) proxyButton.gameObject.SetActive(true);
            if (tip != null) tip.gameObject.SetActive(true);
        }

        private void HideOverlay()
        {
            overlayVisible = false;
            SetNavigation(true);
            SetGuideVisible(false);
        }

        private void SetGuideVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 高亮目标: 遮罩挖洞 + 高亮框 + 点击代理 + 气泡
        /// </summary>
        private void FocusTarget(RectTransform target, string tipStr, float tipGap = TipGap, bool tipAlignRight = false)
        {
            if (target == null || blocker == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            //先求世界包围盒: 目标带旋转/镜像(如返回按钮)时角点顺序会乱, 直接取 corners[0]/[2] 会算出负数尺寸
            GetWorldBounds(corners, out Vector3 wMin, out Vector3 wMax);
            Vector2 min = blocker.rectTransform.InverseTransformPoint(wMin);
            Vector2 max = blocker.rectTransform.InverseTransformPoint(wMax);

            //尺寸一律取绝对值, 保证高亮框/挖洞始终有效
            Vector2 center = (min + max) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)) + Vector2.one * HolePadding;

            SetHole(center, size, true);

            if (highlight != null)
            {
                highlight.gameObject.SetActive(true);
                highlight.anchoredPosition = center;
                highlight.sizeDelta = size;
            }
            if (proxyButton != null)
            {
                RectTransform rt = proxyButton.transform as RectTransform;
                rt.gameObject.SetActive(true);
                rt.anchoredPosition = center;
                rt.sizeDelta = size;
            }
            if (tip != null)
            {
                tip.gameObject.SetActive(true);
            }

            PlaceTipBelow(corners, tipGap, tipAlignRight);
            SetTip(tipStr);
        }

        /// <summary>
        /// 取四个世界角点的轴对齐包围盒(与角点顺序、旋转、镜像无关)
        /// </summary>
        private static void GetWorldBounds(Vector3[] corners, out Vector3 wMin, out Vector3 wMax)
        {
            wMin = Vector3.Min(Vector3.Min(corners[0], corners[1]), Vector3.Min(corners[2], corners[3]));
            wMax = Vector3.Max(Vector3.Max(corners[0], corners[1]), Vector3.Max(corners[2], corners[3]));
        }

        private void SetHole(Vector2 center, Vector2 size, bool enabled)
        {
            if (_blockerMat == null)
            {
                return;
            }
            _blockerMat.SetFloat(HoleEnabledId, enabled ? 1f : 0f);
            if (!enabled)
            {
                return;
            }
            _blockerMat.SetVector(HoleCenterId, new Vector4(center.x, center.y, 0f, 0f));
            _blockerMat.SetVector(HoleSizeId, new Vector4(size.x, size.y, 0f, 0f));
            _blockerMat.SetFloat(HoleRadiusId, HoleRadius);
        }

        /// <summary>
        /// 气泡放在目标下方。
        /// alignRight=true 时气泡用右上角锚点, 右上角对齐目标右下角(用于屏幕右上角的返回按钮);
        /// 否则居中放在目标下方, 越界则翻到上方并左右夹紧。
        /// </summary>
        private void PlaceTipBelow(Vector3[] corners, float tipGap, bool alignRight = false)
        {
            if (tip == null || _root == null)
            {
                return;
            }

            GetWorldBounds(corners, out Vector3 wMin, out Vector3 wMax);
            Rect rootRect = _root.rect;

            if (alignRight)
            {
                //气泡锚点改成右上角, 与返回按钮同侧; 位置 = 按钮右下角再向下让 tipGap
                Vector2 bottomRight = _root.InverseTransformPoint(new Vector3(wMax.x, wMin.y, wMin.z));

                tip.anchorMin = Vector2.one;
                tip.anchorMax = Vector2.one;
                tip.pivot = Vector2.one;
                tip.anchoredPosition = new Vector2(
                    bottomRight.x - rootRect.xMax,
                    bottomRight.y - rootRect.yMax - tipGap);
                return;
            }

            Vector2 bottom = _root.InverseTransformPoint(new Vector3((wMin.x + wMax.x) * 0.5f, wMin.y, wMin.z));

            //用实际矩形高度, 避免 sizeDelta 与真实显示尺寸不一致时贴到目标上
            float halfH = tip.rect.height * 0.5f;
            float y = bottom.y - halfH - tipGap;
            if (y - halfH < rootRect.yMin + 8f)
            {
                float topY = _root.InverseTransformPoint(new Vector3((wMin.x + wMax.x) * 0.5f, wMax.y, wMax.z)).y;
                y = topY + halfH + tipGap;
            }
            float halfW = rootRect.width * 0.5f - tip.rect.width * 0.5f - 8f;
            float x = Mathf.Clamp(bottom.x, -halfW, halfW);

            SetTipAnchorCenter();
            tip.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// 气泡锚点复位到屏幕中间(默认形态)
        /// </summary>
        private void SetTipAnchorCenter()
        {
            if (tip == null)
            {
                return;
            }

            tip.anchorMin = new Vector2(0.5f, 0.5f);
            tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0.5f, 0.5f);
        }

        private void SetTip(string text)
        {
            if (tipText == null || lastTip == text)
            {
                return;
            }
            lastTip = text;
            tipText.text = text;
        }

        /// <summary>
        /// 引导期间关闭 EventSystem 键盘/手柄导航
        /// </summary>
        private void SetNavigation(bool on)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (!on)
            {
                if (!navSaved)
                {
                    prevNavigationEvents = eventSystem.sendNavigationEvents;
                    navSaved = true;
                }
                eventSystem.sendNavigationEvents = false;
            }
            else if (navSaved)
            {
                eventSystem.sendNavigationEvents = prevNavigationEvents;
                navSaved = false;
            }
        }

        #endregion

        #region 目标查找

        private static Button GetSkillTabButton(PlayerTabPanel panel)
        {
            if (panel == null || panel.groups == null)
            {
                return null;
            }
            TabPanelGroup group = panel.groups.Find(g => g != null && g.panelName == SkillSubPanelName);
            return group != null ? group.tabBtn : null;
        }

        private static bool IsSkillTabActive(PlayerTabPanel panel)
        {
            return panel != null && panel.curPanel != null && panel.curPanel.subPanelName == SkillSubPanelName;
        }

        private ActiveSkillView GetActiveSkillView(PlayerTabPanel panel)
        {
            if (panel == null)
            {
                return null;
            }
            SubPanel sub = panel.GetPanel(SkillSubPanelName);
            if (sub == null || sub.gameObject == null)
            {
                return null;
            }
            if (_skillView == null || _skillViewGo != sub.gameObject)
            {
                _skillViewGo = sub.gameObject;
                _skillView = sub.gameObject.GetComponentInChildren<ActiveSkillView>(true);
            }
            return _skillView;
        }

        private bool IsSkillDetailShown(ActiveSkillView view)
        {
            SkillDetailUI detail = view != null ? view.GetSkillDetailUI() : null;
            return detail != null && detail.gameObject.activeInHierarchy && detail.skillId == _targetSkillId;
        }

        private string GetSkillTipText()
        {
            string name = LocalizeKey.GetSkillNameKey(_targetSkillId).ToLocalizeStr();
            return LocalizeKey.FormatWithArgs(LocalizeKey.GuideClickSkill, name);
        }

        #endregion

        #region 完成与调试

        /// <summary>
        /// 结束引导并写入完成标记
        /// </summary>
        public void FinishGuide()
        {
            //记录刚完成的是哪条引导, 供完成后的后续表现使用
            string finishedSkillId = _task != null ? _task.skillId : null;

            if (_task != null)
            {
                _task.setStep(StepDone);
                _task.setDone(true);
            }
            PlayerSaveData.SavaData();

            HideGlow();
            HideOverlay();
            proxyAction = null;
            _step = StepDone;
            _task = null;
            ClearHintGlow();
            Debug.Log("[Guide] finished");

            //引导1(撞击)完成: 显示从角色脚下指向关卡入口的地面箭头导航, 玩家到达后自动消失
            if (finishedSkillId == Guide1SkillId)
            {
                LevelEnterTrigger enter = FindFirstObjectByType<LevelEnterTrigger>();
                if (enter != null && navRoutePrefab != null)
                {
                    GuideNavRoute.Show(enter.transform, navRoutePrefab);
                }
            }

            //还有下一条引导(引导2): 立刻从它的第一步接着触发
            _guide = GameAllData.playerSaveData != null ? GameAllData.playerSaveData.guideData : null;
            if (_guide != null)
            {
                Prepare();
                if (_task != null)
                {
                    return;
                }
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 调试: 重置进度并在主城重新触发
        /// </summary>
        public void ResetGuideForDebug()
        {
            ResetGuideProgressOnly();
            debugResetFlag = true;
            _guide = GameAllData.playerSaveData != null ? GameAllData.playerSaveData.guideData : null;
            _task = null;
            _targetSkillId = Guide1SkillId;
            _step = StepOpenPanel;
            missingTimer = 0f;
            lastTip = null;
            overlayVisible = false;
            HideGlow();
            ClearHintGlow();
            HideOverlay();
            gameObject.SetActive(true);
            Prepare();
            Debug.Log("[Guide] reset");
        }

        /// <summary>
        /// 调试: 直接标记全部引导完成
        /// </summary>
        public void CompleteGuideForDebug()
        {
            debugResetFlag = false;
            CompleteGuideProgressOnly();
            HideGlow();
            ClearHintGlow();
            HideOverlay();
            proxyAction = null;
            _step = StepDone;
            _task = null;
            gameObject.SetActive(false);
            Debug.Log("[Guide] debug completed");
        }

        /// <summary>
        /// 调试: 是否已经点过一次"重置新手教程", 再点一次则标记完成
        /// </summary>
        public static bool IsGuideResetForDebug()
        {
            return debugResetFlag;
        }

        /// <summary>
        /// 调试: 只重置进度, 不依赖实例
        /// </summary>
        public static bool ResetGuideProgressOnly()
        {
            PlayerSaveData data = GameAllData.playerSaveData;
            if (data == null)
            {
                return false;
            }
            if (data.guideData == null)
            {
                data.guideData = new GuideData();
            }
            List<GuideTask> tasks = BuildTasks(data.guideData);
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].setDone(false);
                tasks[i].setStep(StepOpenPanel);
            }
            PlayerSaveData.SavaData();
            return true;
        }

        /// <summary>
        /// 调试: 只标记完成, 不依赖实例
        /// </summary>
        public static bool CompleteGuideProgressOnly()
        {
            PlayerSaveData data = GameAllData.playerSaveData;
            if (data == null)
            {
                return false;
            }
            if (data.guideData == null)
            {
                data.guideData = new GuideData();
            }
            List<GuideTask> tasks = BuildTasks(data.guideData);
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].setDone(true);
                tasks[i].setStep(StepDone);
            }
            PlayerSaveData.SavaData();
            debugResetFlag = false;
            return true;
        }

        #endregion
    }
}
