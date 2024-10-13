using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>,IMgr
    {
        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel
        public Canvas topCanvas;
        public Canvas midCanvas;


        public BattleHud battleHud;

        public SkillBarHud skillBarHud;

        public LevelPanel levelPanel;

        public DebugPanel debugPanel;

        public SettingPanel settingPanel;

        public PlayerPanel playerPanel;

        public HashSet<PanelBase> panels = new HashSet<PanelBase>();

        //懒加载 或 主动加载

        public override void Init()
        {
            base.Init();
            battleHud?.Init();
            skillBarHud?.Init();
            //debugPanel?.Init();
            settingPanel?.Init();
            playerPanel?.Init();
        }

        public void ShowView(UIPanelType type)
        {
            PanelBase panel = GetPanel(type);
            panel.Show();
            if (IsHideMid(type))
            {
                midCanvas.enabled = false;
            }
            panels.Add(panel);
            CheckPlayInputAble();
        }

        public void HideView(UIPanelType type)
        {
            PanelBase panel = GetPanel(type);
            if (panel && panel.IsShowing)
            {
                panel.IsShowing = false;
                panel.Hide();
                panels.Remove(levelPanel);
            }
            if (IsHideMid(type))
            {
                midCanvas.enabled = true;
            }
            CheckPlayInputAble();
        }



        //屏蔽输入
        void CheckPlayInputAble()
        {
            bool can = true;
            foreach (var panel in panels)
            {
                if (panel.StopPlayerControl && panel.IsShowing)
                {
                    can = false;
                }
            }
            GameData.battleData.CanPlayerControl.SetValue(can);
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
            if (type == UIPanelType.SettingPanel)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void PlayDamageText(int atk, Vector3 textPos)
        {
            battleHud.ShowDamageText(atk, textPos);
        }
    }
}


