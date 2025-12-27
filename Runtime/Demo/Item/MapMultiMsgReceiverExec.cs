using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;
using XiaoCao.UI;

/// <summary>
/// 与门逻辑
/// </summary>
public class MapMultiMsgReceiverExec : MonoExecute, IMapMsgReceiver, IMapMsgSender
{
    [Tooltip("全部接收后发送sendMsg")] public List<string> msgList = new List<string>() { "Group1" };

    public string sendMsg;
    
    public UnityEvent receiverEvent;

    public MonoExecute nextExecute;

    public string popToastKey;
    public void OnReceiveMsg(string receiveMsg)
    {
        if (receiveMsg == "")
        {
            return;
        }

        if (msgList.Remove(receiveMsg))
        {
            if (msgList.Count == 0)
            {
                if (!string.IsNullOrEmpty(receiveMsg))
                {
                    GameEvent.Send<string>(EGameEvent.MapMsg.ToInt(), sendMsg);
                }

                Execute();
            }
        }
    }


    private void Start()
    {
        GameEvent.AddEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
    }

    private void OnDestroy()
    {
        GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
    }

    public override void Execute()
    {
        nextExecute?.Execute();
        receiverEvent.Invoke();
        UIMgr.PopToastKey(popToastKey);
    }
}


interface IMapMsgReceiver
{
    void OnReceiveMsg(string receiveMsg);
}

interface IMapMsgSender { }