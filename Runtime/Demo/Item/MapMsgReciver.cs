using TEngine;
using UnityEngine.Events;
using XiaoCao.Buff;
using XiaoCao;

public class MapMsgReciver : MonoExecute, IMapMsgReciver
{
    public string msg;

    public UnityEvent reciverEvent;

    private void Start()
    {
        GameEvent.AddEventListener<string>(EGameEvent.MapMsg.Int(), OnReciveMsg);
    }

    private void OnDestroy()
    {
        GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.Int(), OnReciveMsg);
    }

    public void OnReciveMsg(string msg)
    {
        if (msg == "")
        {
            return;
        }

        if (this.msg == msg)
        {
            Execute();
        }
    }

    public override void Execute()
    {
        reciverEvent.Invoke();
    }
}
interface IMapMsgReciver {
    void OnReciveMsg(string msg);
}

interface IMapMsgSender { }