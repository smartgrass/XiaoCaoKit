using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]

    /// <see cref="RaceIdSetting"/>
    /// <see cref="PlayerSkillSetting"/>
    /// player专属的设置
    public class PlayerSettingSo : SettingSo<PlayerSetting>
    {
        public override void OnInit()
        {
            base.OnInit();
            foreach (var item in array)
            {
                item.OnInit();
            }
        }
    }


    [Serializable]
    public class PlayerSetting : ArrayDic<PlayerSkillSetting>, IIndex
    {
        public int id = 0;
        public int Id => id;

        public int norAtkCount = 3;
        //平a重置时间
        public float resetNorAckTime = 1.5f;


        //TODO 序号和id
        public PlayerSkillSetting GetSkillSetting(int skillIndex)
        {
            //InitMap(playerSkillSettingArray);
            return GetOrFrist(skillIndex);
        }
    }

    public class ArrayDic<T> where T : IIndex
    {
        public Dictionary<int, T> map;

        public T[] array;

        public bool hasInited = false;

        public void InitMap(T[] array)
        {
            hasInited = true;
            map = ArrayToMap(array);
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
                throw new Exception();
            }
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

        public void OnInit()
        {
            hasInited = false;
        }
    }


    [Serializable]
    public class PlayerSkillSetting : IIndex
    {
        public int id;

        public float cd;

        public Sprite sprite;

        public int Id => id;
    }


    public static class ArrayExtend
    {
        public static T GetOrFrist<T>(this T[] array, int index)
        {
            if (array.Length > index)
            {
                return array[index];
            }
#if UNITY_EDITOR
            if (index == 0)
            {
                Debug.LogError("--- creat one ");
                array = new T[1];
                array[1] = default(T);
            }
#endif
            return array[0];
        }

        public static Dictionary<int, T> ArrayToMap<T>(T[] array) where T : IIndex
        {
            Dictionary<int, T> dic = new Dictionary<int, T>();
            foreach (var item in array)
            {
                dic[item.Id] = item;
            }
            return dic;
        }
    }
}
