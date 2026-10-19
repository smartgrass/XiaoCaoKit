using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class ItemDeadRewardExec : MonoExecute
{
    public MonoExecute rewardExecute;
    //[XCHeader("奖励等级: -1时为关卡配置,等级决定稀有率")]
    //public int rewardLevel = -1;

    //rewardAfterEffect
    //特效播完再获得奖励
    public bool rewardAfterEffect;

    public Vector3 effectOffset = Vector3.zero;

    public override void Execute()
    {
        if (!rewardAfterEffect)
        {
            ShowEffectAndReward();
        }
        else
        {
            GetReward();
        }

    }

    private void GetReward()
    {
        if (transform.TryGetComponent<ItemIdComponent>(out ItemIdComponent itemCom))
        {
            int killerId = itemCom.deadInfo.killerId;
            rewardExecute.Execute();
        }
    }

    void ShowEffectAndReward()
    {
        GetItemEffectHelper.PlayRewardEffect(transform.position + effectOffset, GetReward);
    }
}

public class GetItemEffectHelper
{
    public static void PlayRewardEffect(Vector3 pos, Action effectEndAct)
    {
        GameObject trailGo = PoolMgr.Inst.Get("Assets/_Res/Item/SoulTrail.prefab");
        var trail = trailGo.GetComponent<SoulTrailTween>();
        trailGo.transform.position = pos;
        trail.startPoint = trailGo.transform.position;
        trail.targetTf = GameDataCommon.GetPlayer().transform;
        trail.Play();
        trail.rewardAct = effectEndAct;
    }
}
