using EasyUI.Toast;
using NaughtyAttributes;
using System.Collections.Generic;
using DG.Tweening;
using EasyUI.Helpers;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>, IMgr, ICanvasMgr
    {
        public override bool NeedDontDestroy => false;

        private const string ItemDetailPopPath = "Assets/_Res/UI/ItemDetailPop.prefab";

        private ItemDetailPop _itemDetailPop;

        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel
        public Canvas topCanvas;
        public Canvas midCanvas;
        public WorldCanvasChildUsing worldCanvasChildUsing;

        public BattleHud battleHud;
        
        public LevelSelectPanel levelSelectPanel;
        
        public ForgePanel forgePanel;
        
        public SettingPanel settingPanel;

        public PlayerTabPanel playerPanel;
        

        public KeyInputHud keyInputHud;

        public HashSet<PanelBase> showingPanels = new HashSet<PanelBase>();

        //标记后 将不轻易解除Canvas
        public bool keepHideMidCanvas = false;

        public TalkPanel talkPanel;

        public LevelResultPanel levelResultPanel;

        // Buff选择面板
        public BuffSelectPanel buffSelectPanel;

        public RebornPanel rebornPanel;

        public GameObject levelUpTip;

        [ReadOnly] public PanelBase lastpanel;

        public Canvas Canvas => topCanvas;

        //BlackScreenUI

        public override void Init()
        {
            base.Init();
            UICanvasMgr.Inst.canvasMgr = this;
            battleHud?.Init(worldCanvasChildUsing);
            settingPanel?.Init();
            playerPanel?.Init();
            transform.SetParent(null);
            talkPanel.Init();
            buffSelectPanel.Init();
            rebornPanel?.Init();
            forgePanel.Hide();
            levelUpTip.gameObject.SetActive(false);
            OnChangeInputType(GameSetting.UserInputType);
            OnStageUISet(GameDataCommon.Current.gameStage);
            GameEvent.AddEventListener<int, int>(EGameEvent.RoleLevelUp.ToInt(), OnRoleLevelUp);
        }

        //入口和战斗ui有所区别
        private void OnStageUISet(EGameStage currentGameStage)
        {
            if (currentGameStage ==   EGameStage.Level)
            {
                battleHud.gameObject.SetActive(true);
            }
            else if (currentGameStage == EGameStage.Home)
            {
                battleHud.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, int>(EGameEvent.RoleLevelUp.ToInt(), OnRoleLevelUp);
            UICanvasMgr.Inst.EventSystem.ClearAllEvents();
        }

        public void OnChangeInputType(UserInputType type)
        {
            keyInputHud.gameObject.SetActive(true);
            keyInputHud.RefreshInputTypeUI();
        }

        public void ShowView(UIPanelType type, IUIData data = null)
        {
            PanelBase panel = GetPanel(type);
            if (panel.NeedUIData && data == null)
            {
                Debug.LogError($"-- need uiData");
                return;
            }

            if (panel.IsShowing)
            {
                if (panel.NeedUIData)
                {
                    panel.Show(data);
                }
                else
                {
                    Debug.LogError($"-- panel {type} NeedUIData false");
                }

                return;
            }

            panel.Show(data);
            if (IsHideMid(type))
            {
                MidCanvasEnable(false);
            }

            showingPanels.Add(panel);
            lastpanel = panel;
            CheckPlayInputAble();
            GameEvent.Send<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.ToInt(), type, false);
        }

        public void HideView(UIPanelType type)
        {
            PanelBase panel = GetPanel(type);
            if (!panel)
            {
                return;
            }

            if (showingPanels.Contains(panel))
            {
                showingPanels.Remove(panel);
                CheckPlayInputAble();

                //简单处理, 完整应该遍历所有面板
                if (showingPanels.Count == 0)
                {
                    MidCanvasEnable(true);
                }
            }

            if (panel.IsShowing)
            {
                panel.Hide();
            }
        }

        public void MidCanvasEnable(bool isOn)
        {
            midCanvas.enabled = isOn;
        }

        public void PopUIEnable(bool isOn, string uiName)
        {
            TimeStopMgr.UIStopTime(isOn, uiName);
        }

        private void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            if (DialogManager.HasDialog())
            {
                return;
            }

            //Esc
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (lastpanel != null && lastpanel.IsShowing && lastpanel.HideEsc)
                {
                    HideView(lastpanel.PanelType);
                }
            }


            //无ui时
            if (showingPanels.Count <= 0)
            {
                // if (Input.GetKeyDown(KeyCode.Escape))
                // {
                //     ShowView(UIPanelType.SettingPanel);
                // }
                if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.F1))
                {
                    ShowView(UIPanelType.PlayerPanel);
                }
            }
            else
            {
                if (Input.GetKeyDown(XCInputSetting.Space))
                {
                    if (lastpanel != null && lastpanel.IsShowing)
                    {
                        lastpanel.InputKeyCode(XCInputSetting.Space);
                    }
                }
            }
        }

        //屏蔽输入
        public void CheckPlayInputAble()
        {
            bool can = true;
            foreach (var panel in showingPanels)
            {
                if (panel.StopPlayerControl && panel.IsShowing)
                {
                    can = false;
                    break;
                }
            }

            if (TalkMgr.Inst.isTalking)
            {
                can = false;
            }

            // if (GameAllData.battleData.isDialogShow)
            // {
            //     Debug.Log($"-- GameAllData.battleData.isDialogShow");
            //     can = false;
            // }

            GameAllData.battleData.CanPlayerControl.SetValue(can);
        }

        public PanelBase GetPanel(UIPanelType type)
        {
            switch (type)
            {
                case UIPanelType.SettingPanel:
                    return settingPanel;
                case UIPanelType.PlayerPanel:
                    return playerPanel;
                case UIPanelType.BuffSelectPanel:
                    return buffSelectPanel;
                case UIPanelType.LevelResultPanel:
                    return levelResultPanel;
                case UIPanelType.TalkPanel:
                    return talkPanel;
                case UIPanelType.RebornPanel:
                    return rebornPanel;
                case UIPanelType.LevelSelectPanel:
                    return levelSelectPanel;
                case UIPanelType.ForgePanel:
                    return forgePanel;
                default:
                    Debuger.LogError($"--- no panel {type}");
                    return null;
            }
        }

        public bool IsHideMid(UIPanelType type)
        {
            return true;
        }

        public void PlayDamageText(int atk, Vector3 textPos)
        {
            battleHud.ShowDamageText(atk.ToString(), textPos);
        }

        void PopToastInst(string str, float time = 1)
        {
            PopToast(str, time);
            ;
        }

        public static void PopToastKey(string key, float time = 1)
        {
            if (!string.IsNullOrEmpty(key))
            {
                PopToast(key.ToLocalizeStr(), time);
            }
        }

        public static void PopToast(string str, float time = 1.5f)
        {
            Toast.Show(str, time);
        }

        public static void PopRewardItem(Item item)
        {
            //TODO
        }

        public static void PopRewardBuffItem()
        {
        }

        /// <summary>
        /// 由物品格弹出详情窗口: 首次调用时加载预制体并挂在顶层画布下, 之后复用同一实例;
        /// 详情跟随来源格子关闭, 格子刷新物品、被隐藏或所属界面关闭时一并收起
        /// </summary>
        public void PopItemDetail(ItemCell cell)
        {
            if (_itemDetailPop == null)
            {
                GameObject prefab = ResMgr.LoadAseet<GameObject>(ItemDetailPopPath);
                _itemDetailPop = Instantiate(prefab, topCanvas.transform).GetComponent<ItemDetailPop>();
                //预制体默认激活, 先关闭避免无数据时闪出空弹窗
                _itemDetailPop.Hide();
            }

            _itemDetailPop.Show(cell.Item, cell);
        }

        public void PopLevelUpTip(int levelFrom , int levelTo)
        {
            levelUpTip.SetActive(false);
            var levelText = levelUpTip.transform.Find("Image/LevelText").GetComponent<TMP_Text>();
            levelText.text = $"Lv{levelFrom}        Lv{levelTo}";
            levelUpTip.SetActive(true);
            
        }

        /// <summary>
        /// 角色升级回调: 弹出升级提示
        /// </summary>
        private void OnRoleLevelUp(int levelFrom, int levelTo)
        {
            PopLevelUpTip(levelFrom, levelTo);
        }
    }


    //跨场景使用
    public class UICanvasMgr : Singleton<UICanvasMgr>, IMgr
    {
        public ICanvasMgr canvasMgr;

        private UIEventSystem _eventSystem;

        public UIEventSystem EventSystem
        {
            get
            {
                if (_eventSystem == null)
                {
                    _eventSystem = new UIEventSystem();
                }

                return _eventSystem;
            }
        }

        public Transform GetCanvasParent()
        {
            return canvasMgr.Canvas.transform.parent;
        }
    }

    public interface ICanvasMgr
    {
        public Canvas Canvas { get; }
    }
}
