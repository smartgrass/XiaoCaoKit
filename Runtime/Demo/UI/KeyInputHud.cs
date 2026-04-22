using MFPC;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao.UI;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    //pc和移动端共用
    public class KeyInputHud : GameStartMono, IClearCache
    {
        #region References
        public Joystick joystick;

        public Transform slotParent;
        [ReadOnly] public List<SkillSlot> slots;
        public SkillSlot extraSkillBtn; //特殊道具按钮

        public SkillSlot rollBtn;
        public Button jumpBtn;
        public Button norAtkBtn;
        public TouchField touchField;
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
            slots = slotParent.GetComponentsInChildren<SkillSlot>().ToList();
            slots.Remove(extraSkillBtn);
            if (HasIndependentRollBtn)
            {
                rollBtn.slotType = SlotType.Inputs;
                rollBtn.index = InputKey.LeftShift;
            }

            norAtkBtn.onClick.AddListener(() => { PlayerInput.inputs[InputKey.NorAck] = true; });
            jumpBtn.onClick.AddListener(() => { PlayerInput.inputs[InputKey.Space] = true; });

            SetupExtraSkillButton();
            RefreshExtraSkillBtnState(true);
            CheckBarImg();

            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.SkillChange, CheckBarImg);
            RefreshInputTypeUI();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.SkillChange, CheckBarImg);
            ClearExtraSkillSkipClick();
            RemoveExtraSkillButtonListener();
        }

        private void Update()
        {
            CleanupExtraSkillSkipClick();
            if (GameAllData.CommonData.gameState != GameState.Running)
            {
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
                return;
            }

            CheckUIUpdate();
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
            
            joystick.transform.parent.gameObject.SetActive(GameSetting.UserInputType == UserInputType.Touch);
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
            if (GameSetting.UserInputType == UserInputType.Touch)
            {
                ResetExtraSkillKeyState(true);
                return;
            }

            bool canUseExtraSkill = extraSkillBtn != null &&
                                    extraSkillBtn.isActiveAndEnabled &&
                                    extraSkillBtn.slotType == SlotType.ExtraItem &&
                                    BattleData.Current.CanPlayerControl &&
                                    !BattleData.Current.UIEnter;
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
            bool isUseSuccess = GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.TryUseExtraSkill();
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
    }
}
