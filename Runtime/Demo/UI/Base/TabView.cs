using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao.UI
{
    //等级次于TabPanel,属于子界面
    public abstract class TabView: MonoBehaviour
    {
        [NonSerialized] public List<SubPanel> list = new List<SubPanel>();
        [NonSerialized] public SubPanel curPanel;

        public Transform PANELS;
        public Transform TABS;

        public bool IsInited { get; set; }

        public virtual void Init()
        {
            if (IsInited)
            {
                return;
            }

            IsInited = true;
        }


        public virtual void OnEnable()
        {
            Init();
            if (IsInited)
            {
                curPanel?.RefreshUI();
            }
        }


        public virtual void SwitchPanel(string subPanelName)
        {
            foreach (var subPanel in list)
            {
                if (subPanel.subPanelName != subPanelName)
                {
                    subPanel.Hide();
                }
                else
                {
                    curPanel = subPanel;
                    curPanel.Show();
                }
            }
        }

        public SubPanel GetPanel(string subPanelName)
        {
            return list.Find(p => p.subPanelName == subPanelName);
        }
        
    }
}