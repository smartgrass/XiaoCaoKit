﻿using System;
using System.Collections.Generic;
using TEngine;

namespace XiaoCao
{
    public class EnemyMaker : Singleton<EnemyMaker>, IClearCache
    {
        public Dictionary<int, Type> enemyTypeDic = new Dictionary<int, Type>();

        public Dictionary<string, Enemy0> enmeyGroup = new Dictionary<string, Enemy0>(); 
        
        protected override void Init()
        {
            base.Init();
            Add(1, typeof(Enemy0));
        }

        public void Add(int enemyId, Type type)
        {
            enemyTypeDic[enemyId] = type;
        }

        public Enemy0 CreatEnemy(int enemyId,int level = 1)
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
            enemy.Init(enemyId, level);
            enemy.IsAiOn = true;
            return enemy;
        }


    }

    public class EnmeyGroup:IDisposable
    {
        public List<Enemy0> list;

        public EnmeyGroup() {
            GameEvent.AddEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }

        public void Dispose()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }
        private void OnEntityChange(int arg1, RoleChangeType type)
        {
            
        }

        public  void Add(Enemy0 e)
        {
            list.Add(e);

        }

        public void OnEnemyDead()
        {

        }


    }

}