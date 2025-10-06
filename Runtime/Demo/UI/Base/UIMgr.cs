using EasyUI.Toast;
using NaughtyAttributes;
using System.Collections.Generic;
using DG.Tweening;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

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

        public LevelPanel levelPanel;

        public SettingPanel settingPanel;

        public PlayerPanel playerPanel;

        public StandaloneInputHud standaloneInputHud;

        public MobileInputHud mobileInputHud;

        public HashSet<PanelBase> showingPanels = new HashSet<PanelBase>();

        public TalkPanel talkPanel;

        [ReadOnly] public PanelBase lastPanel;

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

        public void ShowView(UIPanelType type)
        {
            PanelBase panel = GetPanel(type);
            panel.Show();
            if (IsHideMid(type))
            {
                midCanvas.enabled = false;
            }

            showingPanels.Add(panel);
            lastPanel = panel;
            CheckPlayInputAble();
            GameEvent.Send<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.Int(), type, false);
        }

        public void HideView(UIPanelType type)
        {
            PanelBase panel = GetPanel(type);
            if (!panel || !panel.IsShowing)
            {
                return;
            }

            panel.IsShowing = false;
            panel.Hide();
            showingPanels.Remove(panel);

            if (IsHideMid(type))
            {
                midCanvas.enabled = true;
            }

            CheckPlayInputAble();
        }

        public void MidCanvasEnable(bool enable)
        {
            midCanvas.enabled = enable;
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
                    if (lastPanel != null && lastPanel.IsShowing)
                    {
                        lastPanel.Hide();
                    }
                }
            }
        }

        //屏蔽输入
        void CheckPlayInputAble()
        {
            bool can = true;
            foreach (var panel in showingPanels)
            {
                if (panel.StopPlayerControl && panel.IsShowing)
                {
                    can = false;
                }
            }

            if (TalkMgr.Inst.isTalking)
            {
                can = false;
            }

            GameAllData.battleData.CanPlayerControl.SetValue(can);
        }

        public PanelBase GetPanel(UIPanelType type)
        {
            if (type == UIPanelType.LevelPanel)
            {
                return levelPanel;
            }
            else if (type == UIPanelType.SettingPanel)
            {
                return settingPanel;
            }
            else if (type == UIPanelType.PlayerPanel)
            {
                return playerPanel;
            }

            Debuger.LogError($"--- no panel {type}");
            return null;
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

        public static void PopToast(string str, float time = 1)
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