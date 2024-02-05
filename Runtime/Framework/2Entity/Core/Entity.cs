using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// Entity不是Behavior, 核心是id
    /// System也融合进来, 但可以有单纯的System
    /// 有接口, 做一层封装? date 可能也可以省略了
    /// </summary>
    public class Entity : IDisposable, ITags
    {
        //TODO 物理碰撞 OnTriggerEnter => 使用Mono类封装 . bindId
        public int id;
        public GameObject gameObject { get; private set; }
        public Transform transform { get; private set; }

        public virtual EntityBaseType BindType { get => EntityBaseType.Entity; }

        protected void BindGameObject(GameObject go)
        {
            gameObject = go;
            transform = gameObject.transform;
        }

        #region Tag

        public HashSet<int> Tags = new HashSet<int>();

        public void AddTag(int tag)
        {
            Tags.Add(tag);
        }

        public bool HasTag(int tag)
        {
            return Tags.Contains(tag);
        }

        public void RemoveTag(int tag)
        {
            Tags.Remove(tag);
        }

        public void Dispose()
        {
            
        }

        public virtual void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debug.Log($"--- Receive {type} fromId: {fromId}");

        }

        #endregion
    }


    public enum EntityBaseType
    {
        Entity,
        BehaviorEntity
    }

}