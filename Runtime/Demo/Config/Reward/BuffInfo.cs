using NUnit.Framework;
using OdinSerializer;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

namespace XiaoCao
{
    //词条Buff的最基础单位
    [Serializable]
    public struct BuffInfo : IPower
    {
        public int Power => power;

        public int power;

        public EBuff eBuff;

        public float[] addInfo;
    }
    /// <summary>
    /// Buff容器, 可以承载多个词条
    /// </summary>
    [Serializable]
    public struct BuffItem
    {
        public int level; //星级

        public List<BuffInfo> buffs;

        //取第一个为主属性
        public EBuffType GetBuffType
        {
            get
            {
                if (buffs == null || buffs.Count == 0)
                {
                    return EBuffType.None;
                }
                return buffs[0].eBuff.GetBuffType();
            }
        }
        public int GetMaxBuffCount => level + 1; // 0级1个词条, 1级两个词条


        public void UpGradeItem(BuffItem costItem)
        {
            buffs.Add(costItem.buffs[0]);
            level++;
            if (level > GameSetting.MaxBuffLevel)
            {
                Debug.LogError($"--- level > MaxBuffLevel {level} > {GameSetting.MaxBuffLevel} ");
            }
        }

        public void Clear()
        {
            level = -1;
            buffs = null;
        }

        //同级 & 非满级 -> 可合成
        public bool CanUpGradeItem(BuffItem buffItem)
        {
            if (level > GameSetting.MaxBuffLevel)
            {
                return false;
            }
            return level == buffItem.level;
        }

    }

    public static class BuffItemHelper
    {
        public static BuffItem GenRandomBuffItem(int level, List<BuffInfo> buffs)
        {
            BuffItem item = new BuffItem();
            item.level = level;
            item.buffs = RandomHelper.GetRandomList<BuffInfo>(buffs, level+1);

            return item;
        }

        public static List<BuffInfo> GetBuffInfos(this List<BuffItem> items)
        {
            List<BuffInfo> list = new List<BuffInfo>();
            foreach (var item in items)
            {
                if (item.GetBuffType != EBuffType.None)
                {
                    list.AddRange(item.buffs);
                }
                return list;
            }
            return list;
        }

        /// <summary>
        /// 合并同类buff列表
        /// </summary>
        /// <param name="buffList"></param>
        /// <returns></returns>
        public static List<BuffInfo> Combine(this List<BuffInfo> buffList)
        {
            Dictionary<EBuff, BuffInfo> dic = new Dictionary<EBuff, BuffInfo>();

            foreach (BuffInfo buff in buffList)
            {
                EBuff key = buff.eBuff;
                if (dic.ContainsKey(key))
                {
                    //合并同类buff
                    BuffInfo combineBuff = CombineBuffInfo(key, dic[key], buff);
                    dic[key] = combineBuff;
                }
                else
                {
                    dic[key] = buff;
                }
            }
            return new List<BuffInfo>(dic.Values);
        }


        //合并同类词条
        public static BuffInfo CombineBuffInfo(EBuff eBuff, BuffInfo buff, BuffInfo buff2)
        {
            if (buff.eBuff != eBuff || buff2.eBuff != eBuff)
            {
                Debuger.LogError($"--- buff type error {buff.eBuff} + {buff2.eBuff}");
            }

            //TODO 特殊buff处理
            if (eBuff.IsOtherBuff())
            {
                return new BuffInfo()
                {
                    eBuff = eBuff,
                    addInfo = CombineOtherAddInfo(eBuff, buff.addInfo, buff2.addInfo)
                };
            }


            return new BuffInfo()
            {
                eBuff = eBuff,
                addInfo = CombineAddInfo(buff.addInfo, buff2.addInfo)
            };
        }


        private static bool IsOtherBuff(this EBuff eBuff)
        {
            return false;
        }

        public static float[] CombineOtherAddInfo(EBuff eBuff, float[] array1, float[] array2)
        {
            return null;
        }

        private static float[] CombineAddInfo(float[] array1, float[] array2)
        {
            int len = array1.Length;
            float[] floats = new float[len];
            for (int i = 0; i < len; i++)
            {
                floats[i] = array1[i] + array2[i];
            }
            return floats;
        }

    }


}
