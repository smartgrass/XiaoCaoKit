using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    /// <summary>
    /// HomeNPC 对话动作配置: 对话id → 对话音效 + cmdId,
    /// 每个 cmd 再配置 Animator Trigger 的时间队列与结尾延迟
    /// </summary>
    [CreateAssetMenu(fileName = "HomeNpcCmdSo", menuName = "SO/HomeNpcCmdSo")]
    public class HomeNpcCmdSo : KeyMapSo<HomeTalkCmdEntry>
    {
        [Label("Cmd配置")]
        public HomeNpcCmdEntry[] cmdArray = new HomeNpcCmdEntry[0];

        /// <summary>
        /// 根据对话id查找对话配置; 先按对话id匹配, 没配对话id时退回按角色key匹配,
        /// 都没有配置则返回null(表示该对话不做任何处理)
        /// </summary>
        public HomeTalkCmdEntry GetTalkEntry(string talkId, string roleKey)
        {
            return FindTalkEntry(talkId) ?? FindTalkEntry(roleKey);
        }

        /// <summary>
        /// 查找对话配置对应的cmd; 没配cmdId或找不到cmd时返回null(表示该对话不播放动作)
        /// </summary>
        public HomeNpcCmdEntry GetCmd(HomeTalkCmdEntry talkEntry)
        {
            if (talkEntry == null || string.IsNullOrEmpty(talkEntry.cmdId))
            {
                return null;
            }

            return FindCmdEntry(talkEntry.cmdId);
        }

        private HomeTalkCmdEntry FindTalkEntry(string talkId)
        {
            if (string.IsNullOrEmpty(talkId) || !ContainsKey(talkId))
            {
                return null;
            }

            return map[talkId];
        }

        private HomeNpcCmdEntry FindCmdEntry(string cmdId)
        {
            foreach (HomeNpcCmdEntry cmd in cmdArray)
            {
                if (cmd != null && cmd.cmdId == cmdId)
                {
                    return cmd;
                }
            }

            Debug.LogError($"HomeNpcCmdSo 找不到cmdId: {cmdId}");
            return null;
        }
    }


    /// <summary>
    /// 对话与cmd的映射: 一个对话id对应一个cmdId
    /// </summary>
    [Serializable]
    public class HomeTalkCmdEntry : IKey
    {
        [Label("对话id")] public string talkId; //如 Talk_Role_1_0, 也可直接填角色key
        [Label("对话音效")] public string audioId; //如 Talk/曼波, 对应 Assets/_Res/Audio/Talk/曼波.mp3
        [Label("Cmd id")] public string cmdId;

        public string Key => talkId;
    }


    /// <summary>
    /// cmd 子配置: 配置Animator Trigger的时间队列, 以及结尾延迟时长
    /// </summary>
    [Serializable]
    public class HomeNpcCmdEntry : IKey
    {
        [Label("Cmd id")] public string cmdId;
        [Label("描述")] public string des;

        [Label("Trigger时间队列")]
        public List<CmdTriggerEvent> triggerQueue = new List<CmdTriggerEvent>();

        [Label("结尾延迟(秒)")] public float delayEnd;

        public string Key => cmdId;

        /// <summary>
        /// cmd 总时长: 最后一个 Trigger 时间 + 结尾延迟;
        /// 这段时间内cmd处于执行中, 不会被新的命令打断
        /// </summary>
        public float Duration
        {
            get
            {
                float triggerLen = 0f;
                foreach (CmdTriggerEvent item in triggerQueue)
                {
                    if (item.time > triggerLen)
                    {
                        triggerLen = item.time;
                    }
                }

                return triggerLen + Mathf.Max(0f, delayEnd);
            }
        }

        /// <summary>
        /// 按时间升序返回 Trigger 队列, 供播放时依次触发
        /// </summary>
        public List<CmdTriggerEvent> GetSortedEvents()
        {
            List<CmdTriggerEvent> list = new List<CmdTriggerEvent>(triggerQueue);
            list.Sort((a, b) => a.time.CompareTo(b.time));
            return list;
        }
    }


    /// <summary>
    /// 单个 Trigger 事件: cmd 开始 time 秒后触发 animator 的 triggerName
    /// </summary>
    [Serializable]
    public class CmdTriggerEvent
    {
        [Label("Trigger名")] public string triggerName;
        [Label("触发时间(秒)")] public float time;
    }
}
