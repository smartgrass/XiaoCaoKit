using System;
using System.Collections.Generic;

namespace XiaoCao
{
    public class EntityMgr : MonoSingleton<EntityMgr>
    {
        private Dictionary<int, Entity> entityDic = new();
        private Dictionary<int, BehaviorEntity> behaviorEntityDic = new();

        public T CreatEntity<T>() where T : Entity
        {
            int id = IdMgr.GenId();
            var type = typeof(T);


            var entity = Activator.CreateInstance(type) as T;
            entity.id = id;
            AddEntity(entity);
            return entity;
        }

        void AddEntity(Entity entity)
        {
            int id = entity.id;
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
                }
                else
                {
                    behaviorEntityDic.Remove(item.Key);
                    Debuger.Error($"remove behaviorEntity {item.Key}");
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