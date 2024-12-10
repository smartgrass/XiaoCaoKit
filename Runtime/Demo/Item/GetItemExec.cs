using UnityEngine;
using XiaoCao;

public class GetItemExec : MonoExecute
{
    public Vector3 startOffset = Vector3.zero;
    public override void Execute()
    {
        GetItemHelper.GetItem(transform.position + startOffset);
    }
}


public class GetItemHelper
{
    public static void GetItem(Vector3 pos)
    {
        GameObject trailGo = PoolMgr.Inst.Get("Assets/_Res/Item/SoulTrail.prefab");
        var trail = trailGo.GetComponent<SoulTrailTween>();
        trailGo.transform.position = pos;
        trail.startPoint = trailGo.transform.position;
        trail.targetTf = GameDataCommon.GetPlayer().transform;
        trail.Play();
    }
}