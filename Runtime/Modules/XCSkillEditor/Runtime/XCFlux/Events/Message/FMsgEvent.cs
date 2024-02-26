using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XiaoCao;
using static UnityEngine.UI.InputField;

namespace Flux
{
    [FEvent("Message/Msg")]

    public class FMsgEvent : FEvent
    {
        public XCEventMsg[] msgList = new XCEventMsg[0];

        public bool HasEvent
        {
            get => msgList.Length > 0;
        }


        public override string Text
        {
            get
            {
                if (msgList.Length > 0)
                {
                    return msgList[0].msgType.ToString();
                }
                return null;
            }
        }

        public List<XCMsgEvent> ToXCEventList()
        {
            FMsgEvent fe = this;
            List<XCMsgEvent> list = new List<XCMsgEvent>();

            int len = fe.msgList.Length;
            for (int i = 0; i < len; i++)
            {
                XCMsgEvent xce = new XCMsgEvent();
                var msg = fe.msgList[i];
                xce.eName = "MsgEvent";
                xce.range = new XCRange(fe.Start, fe.End);

                xce.isUndoOnFinish = msg.isUndoOnFinish;
                xce.baseMsg = msg.baseMsg;
                xce.msgType = msg.msgType;
                list.Add(xce);
            }
            return list;

        }
    }
}
