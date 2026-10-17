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
            GameEvent.AddEventListener<int, string>(EGameEvent.EnemyGroupEndEvent.ToInt(), OnEnemyGroupEndEvent);
        }


        public override void RemoveListener()
        {
            base.RemoveListener();
            if (isGameStarted)
            {
                GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
                GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
                GameEvent.RemoveEventListener<int, string>(EGameEvent.EnemyGroupEndEvent.ToInt(), OnEnemyGroupEndEvent);
            }
        }

        private void OnEnemyGroupEndEvent(int count, string msg)
        {
            //获取配置
            LevelData levelData = LevelData.Current;
            int coin = LevelSettingHelper.GetEnemyCoin(levelData.LevelName);
            int allCoin = coin * count;
            GameAllData.playerSaveData.RewardCoin(allCoin);
        }

        private void SetEnemy()
        {
            Transform actionTf = transform.Find(MapMgr.LevelActionNodeName);
            if (!actionTf)
            {
                actionTf = transform.GetChild(0);
                Debug.Log("---  no LevelAction");
            }

            SetEnemyRecursion(actionTf);
        }

        /// <summary>
        /// 递归查找Group开头的节点并初始化敌人配置
        /// LevelAction预制体加载后会多一层根节点, 所以需要递归而不是只看直接子节点
        /// </summary>
        private void SetEnemyRecursion(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith(EnemyGroupComponent.GroupPrefix))
                {
                    var key = LevelData.GetLevelEnemyInfoKey(child.name);
                    child.GetComponentsInChildren<EnemyGroupComponent>(true).ForEach(x =>
                    {
                        x.GetCreateEnemyInfoFromConfig(key);
                    });
                    continue;
                }

                SetEnemyRecursion(child);
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
            //优先使用LevelAction配置上的起始点覆盖
            Transform overridTf = GetStartPointOverrid();
            if (overridTf)
            {
                return overridTf.position;
            }

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
            //优先使用LevelAction配置上的结束点覆盖
            Transform overridTf = GetEndPointOverrid();
            if (overridTf)
            {
                return overridTf.position;
            }

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

        /// <summary>
        /// 获取LevelAction配置上的起终点覆盖组件, 与LevelAction配置同节点
        /// 只缓存找到的结果, 避免LevelAction还未加载时把空结果缓存下来
        /// </summary>
        private LevelActionChecker _actionChecker;

        private LevelActionChecker GetActionChecker()
        {
            if (_actionChecker)
            {
                return _actionChecker;
            }

            Transform actionTf = transform.Find(MapMgr.LevelActionNodeName);
            if (actionTf)
            {
                //LevelAction节点下可能有多个LevelAction_xxx, 只取当前激活的那个的配置
                for (int i = 0; i < actionTf.childCount; i++)
                {
                    Transform child = actionTf.GetChild(i);
                    if (!child.name.StartsWith(MapMgr.LevelActionPrefix) || !child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    _actionChecker = child.GetComponentInChildren<LevelActionChecker>(true);
                    if (_actionChecker)
                    {
                        return _actionChecker;
                    }
                }

                //节点自身或未激活子物体上的配置兜底
                _actionChecker = actionTf.GetComponentInChildren<LevelActionChecker>(true);
                if (_actionChecker)
                {
                    return _actionChecker;
                }
            }

            _actionChecker = GetComponentInChildren<LevelActionChecker>(true);
            return _actionChecker;
        }

        private Transform GetStartPointOverrid()
        {
            LevelActionChecker checker = GetActionChecker();
            return checker ? checker.startPointOverrid : null;
        }

        private Transform GetEndPointOverrid()
        {
            LevelActionChecker checker = GetActionChecker();
            return checker ? checker.endPointOverrid : null;
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