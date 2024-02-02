using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiaoCao;
using static UnityEngine.UI.InputField;

namespace Flux
{
    [FEvent("Message/Msg")]

    internal class FMsgEvent : FEvent
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
                    return msgList[0].entityMsg.ToString();
                }
                return null;
            }
        }
    }
}
