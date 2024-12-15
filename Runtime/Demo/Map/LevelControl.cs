using TEngine;
using UnityEngine;

namespace XiaoCao
{
    //控制敌人生成, 地图生成, 敌人奖励等等
    public class LevelControl : GameStartMono
    {
        [HideInInspector]
        public EnemyKillRewardSo enemyKillRewardSo;

        public string[] rewardPools = { "0", "1", "2" };

        public int rewardLevel;

        public override void OnGameStart()
        {
            base.OnGameStart();

            GameMgr.Inst.levelControl = this;

            BattleData.Current.levelRewardData.RewardLevel = rewardLevel;

            enemyKillRewardSo = ConfigMgr.enemyKillRewardSo;

            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
        }


        void OnEnemyDeadEvent(int id)
        {
            Debug.Log($"--- {id}");
            if (EntityMgr.Inst.FindEntity<Enemy0>(id, out Enemy0 enemy))
            {
                var deadInfo = enemy.enemyData.deadInfo;
                // 关卡奖励分等级 && 关卡等级
                if (deadInfo.killerId.IsLocalPlayerId())
                {
                    //获取奖励等级
                    int rewardLevel = enemy.enemyData.rewardLevel;
                    //获取奖池id
                    string rewardPoolId = rewardPools[Mathf.Min(rewardLevel, rewardPools.Length)];

                    RewardHelper.RewardBuffFromPool(deadInfo.killerId, rewardPoolId, rewardLevel);
                }
            }
            else
            {
                Debuger.LogError($"--- no enmey {id}");
            }
        }


        public override void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
            base.OnDestroy();
        }
    }


}
