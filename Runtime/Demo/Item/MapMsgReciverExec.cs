using TEngine;
using UnityEngine.Events;
using XiaoCao.Buff;
using XiaoCao;
using NaughtyAttributes;



public class MapMsgReciverExec : MonoExecute, IMapMsgReciver
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

    public virtual void OnReciveMsg(string reciveMsg)
    {
        if (reciveMsg == "")
        {
            return;
        }

        if (this.msg == reciveMsg)
        {
            Execute();
        }
    }

    public override void Execute()
    {
        reciverEvent.Invoke();
    }
}
interface IMapMsgReciver
{
    void OnReciveMsg(string reciveMsg);
}

interface IMapMsgTrigger { }