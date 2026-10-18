using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 背包页签: 纹章/道具为空时隐藏对应页签, 只保留有内容的页签
    /// <see cref="BuffView"/>
    /// <see cref="ExtraItemView"/>
    /// <see cref="ConsumView"/>
    /// </summary>
    public class BagTabView : TabView
    {
        public override void OnEnable()
        {
            //数据可能在面板关闭期间变化, 每次打开都重新计算页签显隐
            RefreshTabVisible();
            base.OnEnable();
        }

        public override bool IsGroupVisible(TabPanelGroup group)
        {
            if (group == null || group.panel == null)
            {
                return true;
            }

            if (group.panel.GetComponent<BuffView>() != null)
            {
                return BuffView.HasEquippedBuff();
            }

            if (group.panel.GetComponent<ExtraItemView>() != null)
            {
                return ExtraItemView.HasAnyExtraItem();
            }

            //消耗品页签始终显示, 空背包由 ConsumView 自己提示
            return true;
        }

        /// <summary>
        /// 刷新页签按钮显隐, 当前页签被隐藏时切到第一个可见页签
        /// </summary>
        public void RefreshTabVisible()
        {
            TabPanelGroup firstVisible = null;
            var curVisible = true;

            foreach (var group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                var visible = IsGroupVisible(group);
                SetGroupVisible(group, visible);

                if (visible && firstVisible == null)
                {
                    firstVisible = group;
                }

                if (curPanel != null && curPanel.subPanelName == group.panelName)
                {
                    curVisible = visible;
                }
            }

            if (!curVisible && firstVisible != null)
            {
                SwitchPanel(firstVisible.panelName);
            }
        }
    }
}
