using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <see cref="HomeMainPanel"/>
    /// <see cref="HomeFightPanel"/>
    public class HomeHud : MonoBehaviour
    {
        public static HomeHud Inst;
        
        public static UIEventSystem EventSystem => Inst.eventSystem;

        // HomeHud的事件系统实例
        public UIEventSystem eventSystem;

        private void Awake()
        {
            Inst = this;
            eventSystem = new UIEventSystem();
        }


        public List<GameObject> panels;


        public void SwitchPanel(EHomePanel panel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(false);
            }

            panels[(int)panel].SetActive(true);
        }

        public void BackToMain()
        {
            SwitchPanel(EHomePanel.MainPanel);
        }
    }

    public enum EHomePanel
    {
        MainPanel,
        FightPanel,
    }
}