using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    // player专属的设置
    public class PlayerSettingSo : NumMapSo<PlayerSetting>
    {
    }


    [Serializable]
    public class PlayerSetting : IIndex
    {
        public int id;
        public int Id => id;

        public int norAtkCount = 3;
        //平a重置时间
        public float resetNorAckTime = 1.5f;

        public float[] norAckCdTimes = { 0.5f, 0.5f, 0.5f, 0.5f };

        public float JumpY = 2.5f;

        public float JumpNoGravityT = 0.4f;

        [ReadOnly] //通过配置读取
        public List<string> skillIdList = new List<string>();
        
        //默认技能
        public List<string> defaultSkillList = new List<string>();

        public string rollSkillId;

        public string jumpSkillId;
    }

    [Serializable]
    public class AttrSetting : IIndex
    {
        public string desc = "备注";
        public int id;
        public int Id => id;
        
        public AttrSettingTag settingTag;
        public int maxLevel = 50;

        [FormerlySerializedAs("baseHp")] 
        public int addHp = 100;
        
        //倍率微调
        public float atkMult = 1;
        public float defMult = 1;
        public float hpMult = 1;

        public float maxArmor = 4;

        public float armorDef = 0.8f; //霸体减免,boss持有,克制多段伤害

        public float recoverCdOnBreak = 4; //进入Break后多久触发恢复/眩晕时间

        public float recoverCdIfOnHurt = 4; //角色未受伤后多久触发恢复

        public float actionRecover = 0.1f; //攻击时恢复
        
        [XCLabel("不受击时恢复速度")]
        public float noHurtRecoverSpeed = 0.2f; //不受击时恢复速度

        [Header("最短受击动画间隔, boss偏高")]
        public float hitAnimSpan = 0.1f; //受击动画间隔
        [Header("处于Break状态时是否霸体")]
        public bool isNoOnBreakState;
    }

    public enum AttrSettingTag
    {
        None,
        Boss
    }


    public class ArrayDic<T> where T : IIndex
    {
        public Dictionary<int, T> map;

        public T[] array;

        public bool hasInited;

        public void InitMap(T[] intiArray)
        {
            hasInited = true;
            map = ArrayToMap(intiArray);
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
