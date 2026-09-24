using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class TaskTriggerBase : HomeTriggerBase
    {
        public override void TriggerSucceed()
        { 
            //打开任务界面
            UIMgr.Inst.ShowView(UIPanelType.TaskPanel);
        }
    }
}