using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <see cref="BuffView"/>
    /// <see cref=""/>
    public class BagTabView : TabView
    {
        public List<TabPanelGroup> groups = new List<TabPanelGroup>();

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();


            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                var subPanel = InitGroup(group);
                subPanel.gameObject.SetActive(index == 0);
            }

            SwitchPanel(groups[0].panelName);
        }


        private SubPanel InitGroup(TabPanelGroup group)
        {
            var subPanel = new SimpleSubPanel();
            subPanel.gameObject = group.panel;
            subPanel.subPanelName = group.panelName;
            subPanel.TabBtn = group.tabBtn;
            var tabBtn = group.tabBtn;
            tabBtn.onClick.AddListener(() => { SwitchPanel(group.panelName); });

            subPanel.Init();
            list.Add(subPanel);
            return subPanel;
        }

        public override void SwitchPanel(string subPanelName)
        {
            Debug.Log($"--  SwitchPanel {subPanelName}");
            base.SwitchPanel(subPanelName);
            // var group = groups.Find((a) => a.panelName == subPanelName);
            // titleImage.sprite = group.iconSprite;
        }
    }
}