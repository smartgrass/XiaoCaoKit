using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class BagTriggerBase : HomeTriggerBase
    {
        public override void TriggerSucceed()
        {
            //打开背包
            UIMgr.Inst.ShowView(UIPanelType.PlayerPanel);
            UIMgr.Inst.playerPanel.SwitchPanel(nameof(PlayerSubTabNames.BagTab));
        }
    }
}