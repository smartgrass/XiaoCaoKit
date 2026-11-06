using NaughtyAttributes;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 背包, 技能界面
    /// </summary>
    public class PlayerTabPanel : TabStandardPanel
    {
        //技能子界面的页签名(与预制体 groups 配置一致)
        public const string SkillPanelName = "Skill";

        public override UIPanelType PanelType => UIPanelType.PlayerPanel;

        //启用方向键导航: 上下键切主 tab, 左右键切子界面 tab
        public override bool EnableArrowKeyNavigation => true;

        public Image titleImage;

        [Tooltip("金币条,只在指定的子界面显示")]
        public GameObject coinBar;

        [Tooltip("显示金币条的子界面名")]
        public List<string> coinBarShowPanels = new List<string>() { "Skill", "RelicView" };

        public List<TabPanelGroup> groups = new List<TabPanelGroup>();

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();
            gameObject.SetActive(false);


            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                var subPanel = InitGroup(group);
                subPanel.gameObject.SetActive(index == 0);
            }
            SwitchPanel(groups[0].panelName);

            GameEvent.AddEventListener(EGameEvent.SkillRedDotChange.ToInt(), RefreshTabRedDots);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener(EGameEvent.SkillRedDotChange.ToInt(), RefreshTabRedDots);
        }

        /// <summary>
        /// 面板显示时重算页签红点: 隐藏期间(如技能升级)状态可能已经变化
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();
            if (IsInited)
            {
                RefreshTabRedDots();
            }
        }

        /// <summary>
        /// 技能页签存在可学习的技能时显示红点
        /// </summary>
        protected override bool HasRedDot(string subPanelName)
        {
            return subPanelName == SkillPanelName && SkillLearnRedDot.HasAnyNew();
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
            subPanel.TabRedDot = group.tabRedDot;

            var tabBtn = group.tabBtn;
            tabBtn.onClick.AddListener(() => { SwitchPanel(group.panelName); });

            subPanel.Init();
            list.Add(subPanel);
            return subPanel;
        }

        /// <summary>
        /// 切换子界面:同步标题图标,并按配置控制金币条显隐
        /// </summary>
        public override void SwitchPanel(string subPanelName)
        {
            base.SwitchPanel(subPanelName);
            var group = groups.Find((a) => a.panelName == subPanelName);
            titleImage.sprite = group.iconSprite;
            coinBar.SetActive(coinBarShowPanels.Contains(subPanelName));
        }

        /// <summary>
        /// 左右键: 切换当前子界面的页签
        /// </summary>
        protected override void OnArrowHorizontal(int dir)
        {
            curPanel?.gameObject.GetComponent<TabView>()?.SwitchTab(dir);
        }
    }

    public enum PlayerSubTabNames
    {
        RoleView,
        SkillView,
        BagTab
    }
}
