using UnityEngine;
using XiaoCao;

public class ItemDeadRewardExec : MonoExecute
{
    public string rewardPoolId = "0";

    [XCHeader("奖励等级: -1时为关卡配置")]
    public int rewardLevel = -1;

    public bool noGetEffect;

    public Vector3 effectOffset = Vector3.zero;

    public override void Execute()
    {
        if (transform.TryGetComponent<ItemIdComponent>(out ItemIdComponent item))
        {
            int killerId = item.deadInfo.killerId;
            //if (killerId.IsLocalPlayerId())
            {
                if (rewardLevel < 0)
                {
                    rewardLevel = BattleData.Current.levelRewardData.RewardLevel;
                }
                RewardHelper.RewardBuffFromPool(killerId, rewardPoolId, rewardLevel);
            }
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
