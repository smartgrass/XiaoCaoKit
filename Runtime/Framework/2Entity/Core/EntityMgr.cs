using Flux;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace XiaoCao
{
    public class EntityMgr : MonoSingleton<EntityMgr>, IMgr
    {
        private Dictionary<int, Entity> entityDic = new Dictionary<int, Entity>();
        public Dictionary<int, BehaviorEntity> behaviorEntityDic = new Dictionary<int, BehaviorEntity>();
        public override void Init()
        {
            base.Init();       
        }

        public T CreatEntity<T>() where T : Entity
        {
            var entity = CreatEntity(typeof(T));
            return entity as T;
        }

        public Entity CreatEntity(Type entityType)
        {
            int id = IdMgr.GenId();
            var entity = Activator.CreateInstance(entityType) as Entity;
            entity.id = id;
            AddEntity(entity);
            return entity;
        }

        /* 利用反射调用派生类方法
        public Entity CreatEntityByType(Type genType)
        {
            var creatMethod = GetType().GetMethod(nameof(CreatEntity), new Type[] { });
            MethodInfo genMethod = creatMethod.MakeGenericMethod(genType);
            object ret = genMethod.Invoke(EntityMgr.Inst, null);
            return ret as Entity;
        }
        */

        void AddEntity(Entity entity)
        {
            int id = entity.id;
            Debug.Log($"--- AddEntity {id}");
            if (entity.BindType == EntityBaseType.BehaviorEntity)
            {
                behaviorEntityDic[id] = entity as BehaviorEntity;
                behaviorEntityDic[id].OnCreat();
            }
            entityDic[id] = entity;
        }

        //指定销毁方法
        public void RemoveEntity(Entity entity)
        {
            if (entity == null)
                return;

            int id = entity.id;
            if (entity.BindType == EntityBaseType.BehaviorEntity)
            {
                behaviorEntityDic[id].DestroyEvent();
                behaviorEntityDic.Remove(id);
            }
            entityDic.Remove(id);

            entity.Dispose();
        }


        void Update()
        {
            foreach (var item in behaviorEntityDic)
            {
                var entity = item.Value;
                if (entity != null)
                {
                    if (entity.IsRuning)
                    {
                        entity.UpdateEvent?.Invoke();
                    }
                    else
                    {
                        DebugGUI.Log("no Runing", entity.id);
                    }
                }
                else
                {
                    behaviorEntityDic.Remove(item.Key);
                    Debuger.LogError($"remove behaviorEntity {item.Key}");
                }
            }
        }

        void FixedUpdate()
        {
            foreach (var item in behaviorEntityDic)
            {
                var entity = item.Value;
                if (entity != null)
                {
                    if (entity.IsRuning)
                    {
                        entity.FixedUpdateEvent?.Invoke();
                    }
                }
            }
        }

        private void LateUpdate()
        {
            foreach (var item in behaviorEntityDic)
            {
                var entity = item.Value;
                if (entity != null)
                {
                    if (entity.IsRuning)
                    {
                        entity.LaterUpdateEvent?.Invoke();
                    }
                }
            }
        }


        public bool FindEntity(int id, out Entity entity)
        {
            entity = null;
            if (entityDic.ContainsKey(id))
            {
                entity = entityDic[id];
                return true;
            }
            return false;
        }

        public bool FindEntity<T>(int id, out T entity) where T : Entity
        {
            entity = null;
            if (entityDic.ContainsKey(id))
            {
                var sourceEntity = entityDic[id];
                entity = sourceEntity as T;
                if (entity != null)
                {
                    return true;
                }
            }
            return false;
        }

    }
}