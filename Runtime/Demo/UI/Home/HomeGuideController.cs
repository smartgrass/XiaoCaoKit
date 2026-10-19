using System;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCao.UI
{
    /// <summary>
    /// 主城新手引导: 引导玩家学习第二个技能(skillId=2)。
    /// step0 只在主城给入口按钮加发光提示, 不强制点击;
    /// step1~4 用遮罩 + 高亮 + 点击代理引导, 期间屏蔽键盘导航。
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeGuideController : MonoBehaviour
    {
        public const string GuidePrefabPath = "Assets/_Res/UI/HomeUI/HomeGuideRoot.prefab";

        //第二个技能: 技能id=1。新号默认只解锁技能1, 该技能首学费用已调为0
        public const string TargetSkillId = "1";

        //技能子面板在 PlayerTabPanel.groups 里的名字
        public const string SkillSubPanelName = "Skill";

        public const int StepOpenPanel = 0;
        public const int StepSkillTab = 1;
        public const int StepSkillCell = 2;
        public const int StepUpgrade = 3;
        public const int StepFinish = 4;
        public const int StepDone = 99;

        private const float HolePadding = 12f;
        private const float TipGap = 16f;
        private const float FinishDelay = 2f;

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

        [Header("引导节点(留空按名称自动查找)")]
        public RectTransform highlight;

        public Button proxyButton;

        public Image blocker;

        public RectTransform tip;

        public TMP_Text tipText;

        private CanvasGroup _canvasGroup;
        private RectTransform _root;
        private Material _blockerMat;
        private GuideData _guide;

        private UIOpenBtn _openBtn;
        private ActiveSkillView _skillView;
        private GameObject _skillViewGo;

        private int _step = StepOpenPanel;
        private bool overlayVisible;
        private Action proxyAction;
        private float finishTimer;
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
            if (_step < StepUpgrade && PlayerHelper.GetSkillLevel(TargetSkillId) > 0)
            {
                FinishGuide();
                return;
            }

            switch (_step)
            {
                case StepOpenPanel:
                    TickOpenPanel();
                    break;
                case StepSkillTab:
                    TickSkillTab();
                    break;
                case StepSkillCell:
                    TickSkillCell();
                    break;
                case StepUpgrade:
                    TickUpgrade();
                    break;
                case StepFinish:
                    TickFinish();
                    break;
            }
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

            //技能已学会 或 已完成: 不再打扰
            if (_guide.homeSkillDone || PlayerHelper.GetSkillLevel(TargetSkillId) > 0)
            {
                if (!_guide.homeSkillDone)
                {
                    _guide.homeSkillDone = true;
                    _guide.homeSkillStep = StepDone;
                    PlayerSaveData.SavaData();
                }
                SetGuideVisible(false);
                gameObject.SetActive(false);
                return;
            }

            _step = Mathf.Clamp(_guide.homeSkillStep, StepOpenPanel, StepFinish);
            finishTimer = 0f;
            missingTimer = 0f;
            refreshTried = false;
            Debug.Log($"[Guide] prepare step={_step}");
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

            ShowOpenPanelTip();
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

            SkillItemCell cell = view.GetSkillCell(TargetSkillId);
            if (cell == null && !refreshTried)
            {
                //首次进来技能列表可能还没构建
                refreshTried = true;
                view.RefreshGuideUI();
                cell = view.GetSkillCell(TargetSkillId);
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
            if (detail == null || !detail.gameObject.activeInHierarchy || detail.skillId != TargetSkillId)
            {
                //详情被关掉: 退回上一步重新选技能
                GoToStep(StepSkillCell);
                return;
            }

            ShowOverlay();
            FocusTarget(detail.upgradeBtn.transform as RectTransform, LocalizeKey.GuideClickUpgrade.ToLocalizeStr());
            proxyAction = OnUpgradeClick;

            if (PlayerHelper.GetSkillLevel(TargetSkillId) > 0)
            {
                GoToStep(StepFinish);
            }
        }

        private void TickFinish()
        {
            ShowOverlay();
            SetHole(Vector2.zero, Vector2.zero, false);
            if (highlight != null)
            {
                highlight.gameObject.SetActive(false);
            }

            //完成后全屏可点, 点任意位置结束
            if (proxyButton != null)
            {
                RectTransform rt = proxyButton.transform as RectTransform;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = _root.rect.size;
            }
            if (tip != null)
            {
                tip.gameObject.SetActive(true);
                tip.anchoredPosition = Vector2.zero;
            }
            SetTip(LocalizeKey.GuideSkillLearned.ToLocalizeStr());
            proxyAction = FinishGuide;

            finishTimer += Time.unscaledDeltaTime;
            if (finishTimer >= FinishDelay)
            {
                FinishGuide();
            }
        }

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
            if (detail == null || detail.skillId != TargetSkillId)
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
            var setting = LubanTables.GetSkillUpgradeSetting(TargetSkillId);
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
            finishTimer = 0f;
            missingTimer = 0f;
            refreshTried = false;
            lastTip = null;

            if (_guide != null)
            {
                _guide.homeSkillStep = step;
                PlayerSaveData.SavaData();
            }
            Debug.Log($"[Guide] step -> {step}");
        }

        #endregion

        #region step0 发光提示

        private void ShowOpenPanelTip()
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

            SetTip(LocalizeKey.GuideOpenPlayerPanel.ToLocalizeStr());

            RectTransform btnRt = _openBtn.transform as RectTransform;
            if (btnRt != null && tip != null)
            {
                Vector3[] corners = new Vector3[4];
                btnRt.GetWorldCorners(corners);
                PlaceTipBelow(corners);
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
        private void FocusTarget(RectTransform target, string tipStr)
        {
            if (target == null || blocker == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector2 min = blocker.rectTransform.InverseTransformPoint(corners[0]);
            Vector2 max = blocker.rectTransform.InverseTransformPoint(corners[2]);
            Vector2 center = (min + max) * 0.5f;
            Vector2 size = (max - min) + Vector2.one * HolePadding;

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

            PlaceTipBelow(corners);
            SetTip(tipStr);
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
        /// 气泡放在目标下方, 超出屏幕则放到上方, 左右夹紧在安全区内
        /// </summary>
        private void PlaceTipBelow(Vector3[] corners)
        {
            if (tip == null || _root == null)
            {
                return;
            }

            Vector2 bottom = _root.InverseTransformPoint((corners[0] + corners[3]) * 0.5f);
            Vector2 top = _root.InverseTransformPoint((corners[1] + corners[2]) * 0.5f);

            float halfH = tip.sizeDelta.y * 0.5f;
            float y = bottom.y - halfH - TipGap;
            Rect rootRect = _root.rect;
            if (y - halfH < rootRect.yMin + 8f)
            {
                y = top.y + halfH + TipGap;
            }
            float halfW = rootRect.width * 0.5f - tip.sizeDelta.x * 0.5f - 8f;
            float x = Mathf.Clamp(bottom.x, -halfW, halfW);
            tip.anchoredPosition = new Vector2(x, y);
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

        private static bool IsSkillDetailShown(ActiveSkillView view)
        {
            SkillDetailUI detail = view != null ? view.GetSkillDetailUI() : null;
            return detail != null && detail.gameObject.activeInHierarchy && detail.skillId == TargetSkillId;
        }

        private string GetSkillTipText()
        {
            string name = LocalizeKey.GetSkillNameKey(TargetSkillId).ToLocalizeStr();
            return LocalizeKey.FormatWithArgs(LocalizeKey.GuideClickSkill, name);
        }

        #endregion

        #region 完成与调试

        /// <summary>
        /// 结束引导并写入完成标记
        /// </summary>
        public void FinishGuide()
        {
            if (_guide != null)
            {
                _guide.homeSkillStep = StepDone;
                _guide.homeSkillDone = true;
            }
            PlayerSaveData.SavaData();

            HideGlow();
            HideOverlay();
            _step = StepDone;
            Debug.Log("[Guide] finished");
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
            _step = StepOpenPanel;
            finishTimer = 0f;
            missingTimer = 0f;
            lastTip = null;
            overlayVisible = false;
            HideGlow();
            HideOverlay();
            gameObject.SetActive(true);
            Debug.Log("[Guide] reset");
        }

        /// <summary>
        /// 调试: 直接标记完成
        /// </summary>
        public void CompleteGuideForDebug()
        {
            debugResetFlag = false;
            FinishGuide();
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
            data.guideData.homeSkillDone = false;
            data.guideData.homeSkillStep = StepOpenPanel;
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
            data.guideData.homeSkillDone = true;
            data.guideData.homeSkillStep = StepDone;
            PlayerSaveData.SavaData();
            debugResetFlag = false;
            return true;
        }

        #endregion
    }
}
