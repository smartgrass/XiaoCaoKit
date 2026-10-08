using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

namespace FluxEditor
{
    /// <summary>
    /// 单条指令模版:模版名、描述以及选中后要写入 FCommandEvent 的 BaseMsg 数据
    /// </summary>
    [Serializable]
    public class XCCommandTemplate
    {
        public string templateName;
        [TextArea(1, 4)]
        public string description;
        public BaseMsg baseMsg;
    }

    /// <summary>
    /// 指令 otherMsgs 的预设值,按名字一键写入
    /// </summary>
    [Serializable]
    public class XCCommandOtherMsgPreset
    {
        public string presetName;
        public List<string> values = new List<string>();
    }

    /// <summary>
    /// 按指令名分组的指令模版配置(编辑器专用)
    /// </summary>
    [CreateAssetMenu(fileName = "XCCommandTemplateConfig", menuName = "XiaoCao/指令模版配置")]
    public class XCCommandTemplateConfig : ScriptableObject
    {
        /// <summary>
        /// 一条指令的模版与 otherMsgs 预设
        /// </summary>
        [Serializable]
        public class CommandGroup
        {
            public string commandName;
            public List<XCCommandTemplate> templates = new List<XCCommandTemplate>();

            public List<XCCommandOtherMsgPreset> otherMsgPresets = new List<XCCommandOtherMsgPreset>();
        }

        public List<CommandGroup> groups = new List<CommandGroup>();

        /// <summary>
        /// 所有分组下的模版总数,用于 Inspector 判断配置数据是否已经读到
        /// </summary>
        public int TotalTemplateCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < groups.Count; i++)
                {
                    count += groups[i].templates.Count;
                }

                return count;
            }
        }

        /// <summary>
        /// 取指定指令名的配置,没有配置时返回 null
        /// </summary>
        public CommandGroup GetGroup(string commandName)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (string.Equals(groups[i].commandName, commandName, StringComparison.Ordinal))
                {
                    return groups[i];
                }
            }

            return null;
        }
    }
}
