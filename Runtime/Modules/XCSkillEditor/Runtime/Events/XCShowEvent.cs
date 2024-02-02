using static XiaoCao.PlayerAtkTimer;
using UnityEngine;
using static UnityEngine.UI.InputField;
using System.Buffers;
using System.Data;

namespace XiaoCao
{
    //控制物体显示隐藏
    public class XCShowEvent : XCEvent
    {
        public bool hideOnEnd = true;

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            task.Show(true);
        }

        public override void OnFinish()
        {
            base.OnFinish();
            if (hideOnEnd)
            {
                task.Show(false);
            }
        }

    }


}
