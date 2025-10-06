using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoKit
{
    public class HomeHud : MonoBehaviour
    {
        public static HomeHud Inst;

        private void Awake()
        {
            Inst = this;
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
        FightPanel
    }
}