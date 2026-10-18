using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao.UI
{
    //等级次于TabPanel,属于子界面
    public abstract class TabView : MonoBehaviour
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

            TabPanelGroup firstVisible = null;
            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                var subPanel = InitGroup(group);
                subPanel.gameObject.SetActive(false);

                var visible = IsGroupVisible(group);
                SetGroupVisible(group, visible);
                if (visible && firstVisible == null)
                {
                    firstVisible = group;
                }
            }

            //默认展示第一个可见的页签
            if (firstVisible != null)
            {
                SwitchPanel(firstVisible.panelName);
            }
        }

        /// <summary>
        /// 页签是否显示, 子类可按数据内容隐藏页签(如背包为空时隐藏)
        /// </summary>
        public virtual bool IsGroupVisible(TabPanelGroup group)
        {
            return true;
        }

        private readonly Dictionary<TabPanelGroup, bool> _tabRootDefaultActive = new Dictionary<TabPanelGroup, bool>();

        /// <summary>
        /// 显隐整个页签按钮节点: tabBtn 通常只是分组根节点下的"Btn"子物体,
        /// 只隐藏按钮本身会留下底板和高亮, 所以这里按根节点处理
        /// </summary>
        protected void SetGroupVisible(TabPanelGroup group, bool visible)
        {
            if (group == null || group.tabBtn == null)
            {
                return;
            }

            var parent = group.tabBtn.transform.parent;
            var tabRoot = parent != null ? parent.gameObject : group.tabBtn.gameObject;

            if (!_tabRootDefaultActive.ContainsKey(group))
            {
                _tabRootDefaultActive.Add(group, tabRoot.activeSelf);
            }

            var target = _tabRootDefaultActive[group] && visible;
            if (tabRoot.activeSelf != target)
            {
                tabRoot.SetActive(target);
            }
        }

        public List<TabPanelGroup> groups = new List<TabPanelGroup>();


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

        /// <summary>
        /// 切换到相邻的可见子页签(方向键导航用), dir>0 切到下一个, dir<0 切到上一个, 循环切换,
        /// 隐藏的页签不参与切换
        /// </summary>
        public void SwitchTab(int dir)
        {
            //IsGroupVisible 是显隐依据, tabBtn.activeInHierarchy 兜底确认按钮实际处于显示状态
            var visibleGroups = groups.FindAll(g => g != null && IsGroupVisible(g)
                && g.tabBtn != null && g.tabBtn.gameObject.activeInHierarchy);
            if (visibleGroups.Count == 0)
            {
                return;
            }
            int index = visibleGroups.FindIndex(g => curPanel != null && g.panelName == curPanel.subPanelName);
            if (index < 0)
            {
                index = 0;
            }
            index = (index + dir) % visibleGroups.Count;
            if (index < 0)
            {
                index += visibleGroups.Count;
            }
            SwitchPanel(visibleGroups[index].panelName);
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
    }
}