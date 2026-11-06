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

        [Tooltip("玩家设置开关的 key(如 AutoChase); 留空表示该指令始终生效, 填了则开关关闭时这条指令不执行")]
        public string disableTag;

        public BaseMsg baseMsg;

        public string[] otherMsgs;

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
            if (commandName == null)
            {
                commandName = string.Empty;
            }

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
                //旧数据里 commandName 可能没写过(序列化后为 null), 编辑器刷新事件标签时不能抛异常
                if (string.IsNullOrEmpty(commandName))
                {
                    return string.Empty;
                }

                return commandName.StartsWith("XCCommand_")
                    ? commandName.Replace("XCCommand_", "")
                    : commandName;
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
            xce.disableTag = disableTag;
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
