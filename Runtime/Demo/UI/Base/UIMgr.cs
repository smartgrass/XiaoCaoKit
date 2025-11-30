using EasyUI.Toast;
using NaughtyAttributes;
using System.Collections.Generic;
using DG.Tweening;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>, IMgr, ICanvasMgr
    {
        public override bool NeedDontDestroy => false;

        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel
        public Canvas topCanvas;
        public Canvas midCanvas;

        public BattleHud battleHud;

        public SettingPanel settingPanel;

        public PlayerPanel playerPanel;

        public StandaloneInputHud standaloneInputHud;

        public MobileInputHud mobileInputHud;

        public HashSet<PanelBase> showingPanels = new HashSet<PanelBase>();


        public TalkPanel talkPanel;

        public LevelResultPanel levelResultPanel;

        // Buff选择面板
        public BuffSelectPanel buffSelectPanel;

        [ReadOnly] public PanelBase lastpanel;

        public Canvas Canvas => topCanvas;
        //BlackScreenUI

        public override void Init()
        {
            base.Init();
            UICanvasMgr.Inst.canvasMgr = this;
            battleHud?.Init();
            settingPanel?.Init();
            playerPanel?.Init();
            transform.SetParent(null);
            talkPanel.Init();
            buffSelectPanel.Init();
            OnChangeInputType(GameSetting.UserInputType);
        }

        private void OnDestroy()
        {
        }

        public void OnChangeInputType(UserInputType type)
        {
            bool isTouch = type == UserInputType.Touch;
            mobileInputHud.gameObject.SetActive(isTouch);
            standaloneInputHud.gameObject.SetActive(true);
            standaloneInputHud.SetTouchShow(isTouch);
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
                Debug.Log($"-- {panel.PanelType} is showing ");
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
            GameEvent.Send<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.Int(), type, false);
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
            //电脑端
            if (showingPanels.Count <= 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ShowView(UIPanelType.SettingPanel);
                }
                else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.F1))
                {
                    ShowView(UIPanelType.PlayerPanel);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (lastpanel != null && lastpanel.IsShowing && lastpanel.HideEsc)
                    {
                        HideView(lastpanel.PanelType);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (lastpanel != null && lastpanel.IsShowing)
                    {
                        lastpanel.InputKeyCode(KeyCode.Space);
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

        public static void PopToastKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                PopToast(key.ToLocalizeStr());
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
    }


    //跨场景使用
    public class UICanvasMgr : Singleton<UICanvasMgr>, IMgr
    {
        public ICanvasMgr canvasMgr;

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