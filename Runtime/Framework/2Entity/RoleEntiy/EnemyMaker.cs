using System;
using System.Collections.Generic;
using TEngine;

namespace XiaoCao
{
    public class EnemyMaker : Singleton<EnemyMaker>, IClearCache
    {
        public Dictionary<string, Type> enemyTypeDic = new Dictionary<string, Type>();

        public Dictionary<string, Enemy0> enmeyGroup = new Dictionary<string, Enemy0>();

        protected override void Init()
        {
            base.Init();
            //Add(1, typeof(Enemy0));
        }

        public void Add(string enemyId, Type type)
        {
            enemyTypeDic[enemyId] = type;
        }

        public Enemy0 CreatEnemy(string enemyId, int level = 1, string skinNameSet = null)
        {
            Enemy0 enemy = null;
            if (enemyTypeDic.ContainsKey(enemyId))
            {
                Type enemyType = enemyTypeDic[enemyId];
                enemy = EntityMgr.Inst.CreatEntity(enemyType) as Enemy0;
            }
            else
            {
                enemy = EntityMgr.Inst.CreatEntity<Enemy0>();
            }

            enemy.Init(enemyId, level, skinNameSet);
            enemy.IsAiOn = true;
            return enemy;
        }
    }

    public class EnmeyGroup : IDisposable
    {
        public List<Enemy0> list;

        public EnmeyGroup()
        {
            GameEvent.AddEventListener<int, RoleChangeType>(EGameEvent.RoleChange.Int(), OnEntityChange);
        }

        public void Dispose()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EGameEvent.RoleChange.Int(), OnEntityChange);
        }

        private void OnEntityChange(int arg1, RoleChangeType type)
        {
        }

        public void Add(Enemy0 e)
        {
            list.Add(e);
        }

        public void OnEnemyDead()
        {
        }
    }
}