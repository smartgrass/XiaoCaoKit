using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XiaoCao;

namespace Flux
{
    [FEvent("Message/Command")]

    public class FCommandEvent : FEvent
    {
        //类名
        [Dropdown(nameof(GetCommandNames))]
        public string commandName;

        public BaseMsg baseMsg;

        public string[] GetCommandNames
        {
            get
            {
                var dic = XCCommandBinder.Inst.GetAllCommandTypes();
                List<string> list = new List<string>();
                list.Add("");
                list.AddRange(dic.Keys);
                return list.ToArray();
            }
        }

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
            xce.eName = commandName;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.baseMsg = baseMsg;
            return xce;
        }
    }
}
