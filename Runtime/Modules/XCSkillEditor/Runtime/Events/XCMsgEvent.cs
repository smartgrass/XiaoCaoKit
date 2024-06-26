﻿using UnityEngine;
using UnityEngine.UIElements;

namespace XiaoCao
{
    public class XCMsgEvent : XCEvent
    {
        public EntityMsgType msgType;
        public BaseMsg baseMsg;
        public bool isUndoOnFinish;

        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            if (msgType == EntityMsgType.SetNoBusy)
            {
                task.SetNoBusy();
            }
            baseMsg.state = 0;
            Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
        }



        public override void OnFinish()
        {
            base.OnFinish();
            if (HasTrigger && isUndoOnFinish)
            {
                baseMsg.state = 1;
                Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
            }
        }
    }
}
