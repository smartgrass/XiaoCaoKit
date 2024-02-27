using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

namespace Flux
{
    [FEvent("Message/Command")]

    public class FCommandEvent : FEvent
    {
        //类名
        public string commandName;

        public XCEventMsg baseMsg;


        public override string Text
        {
            get
            {
                return commandName;
            }
        }

        public override XCEvent ToXCEvent()
        {
            if (string.IsNullOrEmpty(commandName))
            {
                Debug.LogError("commandName null");
                return null;
            }

            FCommandEvent fe = this;

            XCCommondEvent xce = new XCCommondEvent();
            var msg = baseMsg;
            xce.eName = commandName;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.baseMsg = msg.baseMsg;
            return xce;
        }
    }
}
