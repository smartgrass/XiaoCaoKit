using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;

namespace Flux
{
    [FEvent("Message/Command")]
    public class FCommandEvent : FEvent
    {
        //类名
        [Dropdown(nameof(GetCommandNames))]
        public string commandName;

        public BaseMsg baseMsg;

        public string[] otherMsgs;

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
            if (otherMsgs == null)
            {
                otherMsgs = new string[0];
            }
        }

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
            xce.otherMsgs = otherMsgs == null ? null : (string[])otherMsgs.Clone();
            return xce;
        }
#if UNITY_EDITOR
        [Button]
        void SelectCmdCode()
        {
            string path = $"Assets/XiaoCaoKit/Runtime/Modules/XCSkillEditor/Runtime/XCCommand/{commandName}.cs";
            UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }
#endif
    }
}
