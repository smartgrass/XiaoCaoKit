using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class ItemDeadRewardExec : MonoExecute
{
    public bool usePool;

    [HideIf(nameof(usePool))]
    public Item item;
    

    [ShowIf(nameof(usePool))]
    [Tooltip("奖池配置->RewardPoolSo")]
    public string rewardPoolId = "0";

    //[XCHeader("奖励等级: -1时为关卡配置,等级决定稀有率")]
    //public int rewardLevel = -1;

    public bool noGetEffect;

    public Vector3 effectOffset = Vector3.zero;

    public override void Execute()
    {
        if (transform.TryGetComponent<ItemIdComponent>(out ItemIdComponent itemCom))
        {
            int killerId = itemCom.deadInfo.killerId;

            if (usePool)
            {
                item = RewardHelper.GetItemWithPool(rewardPoolId);
            }
            RewardHelper.RewardItem(item);
        }

        if (!noGetEffect)
        {
            ShowEffect();
        }
    }
    void ShowEffect()
    {
        GetItemEffectHelper.GetItem(transform.position + effectOffset);
    }
}

public class GetItemEffectHelper
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
