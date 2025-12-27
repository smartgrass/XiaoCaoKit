using TEngine;
using XiaoCao;

public class MapMsgTriggerExec : MonoExecute, IMapMsgSender
{
    public string msg;
    public override void Execute()
    {
        if (!string.IsNullOrEmpty(msg))
        {
            GameEvent.Send<string>(EGameEvent.MapMsg.ToInt(), msg);
        }
    }
}
