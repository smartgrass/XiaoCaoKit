using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    //Key Map
    public abstract class KeyMapSo<T> : ScriptableObject where T : IKey
    {
        [Label("描述")] public string Des;

        public Dictionary<string, T> map;

        public T[] array;

        private bool _inited = false;

        private void OnEnable()
        {
#if UNITY_EDITOR
            _inited = false;
#endif
        }

        private void Check()
        {
            if (!_inited)
            {
                InitMap(array);
            }
        }

        public void InitMap(T[] array)
        {
            _inited = true;
            map = ArrayToMap(array);
        }

        public bool ContainsKey(string key)
        {
            Check();
            return map.ContainsKey(key);
        }


        public T GetOrDefault(string key)
        {
            Check();

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

        private static Dictionary<string, T2> ArrayToMap<T2>(T2[] array) where T2 : IKey
        {
            Dictionary<string, T2> dic = new Dictionary<string, T2>();
            foreach (var item in array)
            {
                dic[item.Key] = item;
            }

            return dic;
        }
    }


    public class StringArrayDic<T> where T : IKey
    {
        public Dictionary<string, T> map;

        public T[] array;

        private bool _inited = false;

        private void InitMap(T[] array)
        {
            _inited = true;
            map = ArrayToMap(array);
        }

        public T GetOrFirst(string key)
        {
            if (!_inited)
            {
                InitMap(array);
            }

            if (map.TryGetValue(key, out var result))
            {
                return result;
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

        private static Dictionary<string, T2> ArrayToMap<T2>(T2[] array) where T2 : IKey
        {
            Dictionary<string, T2> dic = new Dictionary<string, T2>();
            foreach (var item in array)
            {
                dic[item.Key] = item;
            }

            return dic;
        }
    }

    public interface IKey
    {
        public string Key { get; }
    }
}