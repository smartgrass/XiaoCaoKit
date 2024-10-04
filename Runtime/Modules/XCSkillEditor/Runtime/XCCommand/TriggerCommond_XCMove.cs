using System;
using UnityEngine;

namespace XiaoCao
{
    public class TriggerCommond_XCMove
    {
        public XCMoveEvent moveEvent;
        public string msg;
        internal void Trigger(XCMoveEvent moveEvent, string msg)
        {
            this.moveEvent = moveEvent;
            this.msg = msg;
            if (msg == Role.WeaponFirePointName)
            {
                ToWeaponFirePonit();
            }

        }

        private void ToWeaponFirePonit()
        {
            var role = moveEvent.Info.role;
            if (role.GetPonitCache(msg, out Transform tf))
            {
                Vector3 end = moveEvent.endVec + (tf.position - moveEvent.startVec);
                moveEvent.ResetStartEnd(tf.position, end);
            }
        }
    }

    public static class TriggerCommondHelper
    {
        public static void AddCommond(ETriggerCmd type, string msg, XCEvent xcEvent)
        {

            if (type == ETriggerCmd.MoveTrigger)
            {
                var trigger = new TriggerCommond_XCMove();
                trigger.Trigger(xcEvent as XCMoveEvent, msg);
            }
        }
    }

    public enum ETriggerCmd
    {
        None = 0,
        MoveTrigger = 1,
    }


}
