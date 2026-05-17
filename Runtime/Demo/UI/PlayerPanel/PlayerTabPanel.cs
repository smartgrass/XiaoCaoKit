using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 背包, 技能界面
    /// </summary>
    public class PlayerTabPanel : TabStandardPanel
    {
        public override UIPanelType PanelType => UIPanelType.PlayerPanel;

        public Image titleImage;

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
            if (GameDataCommon.Current.isFighting)
            {
                gameObject.SetActive(false);
            }
        }

        [Button]
        public override void OnCloseBtnClick()
        {
            if (GameDataCommon.Current.isFighting)
            {
                UIMgr.Inst.HideView(UIPanelType.PlayerPanel);
            }
            else
            {
                UICanvasMgr.Inst.EventSystem.SendEvent(UIEventNames.SwitchSubView, EHomeSubView.Main);
            }
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
            base.SwitchPanel(subPanelName);
            var group = groups.Find((a) => a.panelName == subPanelName);
            titleImage.sprite = group.iconSprite;
        }
    }
}