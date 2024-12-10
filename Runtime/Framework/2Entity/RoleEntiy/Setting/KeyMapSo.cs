using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    //Key Map
    public abstract class KeyMapSo<T> : ScriptableObject where T : IKey
    {
        public Dictionary<string, T> map;

        public T[] array;

        [NonSerialized]
        public bool hasInited = false;
        private void OnEnable()
        {
            OnInit();
        }

        public void InitMap(T[] array)
        {
            hasInited = true;
            map = ArrayToMap(array);
        }

        public T GetOrDefault(string key)
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
                throw new Exception();
            }
        }

        public static Dictionary<string, T2> ArrayToMap<T2>(T2[] array) where T2 : IKey
        {
            Dictionary<string, T2> dic = new Dictionary<string, T2>();
            foreach (var item in array)
            {
                dic[item.Key] = item;
            }
            return dic;
        }

        public void OnInit()
        {
            hasInited = false;
        }
    }


    public class StringArrayDic<T> where T : IKey
    {
        public Dictionary<string, T> map;

        public T[] array;

        public bool hasInited = false;

        public void InitMap(T[] array)
        {
            hasInited = true;
            map = ArrayToMap(array);
        }

        public T GetOrFrist(string key)
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
                throw new Exception();
            }
        }

        public static Dictionary<string, T2> ArrayToMap<T2>(T2[] array) where T2 : IKey
        {
            Dictionary<string, T2> dic = new Dictionary<string, T2>();
            foreach (var item in array)
            {
                dic[item.Key] = item;
            }
            return dic;
        }

        public void OnInit()
        {
            hasInited = false;
        }
    }

    public interface IKey
    {
        public string Key { get; }
    }
}
