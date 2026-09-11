using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class HomeNpcTriggerBase : HomeTriggerBase
    {
        public float talkOffsetY = 0.5f;
        //默认随机
        public override void TriggerSucceed()
        {
            //获取随机对话
            string talk = GetRoleTalk(uiKey).ToLocalizeStr();
            if (string.IsNullOrEmpty(talk))
            {
                return;
            }

            //对话气泡
            UIMgr.Inst.battleHud.PopTip(talk, transform, offsetY + talkOffsetY);
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
            { "Role_1", new List<string>() { "Talk_Role_1_0" } }
        };
    }
}
