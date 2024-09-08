using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public abstract class SettingSo<T> : ScriptableObject where T : IIndex
    {
        public T[] array = new T[1];

        public Dictionary<int, T> map;

        [NonSerialized]
        public bool hasInited = false;
        private void OnEnable()
        {
            OnInit();
        }

        public virtual void OnInit()
        {
            //初始化数据
            hasInited = false;
        }

        public T GetOrFrist(int key)
        {
            if (!hasInited)
            {
                InitMap(array);
            }

            if (map.ContainsKey(key))
            {
                return map[key];
            }
            else if (array.Length > 0)
            {
                return array[0];
            }
            else
            {
                array = new T[1];
                Debug.LogError("--- creat one " + this.name);
                return array[0];
            }
        }

        public T GetOnArray(int arrayIndex)
        {
            if (array.Length > arrayIndex)
            {
                return array[arrayIndex];
            }

#if UNITY_EDITOR
            if (arrayIndex == 0)
            {
                Debug.LogError("--- creat one " + this.name);
                array = new T[1];
            }
#endif
            return array[0];
        }


        public void InitMap(T[] array)
        {
            hasInited = true;
            map = ArrayToMap(array);
        }

        public static Dictionary<int, T2> ArrayToMap<T2>(T2[] array) where T2 : IIndex
        {
            Dictionary<int, T2> dic = new Dictionary<int, T2>();
            foreach (var item in array)
            {
                dic[item.Id] = item;
            }
            return dic;
        }

    }

    public interface IIndex
    {
        public int Id { get; }
    }
}
