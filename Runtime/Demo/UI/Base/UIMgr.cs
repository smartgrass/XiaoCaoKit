using EasyUI.Helpers;
using EasyUI.Toast;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    public class UIMgr : MonoSingletonPrefab<UIMgr>, IMgr
    {
        public override bool NeedDontDestroy => false;

        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel
        public Canvas topCanvas;
        public Canvas midCanvas;


        public BattleHud battleHud;

        public SkillBarHud skillBarHud;

        public LevelPanel levelPanel;

        public SettingPanel settingPanel;

        public PlayerPanel playerPanel;

        public HashSet<PanelBase> showingPanels = new HashSet<PanelBase>();

        public PanelBase lastPanel;

        public Transform mobileInputHudTf;
        //懒加载 或 主动加载

        public override void Init()
        {
            base.Init();
            battleHud?.Init();
            skillBarHud?.Init();
            settingPanel?.Init();
            playerPanel?.Init();
            transform.SetParent(null);
            mobileInputHudTf = midCanvas.transform.Find("MobileInputHud");
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


        private void Update()
        {
            //电脑端
            if (showingPanels.Count <= 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ShowView(UIPanelType.SettingPanel);
                }
                else if (Input.GetKeyDown(KeyCode.C))
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
            battleHud.ShowDamageText(atk, textPos);
        }

        public static void PopToast(string str, float time = 1)
        {
            Toast.Show(str, time);
        }

    }
}


