using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class HomeNpcTriggerBase : HomeTriggerBase
    {
        public float talkOffsetY = 0.5f;

        [Label("对话Cmd配置")]
        public HomeNpcCmdSo cmdSo;

        //当前cmd的播放协程, 为空表示没有cmd在执行
        private Coroutine cmdRoutine;

        //默认随机
        public override void TriggerSucceed()
        {
            //获取随机对话
            string talkId = GetRoleTalk(uiKey);
            string talk = talkId.ToLocalizeStr();
            if (string.IsNullOrEmpty(talk))
            {
                return;
            }

            //对话气泡
            UIMgr.Inst.battleHud.PopTip(talk, transform, offsetY + talkOffsetY);

            //读取对话id, 播放对话音效并执行对应的cmd动作
            PlayTalkCmd(talkId);
        }

        /// <summary>
        /// 按对话id读取配置: 播放对话音效(未配置则跳过), 并执行对应的cmd动作
        /// </summary>
        void PlayTalkCmd(string talkId)
        {
            if (cmdSo == null)
            {
                return;
            }

            HomeTalkCmdEntry talkEntry = cmdSo.GetTalkEntry(talkId, uiKey);
            if (talkEntry == null)
            {
                return;
            }

            //对话音效, 未配置则跳过
            PlayTalkAudio(talkEntry.audioId);

            PlayCmd(cmdSo.GetCmd(talkEntry));
        }

        /// <summary>
        /// 播放对话音效: 配置如 "Talk/曼波", 对应 Assets/_Res/Audio/Talk/曼波.mp3
        /// </summary>
        void PlayTalkAudio(string audioId)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                return;
            }

            string path = $"{XCPathConfig.AudioClipDir}/{audioId}.mp3";
            SoundMgr.Inst.PlayClip(ResMgr.LoadAseet(path) as AudioClip);
        }

        /// <summary>
        /// 执行cmd动作;
        /// 若当前cmd还没执行完, 不打断当前动作, 也不再保存这期间的新命令
        /// </summary>
        void PlayCmd(HomeNpcCmdEntry cmd)
        {
            if (cmd == null)
            {
                return;
            }

            if (cmdRoutine != null)
            {
                return;
            }

            cmdRoutine = StartCoroutine(IEPlayCmd(cmd));
        }

        /// <summary>
        /// 播放cmd: 按时间队列依次触发Animator的Trigger, 结尾延迟期间同样不能被新命令打断
        /// </summary>
        IEnumerator IEPlayCmd(HomeNpcCmdEntry cmd)
        {
            Animator animator = GetBodyAnimator();

            //按时间队列触发Trigger, 触发期间不打断当前动作
            float elapsed = 0f;
            foreach (CmdTriggerEvent triggerEvent in cmd.GetSortedEvents())
            {
                yield return new WaitForSeconds(Mathf.Max(0f, triggerEvent.time - elapsed));
                elapsed = triggerEvent.time;
                animator.SetTrigger(triggerEvent.triggerName);
            }

            //结尾延迟: 这段时间内cmd仍处于执行中
            yield return new WaitForSeconds(Mathf.Max(0f, cmd.Duration - elapsed));
            cmdRoutine = null;
        }

        /// <summary>
        /// 获取NPC body上的Animator(Npc会重建body, 所以每次重新查找)
        /// </summary>
        Animator GetBodyAnimator()
        {
            return GetComponentInChildren<Animator>(true);
        }

        public static string GetRoleTalk(string key)
        {
            if (TalkDic.TryGetValue(key, out List<string> talkList))
            {
                return talkList.GetRandom();
            }

            return "";
        }

        public static Dictionary<string, List<string>> TalkDic = new Dictionary<string, List<string>>()
        {
            {
                "Role_1", new List<string>()
                {
                    "Talk_Role_1_0",
                    "Talk_Role_1_1",
                    "Talk_Role_1_2",
                } 
                
            }
        };
    }
}
