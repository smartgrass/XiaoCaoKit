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
        public GameObject gameObject { get; set; }

        public EntityBaseType BindType { get => EntityBaseType.Entity; }

        #region Tag

        public HashSet<int> Tags = new();

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
        #endregion
    }


    public enum EntityBaseType
    {
        Entity,
        BehaviorEntity
    }

}