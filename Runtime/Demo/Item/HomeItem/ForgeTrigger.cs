using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class ForgeTriggerBase : HomeTriggerBase
    {
        public override void TriggerSucceed()
        {
            //打开锻造界面
            UIMgr.Inst.ShowView(UIPanelType.ForgePanel);
        }
    }
}