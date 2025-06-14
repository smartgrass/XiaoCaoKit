using OdinSerializer.Utilities;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    //控制敌人生成, 地图生成, 敌人奖励等等
    public class LevelControl : GameStartMono
    {
        public Transform startPoint;

        //[HideInInspector]
        //public RewardPoolSo enemyKillRewardSo;

        public string[] rewardPools = { "0", "1", "2" };

        public int rewardLevel;

        public LevelData LevelData => BattleData.Current.levelData;

        public override void OnGameStart()
        {
            base.OnGameStart();

            GameMgr.Inst.levelControl = this;

            LevelData.RewardLevel = rewardLevel;

            SetEnmeys();

            //enemyKillRewardSo = ConfigMgr.enemyKillRewardSo;

            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
        }

        private void SetEnmeys()
        {
            if (string.IsNullOrEmpty(LevelData.LevelBranch))
            {
                return;
            }

            Transform actionTf = transform.Find("LevelAction");
            if (!actionTf)
            {
                actionTf = transform.GetChild(0);
                Debug.Log("---  no LevelAction");
            }

            //遍历actionTf子物体, 查找Group开头
            for (int i = 0; i < actionTf.childCount; i++)
            {
                Transform child = actionTf.GetChild(i);
                if (child.name.StartsWith("Group"))
                {
                    var key = LevelData.GetLevelEnemyInfoKey(child.name);
                    child.GetComponentsInChildren<EnemeyGroupComponent>().ForEach(x =>
                    {
                        x.GetCreateEnemyInfoFromConfig(key);
                    });
                }
            }
        }

        public override void RemoveListener()
        {
            base.RemoveListener();
            if (isGameStarted)
            {
                GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
            }
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
                    string rewardPoolId = rewardPools[Mathf.Min(rewardLevel, rewardPools.Length - 1)];

                    if  (string.IsNullOrEmpty(rewardPoolId))
                    {
                        return;
                    }

                    Item item = RewardHelper.GetItemWithPool(rewardPoolId, rewardLevel);

                    RewardHelper.RewardItem(item);
                }
            }
            else
            {
                Debuger.LogError($"--- no enmey {id}");
            }
        }

        public Vector3 GetStartPos()
        {
            if (!startPoint)
            {
                startPoint = transform.Find("startPoint");
                return startPoint ? startPoint.position : Vector3.zero;
            }
            else
            {
                return startPoint.position;
            }

        }
    }
}
