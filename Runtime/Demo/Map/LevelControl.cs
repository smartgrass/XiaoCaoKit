using TEngine;
using UnityEngine;

namespace XiaoCao
{
    //控制敌人生成, 地图生成, 敌人奖励等等
    public class LevelControl : GameStartMono
    {
        [HideInInspector]
        public EnemyKillRewardSo enemyKillRewardSo;


        public override void OnGameStart()
        {
            base.OnGameStart();
            enemyKillRewardSo = ConfigMgr.LoadSoConfig<EnemyKillRewardSo>();

            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
        }

        void OnEnemyDeadEvent(int id)
        {
            Debug.Log($"--- id");
            //赐予奖励
            if (EntityMgr.Inst.FindEntity<Enemy0>(id, out Enemy0 enemy))
            {
                var deadInfo = enemy.enemyData.deadInfo;
                 //deadInfo.killerId


            }
        }


        public override void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
            base.OnDestroy();
        }
    }
}
