using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    public class PlayerSettingSo : SettingSo<PlayerSetting>
    {

    }


    [Serializable]
    public class PlayerSetting
    {
        public int norAtkCount = 3;
        //平a重置时间
        public float resetNorAckTime = 1.5f;

        public PlayerSkillSetting[] playerSkillSettingArray;

        

        //TODO cd
        public PlayerSkillSetting GetSkillCd(int index)
        {
            return playerSkillSettingArray.GetOrFrist(index);
        }
    }

    [Serializable]
    public class PlayerSkillSetting
    {
        public int id;

        public float cd;

        public Sprite sprite;
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
    }
}
