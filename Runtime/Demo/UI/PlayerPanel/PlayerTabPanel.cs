using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
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

        //方向键导航: 记录上一帧移动轴值, 用于检测按下瞬间
        private float lastVerticalAxis;
        private float lastHorizontalAxis;

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
            UIMgr.Inst.HideView(UIPanelType.PlayerPanel);
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

        private void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }
            HandleArrowKeyNavigation();
        }

        /// <summary>
        /// 方向键导航: 上下键(玩家移动键的上下, W/S 同效)切换主页签,
        /// 左右键(A/D 同效)切换当前子界面的页签
        /// </summary>
        private void HandleArrowKeyNavigation()
        {
            //使用移动轴检测, 与玩家移动键绑定保持一致
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");

            if (lastVerticalAxis <= 0 && vertical > 0)
            {
                SwitchTab(1);
            }
            else if (lastVerticalAxis >= 0 && vertical < 0)
            {
                SwitchTab(-1);
            }
            lastVerticalAxis = vertical;

            if (lastHorizontalAxis <= 0 && horizontal > 0)
            {
                curPanel?.gameObject.GetComponent<TabView>()?.SwitchTab(1);
            }
            else if (lastHorizontalAxis >= 0 && horizontal < 0)
            {
                curPanel?.gameObject.GetComponent<TabView>()?.SwitchTab(-1);
            }
            lastHorizontalAxis = horizontal;
        }
    }

    public enum PlayerSubTabNames
    {
        RoleView,
        SkillView,
        BagTab
    }
}
