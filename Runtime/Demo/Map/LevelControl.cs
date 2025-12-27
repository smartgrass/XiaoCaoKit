using NaughtyAttributes;
using OdinSerializer.Utilities;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    //TODO没啥用,等着把逻辑抽离mono
    public class LevelControl : GameStartMono, IMapMsgReceiver
    {
        public Transform startPoint;

        public Transform endPoint;
        //[HideInInspector]
        //public RewardPoolSo enemyKillRewardSo;

        //TODO 全都走luban配置得了
        public string[] rewardPools = { "0", "1", "2" };

        public LevelData LevelData => BattleData.Current.levelData;

        public override void OnGameStart()
        {
            base.OnGameStart();

            GameMgr.Inst.levelControl = this;

            SetEnemy();

            //enemyKillRewardSo = ConfigMgr.enemyKillRewardSo;

            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
        }

        public override void RemoveListener()
        {
            base.RemoveListener();
            if (isGameStarted)
            {
                GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
                GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
            }
        }

        private void SetEnemy()
        {
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
                    child.GetComponentsInChildren<EnemyGroupComponent>(true).ForEach(x =>
                    {
                        x.GetCreateEnemyInfoFromConfig(key);
                    });
                }
            }
        }


        void OnEnemyDeadEvent(int id)
        {
            Debug.Log($"--- {id}");
            LevelData.Current.killCount++;
            //TODO 击杀暂无奖励
            return;
            if (EntityMgr.Inst.FindEntity<Enemy0>(id, out Enemy0 enemy))
            {
                var deadInfo = enemy.enemyData.deadInfo;
                // 关卡奖励分等级 && 关卡等级
                if (deadInfo.killerId.IsLocalPlayerId())
                {
                    //获取奖励等级
                    int rewardLevel = enemy.enemyData.rewardLevel;

                    /*
                    //获取奖池id
                    string rewardPoolId = rewardPools[Mathf.Min(rewardLevel, rewardPools.Length - 1)];

                    if (string.IsNullOrEmpty(rewardPoolId))
                    {
                        return;
                    }

                    Item item = RewardHelper.GetItemWithPool(rewardPoolId, rewardLevel);

                    RewardHelper.RewardItem(item);
                    */
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

        public Vector3 GetEndPos()
        {
            if (!endPoint)
            {
                endPoint = transform.Find("endPoint");
                if (endPoint)
                {
                    return endPoint.position;
                }

                if (EnemyGroupComponent.Current)
                {
                    return EnemyGroupComponent.Current.transform.position;
                }

                return GameDataCommon.LocalPlayer.transform.position;
            }
            else
            {
                return endPoint.position;
            }
        }


        [Button("引爆所有机关")]
        void TestTriggerAllLevelGroup()
        {
            var groups = transform.GetComponentsInChildren<ItemCrystal>();
            foreach (var group in groups)
            {
                if (!group.isDead)
                {
                    group.ToDead();
                }
            }
        }

        public void OnReceiveMsg(string receiveMsg)
        {
            if (receiveMsg == "BreakAll")
            {
                TestTriggerAllLevelGroup();
            }
        }
    }
}