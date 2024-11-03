using UnityEngine;
using XiaoCao;

public class TimeStopExecute : MonoExecute
{
    public float time = 5;

    public override void Execute()
    {
        TimeStopMgr.Inst.StopTimeSpeed(time);
    }
}
