using UnityEngine;
using XiaoCao;

public class ItemDeadRewardExec : MonoExecute
{
    public string rewardPoolId = "0";

    [XCHeader("奖励等级: -1时为关卡配置")]
    public int rewardLevel = -1;

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

    }
}
