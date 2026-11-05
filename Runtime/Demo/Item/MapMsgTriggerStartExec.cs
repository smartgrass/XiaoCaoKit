using TEngine;
using XiaoCao;

public class MapMsgTriggerStartExec : GameStartMono, IExecute, IMapMsgSender
{
    public bool isExecuteOnStart = false;

    public string msg;

    public string GetSendMapMsg => msg;

    //MonoExecute
    public override void OnGameStart()
    {
        base.OnGameStart();
        if (isExecuteOnStart)
        {
            Execute();
        }
    }

    public virtual void Execute()
    {
        SendMapMsg();
    }

    public void SendMapMsg()
    {
        if (!string.IsNullOrEmpty(msg))
        {
            GameEvent.Send<string>(EGameEvent.MapMsg.ToInt(), msg);
        }
    }
}
