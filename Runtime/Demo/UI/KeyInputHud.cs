using MFPC;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using XiaoCao.UI;
using Input = UnityEngine.Input;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCao
{
    //pc和移动端共用
    public class KeyInputHud : GameStartMono, IClearCache
    {
        #region References

        public Transform leftDown;
        public Transform rightDown;

        public Joystick joystick;

        public Transform slotParent;
        [ReadOnly] public List<SkillSlot> slots;
        public SkillSlot extraSkillBtn; //特殊道具按钮

        public SkillSlot rollBtn;
        public Button jumpBtn;
        public Button norAtkBtn;
        public TouchField touchField;

        [Header("Layout Config")]
        public KeyInputHudLayoutConfig layoutConfig;
        #endregion

        #region ExtraSkill Fields
        [Header("ExtraSkill")]
        public float extraSkillLongPressDuration = 0.45f;
        public float extraSkillWheelRadius = 100f;
        public Vector2 extraSkillWheelOptionSize = new Vector2(72f, 72f);
        public float extraSkillWheelStartAngle = 90f; //轮盘角度限制
        public float extraSkillWheelAngleRange = 180f;
        #endregion

        #region Cache
        private PlayerAtkTimer _atkTimer;
        public PlayerData0 _playerData;
        private bool _isHoldListenerBound;
        #endregion

        #region ExtraSkill Runtime
        private ButtonLongPressListener _extraSkillLongPressListener;
        private RectTransform _extraSkillWheelRoot;
        private CanvasGroup _extraSkillWheelCanvasGroup;
        private GameObject _extraSkillOptionPrefab;
        private GameObject _extraSkillCancelOptionPrefab;
        private readonly List<ExtraSkillWheelOption> _extraSkillOptions = new List<ExtraSkillWheelOption>();
        private bool _isExtraSkillWheelVisible;
        private int _extraSkillSelectIndex = -1;
        private SlotType _cacheExtraSlotType;
        private string _cacheExtraItemKey = "";
        private int _cacheExtraItemCount = -1;
        private int _cacheExtraItemListCount = -1;
        private bool _isExtraSkillKeyHolding;
        private bool _isExtraSkillKeyLongPressed;
        private float _extraSkillKeyPressTime;
        private int _extraSkillSkipClickFrame = -1;
        private const int ExtraSkillCancelIndex = -1;
        private const string ExtraSkillCancelOptionPrefabPath = "Assets/_Res/UI/SkillCell/ExtraSkillCancelOption.prefab";
        private const string ExtraSkillOptionPrefabPath = "Assets/_Res/UI/SkillCell/ExtraSkillOption.prefab";
        #endregion

        #region Properties
        public PlayerInputData PlayerInput => PlayerData.inputData;

        public PlayerSetting PlayerSetting => PlayerData.playerSetting;

        private bool HasIndependentRollBtn => rollBtn != null && rollBtn != extraSkillBtn;

        public PlayerAtkTimer AtkTimer
        {
            get
            {
                if (_atkTimer == null)
                {
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimer;
                }

                return _atkTimer;
            }
        }

        public PlayerData0 PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = GameDataCommon.LocalPlayer.playerData;
                }

                return _playerData;
            }
        }
        #endregion

        #region Lifecycle

        public override void OnGameStart()
        {
            base.OnGameStart();
            // 收集普通技能槽位，额外道具按钮单独维护，不参与常规技能槽刷新。
            RefreshSkillSlotCache(false);
            if (HasIndependentRollBtn)
            {
                rollBtn.slotType = SlotType.Inputs;
                rollBtn.index = (int)InputKey.LeftShift;
            }

            //普攻/跳跃的触发时机由设置决定(按下或抬起), 两种时机共用同一回调
            BindBtnTrigger(norAtkBtn, OnNorAtkBtnTrigger);
            AddHoldListener(norAtkBtn);
            BindBtnTrigger(jumpBtn, OnJumpBtnTrigger);

            SetupExtraSkillButton();
            RefreshExtraSkillBtnState(true);
            CheckBarImg();

            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.SkillChange, CheckBarImg);
            RefreshInputTypeUI();
        }

        /// <summary>
        /// 给按钮补"按住"事件: Button.onClick 只在抬起时触发, 长按连续普攻/炮台长按连发需要按住状态
        /// </summary>
        private void AddHoldListener(Button btn)
        {
            if (btn == null || _isHoldListenerBound)
            {
                return;
            }

            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<EventTrigger>();
            }

            AddHoldEntry(trigger, EventTriggerType.PointerDown, true);
            AddHoldEntry(trigger, EventTriggerType.PointerUp, false);
            AddHoldEntry(trigger, EventTriggerType.PointerExit, false);
            _isHoldListenerBound = true;
        }

        private void AddHoldEntry(EventTrigger trigger, EventTriggerType eventType, bool hold)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(_ =>
            {
                PlayerInputData data = PlayerInput;
                if (data != null)
                {
                    data.norAckHold = hold;
                }
            });
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// 普攻按钮: 按下触发模式下按下即写入一次普攻输入
        /// </summary>
        private void OnNorAtkBtnTrigger()
        {
            PlayerInput.inputs[(int)InputKey.NorAck] = true;
        }

        /// <summary>
        /// 跳跃按钮: 按下触发模式下按下即写入一次跳跃输入
        /// </summary>
        private void OnJumpBtnTrigger()
        {
            PlayerInput.inputs[(int)InputKey.Space] = true;
        }

        /// <summary>
        /// 给按钮挂上触发时机切换组件, 回调在同一实例上只注册一次(OnGameStart 可能被多次触发)
        /// </summary>
        private static void BindBtnTrigger(Button btn, UnityAction action)
        {
            ButtonDownTrigger trigger = btn.GetComponent<ButtonDownTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<ButtonDownTrigger>();
            }

            trigger.EnsureEventsInitialized();
            trigger.onTrigger.RemoveListener(action);
            trigger.onTrigger.AddListener(action);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.SkillChange, CheckBarImg);
            ClearNorAckHold();
            ClearExtraSkillSkipClick();
            RemoveExtraSkillButtonListener();
        }

        private void Update()
        {
            CleanupExtraSkillSkipClick();
            if (GameAllData.CommonData.gameState != GameState.Running)
            {
                ClearNorAckHold();
                ClearExtraSkillSkipClick();
                ResetExtraSkillKeyState(true);
                return;
            }

            // 每帧同步额外道具按钮状态，并处理 Tab / 长按轮盘输入。
            RefreshExtraSkillBtnState();
            CheckExtraSkillKeyInput();

            if (GameSetting.UserInputType == UserInputType.Touch)
            {
                CheckBtnInput();
            }

            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                //界面接管输入时不保留按住状态, 否则关界面后会继续自动普攻
                ClearNorAckHold();
                return;
            }

            CheckUIUpdate();
        }

        /// <summary>
        /// 清掉普攻按住状态: 暂停/进入界面/离开战斗时兜底, 避免按住标记残留导致自动连续攻击
        /// </summary>
        private void ClearNorAckHold()
        {
            if (GameDataCommon.LocalPlayer == null || GameDataCommon.LocalPlayer.playerData == null)
            {
                return;
            }

            PlayerData0 playerData = GameDataCommon.LocalPlayer.playerData;
            playerData.inputData.norAckHold = false;
            //连击缓存(平a缓存)也一起清掉, 否则重新回到战斗会立刻多打一段
            playerData.norAckCache = false;
        }


        private void FixedUpdate()
        {
            if (GameAllData.CommonData.gameState != GameState.Running)
            {
                return;
            }

            PlayerInputData.LocalSwipeDirection = touchField.GetSwipeDirection;
        }
        #endregion

        #region Common UI
        private void CheckUIUpdate()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                solt.CheckSlotUI(PlayerData.GetBarSkillId(i));
            }

            if (HasIndependentRollBtn)
            {
                rollBtn.CheckSlotUI(PlayerSetting.rollSkillId);
            }
            if (extraSkillBtn.isActiveAndEnabled)
            {
                extraSkillBtn.CheckExtraItemUI();
            }
        }


        private void CheckBtnInput()
        {
            //checkInput
            Vector2 input = joystick.GetInputV;
            PlayerInput.SetXY(input.x, input.y);
        }

        private void CheckBarImg()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                solt.index = i;
                solt.LoadSkillSprite();
            }

            if (extraSkillBtn != null && extraSkillBtn.gameObject.activeSelf)
            {
                extraSkillBtn.LoadSkillSprite();
            }
        }

        public void RefreshInputTypeUI()
        {
            if (GameSetting.UserInputType == UserInputType.Mouse)
            {
                ClearTouchInput();
            }

            ApplySavedLayout(GameSetting.UserInputType);
            RefreshSkillSlotCache(false);

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].RefreshInputTypeUI();
                }
            }

            if (HasIndependentRollBtn)
            {
                rollBtn.RefreshInputTypeUI();
            }
            extraSkillBtn?.RefreshInputTypeUI();
        }

        private void ClearTouchInput()
        {
            var localPlayer = GameDataCommon.LocalPlayer;
            if (localPlayer == null || localPlayer.playerData == null)
            {
                return;
            }

            localPlayer.playerData.inputData.SetXY(0, 0);
            PlayerInputData.LocalSwipeDirection = Vector2.zero;
        }

        private void ApplySavedLayout(UserInputType inputType)
        {
            if (layoutConfig == null)
            {
                return;
            }

            ApplyLayoutData(layoutConfig.GetLayout(inputType), false);
        }

        private void ApplyLayoutData(KeyInputHudLayoutData data, bool includeInactive)
        {
            if (data == null || !data.isSaved)
            {
                return;
            }

            List<SkillSlot> layoutSlots = GetLayoutSlots(includeInactive);

            ApplyLayoutTransform(joystick, data.joystick);
            ApplyLayoutTransform(extraSkillBtn, data.extraSkillBtn);
            ApplyLayoutTransform(rollBtn, data.rollBtn);
            ApplyLayoutTransform(jumpBtn, data.jumpBtn);
            ApplyLayoutTransform(norAtkBtn, data.norAtkBtn);

            if (layoutSlots == null || data.slots == null)
            {
                return;
            }

            int count = Mathf.Min(layoutSlots.Count, data.slots.Count);
            for (int i = 0; i < count; i++)
            {
                ApplyLayoutTransform(layoutSlots[i], data.slots[i]);
            }

            if (!includeInactive)
            {
                RefreshSkillSlotCache(false);
            }
        }

        private List<SkillSlot> GetLayoutSlots(bool includeInactive)
        {
            List<SkillSlot> layoutSlots = includeInactive
                ? transform.GetComponentsInChildren<SkillSlot>(true).ToList()
                : transform.GetComponentsInChildren<SkillSlot>().ToList();
            layoutSlots.Remove(extraSkillBtn);
            if (HasIndependentRollBtn)
            {
                layoutSlots.Remove(rollBtn);
            }

            return layoutSlots
                .OrderBy(GetLayoutSlotSortKey)
                .ThenBy(slot => slot.name)
                .ToList();
        }

        private void RefreshSkillSlotCache(bool includeInactive)
        {
            slots = GetLayoutSlots(includeInactive);
        }

        private static int GetLayoutSlotSortKey(SkillSlot slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.name))
            {
                return int.MaxValue;
            }

            int number = 0;
            bool hasNumber = false;
            for (int i = 0; i < slot.name.Length; i++)
            {
                char c = slot.name[i];
                if (!char.IsDigit(c))
                {
                    continue;
                }

                hasNumber = true;
                number = number * 10 + (c - '0');
            }

            return hasNumber ? number : int.MaxValue;
        }

        private void ApplyLayoutTransform(Component component, KeyInputHudLocalPositionData data)
        {
            if (component == null || data == null)
            {
                return;
            }

            RectTransform rectTransform = component.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            ApplyParentSet(rectTransform, data.parentSet);
            rectTransform.anchoredPosition = data.anchoredPosition;
            Vector3 localScale = rectTransform.localScale;
            localScale.x = data.localSize;
            localScale.y = data.localSize;
            rectTransform.localScale = localScale;
        }

        private void ApplyParentSet(Transform target, ParentSet parentSet)
        {
            if (target == null || parentSet == ParentSet.NoChange)
            {
                return;
            }

            Transform parent = GetParentBySet(parentSet);
            if (parent == null || target.parent == parent)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.SetTransformParent(target, parent, "Load KeyInputHud Layout");
            }
            else
#endif
            {
                target.SetParent(parent, false);
            }
        }

        private Transform GetParentBySet(ParentSet parentSet)
        {
            switch (parentSet)
            {
                case ParentSet.LeftDown:
                    return leftDown;
                case ParentSet.RightDown:
                    return rightDown;
                default:
                    return null;
            }
        }
        #endregion

        #region 炮台操控模式

        //炮台操控期间被隐藏的按钮, 值与隐藏前的激活状态对应
        private readonly Dictionary<GameObject, bool> turretHideCache = new Dictionary<GameObject, bool>();
        private bool isTurretMode;

        /// <summary>
        /// 炮台操控模式: 隐藏技能栏/翻滚/跳跃(空格)/额外道具, 只保留普通攻击键(发射);
        /// 退出炮台改走顶部的 CloseBtn 或 Esc。退出操控时按隐藏前的状态恢复
        /// </summary>
        public void SetTurretControlMode(bool isOn)
        {
            if (isTurretMode == isOn)
            {
                return;
            }

            isTurretMode = isOn;

            if (isOn)
            {
                turretHideCache.Clear();

                //技能栏整块隐藏, PC 模式下槽位上带的按键提示也一起收掉
                HideForTurret(slotParent.gameObject);
                HideForTurret(jumpBtn.gameObject);

                if (HasIndependentRollBtn)
                {
                    HideForTurret(rollBtn.gameObject);
                }

                if (extraSkillBtn != null)
                {
                    HideForTurret(extraSkillBtn.gameObject);
                }

                //布局配置可能把槽位挪到 leftDown 下, 逐个再兜一遍
                if (slots != null)
                {
                    for (int i = 0; i < slots.Count; i++)
                    {
                        HideForTurret(slots[i].gameObject);
                    }
                }

                return;
            }

            foreach (var item in turretHideCache)
            {
                if (item.Key != null)
                {
                    item.Key.SetActive(item.Value);
                }
            }

            turretHideCache.Clear();
            RefreshExtraSkillBtnState(true);
        }

        private void HideForTurret(GameObject go)
        {
            if (go == null || turretHideCache.ContainsKey(go))
            {
                return;
            }

            turretHideCache[go] = go.activeSelf;
            go.SetActive(false);
        }

        #endregion

        #region ExtraSkill
        private void SetupExtraSkillButton()
        {
            if (extraSkillBtn == null)
            {
                return;
            }

            // 给特殊道具按钮挂上长按监听，统一处理长按、拖拽选中和松手确认。
            extraSkillBtn.index = GameSetting.SkillCountOnBar;
            _extraSkillLongPressListener = extraSkillBtn.GetComponent<ButtonLongPressListener>();
            if (_extraSkillLongPressListener == null)
            {
                _extraSkillLongPressListener = extraSkillBtn.gameObject.AddComponent<ButtonLongPressListener>();
            }

            _extraSkillLongPressListener.EnsureEventsInitialized();
            _extraSkillLongPressListener.holdDuration = extraSkillLongPressDuration;
            _extraSkillLongPressListener.onLongPress.RemoveListener(OnExtraSkillLongPress);
            _extraSkillLongPressListener.onLongPress.AddListener(OnExtraSkillLongPress);
            _extraSkillLongPressListener.onDrag.RemoveListener(OnExtraSkillDrag);
            _extraSkillLongPressListener.onDrag.AddListener(OnExtraSkillDrag);
            _extraSkillLongPressListener.onPointerUp.RemoveListener(OnExtraSkillPointerUp);
            _extraSkillLongPressListener.onPointerUp.AddListener(OnExtraSkillPointerUp);
            EnsureExtraSkillWheelRoot();
        }

        private void RemoveExtraSkillButtonListener()
        {
            if (_extraSkillLongPressListener == null)
            {
                return;
            }

            _extraSkillLongPressListener.onLongPress.RemoveListener(OnExtraSkillLongPress);
            _extraSkillLongPressListener.onDrag.RemoveListener(OnExtraSkillDrag);
            _extraSkillLongPressListener.onPointerUp.RemoveListener(OnExtraSkillPointerUp);
        }

        private void RefreshExtraSkillBtnState(bool isForce = false)
        {
            if (extraSkillBtn == null)
            {
                return;
            }

            //炮台操控期间额外道具按钮保持隐藏, 不参与刷新
            if (isTurretMode)
            {
                HideExtraSkillWheel();
                return;
            }

            // 没有额外道具时直接隐藏按钮和轮盘，避免残留选择状态。
            bool hasExtraItem = BattleData.Current.HasExtraItemSkill();
            bool isActive = hasExtraItem;
            if (extraSkillBtn.gameObject.activeSelf != isActive)
            {
                extraSkillBtn.gameObject.SetActive(isActive);
            }

            if (!isActive)
            {
                HideExtraSkillWheel();
                ClearExtraSkillSkipClick();
                CacheExtraSkillState(SlotType.ExtraItem, "", 0, 0);
                return;
            }

            SlotType targetType = SlotType.ExtraItem;
            var selectedItem = BattleData.Current.GetSelectedExtraItem();
            string itemKey = selectedItem != null ? selectedItem.ItemKey : string.Empty;
            int itemCount = selectedItem != null ? selectedItem.count : 0;
            int listCount = BattleData.Current.GetExtraItems().Count;
            // 只有选中道具、数量或列表发生变化时，才刷新按钮显示，减少重复刷新。
            bool isChanged = isForce ||
                             extraSkillBtn.slotType != targetType ||
                             _cacheExtraSlotType != targetType ||
                             _cacheExtraItemKey != itemKey ||
                             _cacheExtraItemCount != itemCount ||
                             _cacheExtraItemListCount != listCount;

            extraSkillBtn.slotType = targetType;
            extraSkillBtn.index = GameSetting.SkillCountOnBar;

            if (isChanged)
            {
                extraSkillBtn.isColdLastFrame = false;
                extraSkillBtn.LoadSkillSprite();
                extraSkillBtn.RefreshInputTypeUI();
                CacheExtraSkillState(targetType, itemKey, itemCount, listCount);
            }

            if (_isExtraSkillWheelVisible && listCount <= 0)
            {
                HideExtraSkillWheel();
            }
        }

        private void CacheExtraSkillState(SlotType slotType, string itemKey, int itemCount, int listCount)
        {
            _cacheExtraSlotType = slotType;
            _cacheExtraItemKey = itemKey;
            _cacheExtraItemCount = itemCount;
            _cacheExtraItemListCount = listCount;
        }

        private void EnsureExtraSkillWheelRoot()
        {
            if (_extraSkillWheelRoot != null)
            {
                return;
            }

            // 轮盘根节点运行时创建一次，后续只复用和清空子节点。
            var wheelRootGo = new GameObject("ExtraSkillWheel", typeof(RectTransform), typeof(CanvasGroup));
            _extraSkillWheelRoot = wheelRootGo.GetComponent<RectTransform>();
            _extraSkillWheelRoot.SetParent(extraSkillBtn.transform.parent, false);
            _extraSkillWheelRoot.localScale = Vector3.one;
            _extraSkillWheelCanvasGroup = wheelRootGo.GetComponent<CanvasGroup>();
            _extraSkillWheelCanvasGroup.alpha = 0;
            _extraSkillWheelCanvasGroup.interactable = false;
            _extraSkillWheelCanvasGroup.blocksRaycasts = false;
        }

        private void OnExtraSkillLongPress()
        {
            if (extraSkillBtn == null || extraSkillBtn.slotType != SlotType.ExtraItem)
            {
                return;
            }

            var extraItems = BattleData.Current.GetExtraItems();
            if (extraItems.Count <= 0)
            {
                return;
            }

            // 长按后打开轮盘，让玩家只切换首选道具，不立即释放。
            ShowExtraSkillWheel(extraItems);
        }

        private void OnExtraSkillDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            UpdateExtraSkillSelection(eventData.position, eventData.pressEventCamera);
        }

        private void OnExtraSkillPointerUp(PointerEventData eventData)
        {
            if (!_isExtraSkillWheelVisible)
            {
                Debug.Log($"[KeyInputHud] ExtraSkill PointerUp ignored, wheelVisible={_isExtraSkillWheelVisible}, frame={Time.frameCount}");
                return;
            }

            int selectedIndex = _extraSkillSelectIndex;
            Debug.Log($"[KeyInputHud] ExtraSkill PointerUp confirm, frame={Time.frameCount}, selectedIndex={selectedIndex}, currentSelected={BattleData.Current.selectedExtraItemIndex}, pointerPos={(eventData != null ? eventData.position.ToString() : "null")}");
            // 长按结束后的这次抬起，不再触发按钮自己的 Click，避免误放技能。
            ArmExtraSkillSkipClick();
            TrySelectExtraSkill(selectedIndex);
            RefreshExtraSkillBtnState(true);
        }

        private void ShowExtraSkillWheel(List<BattleExtraItemData> extraItems)
        {
            EnsureExtraSkillWheelRoot();
            ClearExtraSkillWheel();
            PositionExtraSkillWheel();

            _extraSkillWheelCanvasGroup.alpha = 1;
            _extraSkillWheelCanvasGroup.interactable = true;
            _extraSkillWheelCanvasGroup.blocksRaycasts = false;
            _isExtraSkillWheelVisible = true;

            int optionCount = extraItems.Count + 1;
            // 取消项固定在顶部，其余选项限制在左半圆展开。
            float angleStep = extraSkillWheelAngleRange / optionCount;
            float cancelAngle = extraSkillWheelStartAngle * Mathf.Deg2Rad;
            Vector2 cancelPos = new Vector2(Mathf.Cos(cancelAngle), Mathf.Sin(cancelAngle)) * extraSkillWheelRadius;
            var cancelOption = CreateExtraSkillCancelOption(cancelPos);
            if (cancelOption != null)
            {
                _extraSkillOptions.Add(cancelOption);
            }

            for (int i = 0; i < extraItems.Count; i++)
            {
                float angle = (extraSkillWheelStartAngle + angleStep * (i + 1)) * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * extraSkillWheelRadius;
                var option = CreateExtraSkillOption(i, extraItems[i], pos);
                if (option != null)
                {
                    _extraSkillOptions.Add(option);
                }
            }

            HighlightExtraSkillOption(GetOptionIndexByItemIndex(BattleData.Current.selectedExtraItemIndex));
        }

        private ExtraSkillWheelOption CreateExtraSkillOption(int index, BattleExtraItemData extraItem, Vector2 anchoredPos)
        {
            if (_extraSkillOptionPrefab == null)
            {
                // 普通道具项也改为预制体，方便统一调整轮盘样式。
                _extraSkillOptionPrefab = ResMgr.LoadAseet<GameObject>(ExtraSkillOptionPrefabPath);
            }

            if (_extraSkillOptionPrefab == null)
            {
                Debug.LogError($"Extra skill option prefab not found: {ExtraSkillOptionPrefabPath}");
                return null;
            }

            GameObject optionGo = Instantiate(_extraSkillOptionPrefab, _extraSkillWheelRoot, false);
            var option = optionGo.GetComponent<ExtraSkillWheelOption>();
            if (option == null)
            {
                Debug.LogError($"Extra skill wheel option component missing on prefab: {ExtraSkillOptionPrefabPath}");
                Destroy(optionGo);
                return null;
            }

            option.SetupItem(index, extraItem.ToItem().GetItemSprite(), extraSkillWheelOptionSize, anchoredPos);
            return option;
        }

        private ExtraSkillWheelOption CreateExtraSkillCancelOption(Vector2 anchoredPos)
        {
            if (_extraSkillCancelOptionPrefab == null)
            {
                // 取消项改为预制体，方便直接在编辑器里调整样式。
                _extraSkillCancelOptionPrefab = ResMgr.LoadAseet<GameObject>(ExtraSkillCancelOptionPrefabPath);
            }

            if (_extraSkillCancelOptionPrefab == null)
            {
                Debug.LogError($"Extra skill cancel option prefab not found: {ExtraSkillCancelOptionPrefabPath}");
                return null;
            }

            GameObject optionGo = Instantiate(_extraSkillCancelOptionPrefab, _extraSkillWheelRoot, false);
            var option = optionGo.GetComponent<ExtraSkillWheelOption>();
            if (option == null)
            {
                Debug.LogError($"Extra skill wheel option component missing on prefab: {ExtraSkillCancelOptionPrefabPath}");
                Destroy(optionGo);
                return null;
            }

            option.SetupCancel(ExtraSkillCancelIndex, extraSkillWheelOptionSize, anchoredPos);
            return option;
        }

        private void PositionExtraSkillWheel()
        {
            RectTransform parentRect = _extraSkillWheelRoot.parent as RectTransform;
            RectTransform btnRect = extraSkillBtn.transform as RectTransform;
            if (parentRect == null || btnRect == null)
            {
                return;
            }

            Canvas canvas = extraSkillBtn.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(eventCamera, btnRect.position),
                eventCamera,
                out Vector2 anchoredPos);
            // 轮盘中心始终对齐到额外道具按钮位置，保证选择方向稳定。
            _extraSkillWheelRoot.anchoredPosition = anchoredPos;
        }

        private void HighlightExtraSkillOption(int index)
        {
            if (_extraSkillOptions.Count == 0)
            {
                _extraSkillSelectIndex = -1;
                return;
            }

            int optionIndex = Mathf.Clamp(index, 0, _extraSkillOptions.Count - 1);
            _extraSkillSelectIndex = GetItemIndexByOptionIndex(optionIndex);
            for (int i = 0; i < _extraSkillOptions.Count; i++)
            {
                _extraSkillOptions[i].RefreshVisual(i == optionIndex);
            }
        }

        private int GetOptionIndexByItemIndex(int itemIndex)
        {
            for (int i = 0; i < _extraSkillOptions.Count; i++)
            {
                if (_extraSkillOptions[i].ItemIndex == itemIndex)
                {
                    return i;
                }
            }

            return _extraSkillOptions.Count > 1 ? 1 : 0;
        }

        private int GetItemIndexByOptionIndex(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _extraSkillOptions.Count)
            {
                return ExtraSkillCancelIndex;
            }

            return _extraSkillOptions[optionIndex].ItemIndex;
        }

        private void CheckExtraSkillKeyInput()
        {
            // if (GameSetting.UserInputType == UserInputType.Touch)
            // {
            //     ResetExtraSkillKeyState(true);
            //     return;
            // }

            bool canUseExtraSkill = extraSkillBtn != null &&
                                    extraSkillBtn.isActiveAndEnabled &&
                                    extraSkillBtn.slotType == SlotType.ExtraItem &&
                                    UIMgr.CanReadPlayerInput;
            if (!canUseExtraSkill)
            {
                ResetExtraSkillKeyState(true);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _isExtraSkillKeyHolding = true;
                _isExtraSkillKeyLongPressed = false;
                _extraSkillKeyPressTime = Time.unscaledTime;
                Debug.Log($"[KeyInputHud] ExtraSkill TabDown, frame={Time.frameCount}, wheelVisible={_isExtraSkillWheelVisible}, selectedExtraItemIndex={BattleData.Current.selectedExtraItemIndex}, skipNextClick={(extraSkillBtn != null && extraSkillBtn.skipNextClick)}");
            }

            if (!_isExtraSkillKeyHolding)
            {
                return;
            }

            if (Input.GetKey(KeyCode.Tab))
            {
                if (!_isExtraSkillKeyLongPressed && Time.unscaledTime - _extraSkillKeyPressTime >= extraSkillLongPressDuration)
                {
                    // Tab 长按达到阈值后弹出轮盘。
                    _isExtraSkillKeyLongPressed = true;
                    OnExtraSkillLongPress();
                }

                if (_isExtraSkillWheelVisible)
                {
                    // 键盘长按时，使用当前鼠标方向更新轮盘高亮项。
                    UpdateExtraSkillSelection(Input.mousePosition, GetExtraSkillEventCamera());
                }
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                float holdTime = Time.unscaledTime - _extraSkillKeyPressTime;
                Debug.Log($"[KeyInputHud] ExtraSkill TabUp, frame={Time.frameCount}, holdTime={holdTime:F3}, longPressed={_isExtraSkillKeyLongPressed}, wheelVisible={_isExtraSkillWheelVisible}, selectedIndex={_extraSkillSelectIndex}, currentSelected={BattleData.Current.selectedExtraItemIndex}, skipNextClick={(extraSkillBtn != null && extraSkillBtn.skipNextClick)}");
                if (_isExtraSkillWheelVisible)
                {
                    // 长按松手只切换首选道具。
                    TrySelectExtraSkill(_extraSkillSelectIndex);
                }
                else
                {
                    // 短按则直接释放当前首选道具。
                    TryUseCurrentExtraSkill();
                }

                ResetExtraSkillKeyState(false);
                RefreshExtraSkillBtnState(true);
            }
        }

        private void ResetExtraSkillKeyState(bool hideWheel)
        {
            _isExtraSkillKeyHolding = false;
            _isExtraSkillKeyLongPressed = false;
            _extraSkillKeyPressTime = 0;
            if (hideWheel && _isExtraSkillWheelVisible)
            {
                HideExtraSkillWheel();
            }
        }

        private void ArmExtraSkillSkipClick()
        {
            if (extraSkillBtn == null)
            {
                return;
            }

            // 只屏蔽本次长按抬手附带的 Button.onClick；如果这次没有真正产生 click，下一帧自动清理。
            extraSkillBtn.skipNextClick = true;
            _extraSkillSkipClickFrame = Time.frameCount;
        }

        private void CleanupExtraSkillSkipClick()
        {
            if (extraSkillBtn == null)
            {
                _extraSkillSkipClickFrame = -1;
                return;
            }

            if (!extraSkillBtn.skipNextClick)
            {
                _extraSkillSkipClickFrame = -1;
                return;
            }

            if (_extraSkillSkipClickFrame >= 0 && Time.frameCount > _extraSkillSkipClickFrame)
            {
                extraSkillBtn.skipNextClick = false;
                _extraSkillSkipClickFrame = -1;
            }
        }

        private void ClearExtraSkillSkipClick()
        {
            if (extraSkillBtn != null)
            {
                extraSkillBtn.skipNextClick = false;
            }

            _extraSkillSkipClickFrame = -1;
        }

        private void UpdateExtraSkillSelection(Vector2 screenPos, Camera eventCamera)
        {
            if (!_isExtraSkillWheelVisible || _extraSkillOptions.Count == 0)
            {
                return;
            }

            Camera targetCamera = eventCamera != null ? eventCamera : GetExtraSkillEventCamera();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(targetCamera, _extraSkillWheelRoot.position);
            Vector2 delta = screenPos - center;
            if (delta.sqrMagnitude <= 4f)
            {
                return;
            }

            Vector2 dir = delta.normalized;
            float bestDot = float.MinValue;
            int bestIndex = GetOptionIndexByItemIndex(_extraSkillSelectIndex);
            // 用点积找到与当前指向最接近的扇区，作为轮盘高亮目标。
            for (int i = 0; i < _extraSkillOptions.Count; i++)
            {
                Vector2 optionDir = _extraSkillOptions[i].Direction;
                float dot = Vector2.Dot(dir, optionDir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestIndex = i;
                }
            }

            HighlightExtraSkillOption(bestIndex);
        }

        private Camera GetExtraSkillEventCamera()
        {
            Canvas canvas = extraSkillBtn != null ? extraSkillBtn.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return canvas.worldCamera;
            }

            return null;
        }

        private bool TryUseCurrentExtraSkill()
        {
            // 短按按钮 / Tab 时，直接释放当前首选的特殊道具技能。
            bool isUseSuccess = GameDataCommon.LocalPlayer != null &&
                                GameDataCommon.LocalPlayer.TryUseExtraSkill(BattleData.Current.GetSelectedExtraItem()?.typeId);
            if (isUseSuccess && extraSkillBtn != null && extraSkillBtn.effectTween != null)
            {
                extraSkillBtn.effectTween.Play();
            }

            return isUseSuccess;
        }

        private bool TrySelectExtraSkill(int selectedIndex)
        {
            HideExtraSkillWheel();
            if (selectedIndex == ExtraSkillCancelIndex)
            {
                return false;
            }

            // 轮盘确认后只更新首选索引，真正使用仍然交给后续的短按逻辑。
            BattleData.Current.SelectExtraItem(selectedIndex);
            return true;
        }

        private void HideExtraSkillWheel()
        {
            _isExtraSkillWheelVisible = false;
            _extraSkillSelectIndex = -1;
            if (_extraSkillWheelCanvasGroup != null)
            {
                _extraSkillWheelCanvasGroup.alpha = 0;
                _extraSkillWheelCanvasGroup.interactable = false;
                _extraSkillWheelCanvasGroup.blocksRaycasts = false;
            }

            ClearExtraSkillWheel();
        }

        private void ClearExtraSkillWheel()
        {
            _extraSkillOptions.Clear();
            if (_extraSkillWheelRoot == null)
            {
                return;
            }

            for (int i = _extraSkillWheelRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_extraSkillWheelRoot.GetChild(i).gameObject);
            }
        }
        #endregion

#if UNITY_EDITOR
        private const string DefaultLayoutConfigAssetPath = "Assets/_Res/UI/Setting/KeyInputHudLayoutConfig.asset";

        [Button("保存PC布局", 200, EButtonEnableMode.Editor)]
        private void SavePcLayout()
        {
            SaveLayout(UserInputType.Mouse);
        }

        [Button("读取PC布局", 200, EButtonEnableMode.Editor)]
        private void LoadPcLayout()
        {
            LoadLayout(UserInputType.Mouse);
        }

        [Button("保存移动端布局", 201, EButtonEnableMode.Editor)]
        private void SaveTouchLayout()
        {
            SaveLayout(UserInputType.Touch);
        }

        [Button("读取移动端布局", 201, EButtonEnableMode.Editor)]
        private void LoadTouchLayout()
        {
            LoadLayout(UserInputType.Touch);
        }

        private void SaveLayout(UserInputType inputType)
        {
            if (!EditorUtility.DisplayDialog("确认保存布局",
                    $"是否保存{GetInputTypeLabel(inputType)}布局到\n{DefaultLayoutConfigAssetPath}？",
                    "保存", "取消"))
            {
                return;
            }

            var config = EnsureLayoutConfig();
            if (config == null)
            {
                return;
            }

            List<SkillSlot> layoutSlots = GetLayoutSlots(true);

            KeyInputHudLayoutData data = config.GetLayout(inputType);
            data.isSaved = true;
            CaptureLayoutTransform(data.joystick, joystick);
            CaptureLayoutTransform(data.extraSkillBtn, extraSkillBtn);
            CaptureLayoutTransform(data.rollBtn, rollBtn);
            CaptureLayoutTransform(data.jumpBtn, jumpBtn);
            CaptureLayoutTransform(data.norAtkBtn, norAtkBtn);

            data.slots.Clear();
            if (layoutSlots != null)
            {
                for (int i = 0; i < layoutSlots.Count; i++)
                {
                    if (layoutSlots[i] == null)
                    {
                        continue;
                    }

                    var slotData = new KeyInputHudLocalPositionData();
                    CaptureLayoutTransform(slotData, layoutSlots[i]);
                    data.slots.Add(slotData);
                }
            }

            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string GetInputTypeLabel(UserInputType inputType)
        {
            return inputType == UserInputType.Touch ? "移动端" : "PC";
        }

        private void LoadLayout(UserInputType inputType)
        {
            if (layoutConfig == null)
            {
                layoutConfig = AssetDatabase.LoadAssetAtPath<KeyInputHudLayoutConfig>(DefaultLayoutConfigAssetPath);
            }

            if (layoutConfig == null)
            {
                Debug.LogWarning($"[KeyInputHud] 未找到布局配置: {DefaultLayoutConfigAssetPath}", this);
                return;
            }

            Undo.RecordObject(transform, $"Load KeyInputHud {inputType} Layout");
            ApplyLayoutData(layoutConfig.GetLayout(inputType), true);
            MarkLayoutTargetsDirty();
        }

        private KeyInputHudLayoutConfig EnsureLayoutConfig()
        {
            if (layoutConfig != null)
            {
                return layoutConfig;
            }

            layoutConfig = AssetDatabase.LoadAssetAtPath<KeyInputHudLayoutConfig>(DefaultLayoutConfigAssetPath);
            if (layoutConfig != null)
            {
                return layoutConfig;
            }

            FileTool.CheckFilePathDir(DefaultLayoutConfigAssetPath);
            layoutConfig = ScriptableObject.CreateInstance<KeyInputHudLayoutConfig>();
            AssetDatabase.CreateAsset(layoutConfig, DefaultLayoutConfigAssetPath);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return layoutConfig;
        }

        private void CaptureLayoutTransform(KeyInputHudLocalPositionData data, Component component)
        {
            if (data == null)
            {
                return;
            }

            data.name = component != null ? component.name : string.Empty;
            if (component == null)
            {
                data.parentSet = ParentSet.NoChange;
                data.anchoredPosition = Vector2.zero;
                data.localSize = 1f;
                return;
            }

            RectTransform rectTransform = component.transform as RectTransform;
            if (rectTransform != null)
            {
                data.parentSet = GetParentSet(rectTransform);
                data.anchoredPosition = rectTransform.anchoredPosition;
                data.localSize = rectTransform.localScale.x;
                return;
            }

            data.parentSet = ParentSet.NoChange;
            data.anchoredPosition = Vector2.zero;
            data.localSize = 1f;
        }

        private ParentSet GetParentSet(Transform target)
        {
            if (target == null)
            {
                return ParentSet.NoChange;
            }

            if (target.parent == leftDown)
            {
                return ParentSet.LeftDown;
            }

            if (target.parent == rightDown)
            {
                return ParentSet.RightDown;
            }

            return ParentSet.NoChange;
        }

        private void MarkLayoutTargetsDirty()
        {
            MarkTargetDirty(transform);
            MarkTargetDirty(leftDown);
            MarkTargetDirty(rightDown);
            MarkTargetDirty(slotParent);

            if (joystick != null)
            {
                MarkTargetDirty(joystick.transform);
            }

            if (extraSkillBtn != null)
            {
                MarkTargetDirty(extraSkillBtn.transform);
            }

            if (rollBtn != null)
            {
                MarkTargetDirty(rollBtn.transform);
            }

            if (jumpBtn != null)
            {
                MarkTargetDirty(jumpBtn.transform);
            }

            if (norAtkBtn != null)
            {
                MarkTargetDirty(norAtkBtn.transform);
            }

            List<SkillSlot> layoutSlots = GetLayoutSlots(true);
            if (layoutSlots == null)
            {
                return;
            }

            for (int i = 0; i < layoutSlots.Count; i++)
            {
                if (layoutSlots[i] != null)
                {
                    MarkTargetDirty(layoutSlots[i].transform);
                }
            }
        }

        private static void MarkTargetDirty(Transform target)
        {
            if (target == null)
            {
                return;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
#endif
    }
}
