using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

namespace FluxEditor
{
    /// <summary>
    /// 单条消息模版:模版名、描述,以及选中后要写入 XCEventMsg 的 BaseMsg 数据
    /// </summary>
    [Serializable]
    public class XCEventMsgTemplate
    {
        public string templateName;
        [TextArea(1, 4)]
        public string description;
        public BaseMsg baseMsg;
        [Tooltip("勾选后,应用模版时会把消息的 isUndoOnFinish 一并勾上;不勾选则保留消息原本的设置")]
        public bool needIsUndoOnFinish;
    }

    /// <summary>
    /// XCEventMsg 模版库配置(编辑器专用),按 EntityMsgType 分组存放模版
    /// </summary>
    [CreateAssetMenu(fileName = "XCEventMsgTemplateConfig", menuName = "XiaoCao/消息模版配置")]
    public class XCEventMsgTemplateConfig : ScriptableObject
    {
        /// <summary>
        /// 一种 EntityMsgType 下配置的全部模版
        /// </summary>
        [Serializable]
        public class TemplateGroup
        {
            public EntityMsgType msgType;
            public List<XCEventMsgTemplate> templates = new List<XCEventMsgTemplate>();
        }

        public List<TemplateGroup> groups = new List<TemplateGroup>();

        private static readonly List<XCEventMsgTemplate> EmptyTemplates = new List<XCEventMsgTemplate>();

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
        /// 取出指定消息类型下配置的模版列表,没有配置时返回空列表
        /// </summary>
        public List<XCEventMsgTemplate> GetTemplates(EntityMsgType msgType)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].msgType == msgType)
                {
                    return groups[i].templates;
                }
            }

            return EmptyTemplates;
        }
    }
}
