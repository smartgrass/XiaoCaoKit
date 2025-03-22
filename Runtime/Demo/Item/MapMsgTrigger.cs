using TEngine;
using XiaoCao;

public class MapMsgTrigger : MonoExecute, IMapMsgSender
{
    public string msg;
    public override void Execute()
    {
        if (!string.IsNullOrEmpty(msg))
        {
            GameEvent.Send<string>(EGameEvent.MapMsg.Int(), msg);
        }
    }
}
