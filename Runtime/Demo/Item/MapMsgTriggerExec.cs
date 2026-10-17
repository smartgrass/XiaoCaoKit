using TEngine;
using XiaoCao;

public class MapMsgTriggerExec : MonoExecute, IMapMsgSender
{
    public string msg;

    public string GetSendMapMsg => msg;

    public override void Execute()
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
