
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 与门逻辑
/// </summary>
public class MapMultiMsgReciverExec : MapMsgReciverExec, IMapMsgSender
{
    [Header("msgList全部接收后转发为msg")]
    public List<string> msgList = new List<string>();

    public override void OnReciveMsg(string reciveMsg)
    {
        if (reciveMsg == "")
        {
            return;
        }

        if (msgList.Remove(reciveMsg))
        {
            if (msgList.Count == 0)
            {
                if (!string.IsNullOrEmpty(reciveMsg))
                {
                    GameEvent.Send<string>(EGameEvent.MapMsg.Int(), msg);
                }
                Execute();
            }
        }
    }

}
