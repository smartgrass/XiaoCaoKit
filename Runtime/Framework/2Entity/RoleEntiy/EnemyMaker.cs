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

        public void CreatEnemy(int enemyId)
        {
            if (enemyTypeDic.ContainsKey(enemyId))
            {
                Type enemyType = enemyTypeDic[enemyId];
                Enemy0 enemy = EntityMgr.Inst.CreatEntityByType(enemyType) as Enemy0;
                enemy.Init(enemyId);
                return;
            }
            else
            {
                Enemy0 enemy = EntityMgr.Inst.CreatEntity<Enemy0>();
                enemy.Init(enemyId);
            }
        }
    }
}