using TEngine;
using XiaoCao;

/// <summary>
/// 直接发送关卡完成信号
/// </summary>
public class MapMsgLevelFinish : GameStartMonoExec, IMapMsgSender
{
    public string GetSendMapMsg => "LevelFinish";

    public override void Execute()
    {
        SendMapMsg();
    }

    public void SendMapMsg()
    {
        GameEvent.Send<string>(EGameEvent.MapMsg.ToInt(), GetSendMapMsg);
    }
}
