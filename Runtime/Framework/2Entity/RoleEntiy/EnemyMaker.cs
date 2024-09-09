using System;
using System.Collections.Generic;

namespace XiaoCao
{
    public class EnemyMaker : Singleton<EnemyMaker>, IClearCache
    {
        public Dictionary<int, Type> enemyTypeDic = new Dictionary<int, Type>();
        protected override void Init()
        {
            base.Init();
            Add(1, typeof(Enemy0));
        }

        public void Add(int enemyId, Type type)
        {
            enemyTypeDic[enemyId] = type;
        }

        public Enemy0 CreatEnemy(int enemyId)
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
            enemy.Init(enemyId);
            enemy.IsAiOn = true;
            return enemy;
        }

    }
}