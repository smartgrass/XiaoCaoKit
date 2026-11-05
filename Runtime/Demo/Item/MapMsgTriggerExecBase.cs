using TEngine;
using XiaoCao;

/// <summary>
/// 只继承 不使用
/// </summary>
public abstract class MapMsgTriggerExecBase : MonoExecute, IExecute, IMapMsgSender
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
