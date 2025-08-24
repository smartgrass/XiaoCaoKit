using cfg;
using OdinSerializer;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using static Cinemachine.DocumentationSortingAttribute;

namespace XiaoCao
{
    /// <summary>
    /// Buff容器, 可以承载多个词条
    /// </summary>
    [Serializable]
    public class BuffItem : ISubItem
    {
        public int level; //等级,默认0,文本显示+1

        public List<BuffInfo> buffs;

        public List<BuffInfo> GetBuffs
        {
            get
            {
                if (buffs == null)
                {
                    buffs = new List<BuffInfo>();
                }
                return buffs;
            }
        }

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

        public EBuff GetFirstEBuff
        {
            get

            {
                if (buffs == null || buffs.Count == 0)
                {
                    return EBuff.None;
                }
                return buffs[0].eBuff;
            }
        }

        public bool IsEnable
        {
            get
            {
                return GetBuffType != EBuffType.None;
            }
        }

        public bool IsMaxLevel
        {
            get
            {
                return level >= ConfigMgr.BuffConfigSo.GetMaxLevel(GetFirstEBuff);
            }

        }
        public int GetExBuffMaxLevel
        {
            get => ConfigMgr.BuffConfigSo.GetMaxLevel(GetFirstEBuff);
        }

        /// <summary>
        /// 纹章合成
        /// </summary>
        /// <param name="costItem"></param>
        public void CombineItem(BuffItem costItem)
        {
            //ExBuff根据level读取配置
            if (costItem.GetBuffType == EBuffType.Ex)
            {
                level = (costItem.level + 1 + level);
                level = Math.Min(level, costItem.GetExBuffMaxLevel);
                buffs[0] = ConfigMgr.BuffConfigSo.GetLevelBuffInfo(costItem.buffs[0].eBuff, level);
            }
            else
            {
                var getBuffs = GetBuffs; //check null         
                for (int i = 0; i < buffs.Count; i++)
                {
                    if (buffs[i].eBuff == costItem.buffs[0].eBuff)
                    {
                        //合并同类buff
                        buffs[i] = BuffHelper.CombineBuffInfo(buffs[i].eBuff, buffs[i], costItem.buffs[0]);
                        return;
                    }
                }
                buffs.Add(costItem.buffs[0]);
                level++;
            }

        }

        public void Clear()
        {
            level = -1;
            buffs = null;
        }

        public Item ToItem()
        {
            int num = ((int)buffs[0].eBuff);
            Item item = new Item(ItemType.Buff, num.ToString());
            return item;
        }

        public static BuffItem Create(Item item)
        {
            EBuff eBuff;
            if (item.id[0] == '#')
            {
                //根据类型抽取
                var valueString = item.id.Substring(1);
                int.TryParse(valueString, out int num);
                EBuffType eBuffType = (EBuffType)num;
                eBuff = BuffHelper.GetRandomBuff(eBuffType);
            }
            else
            {
                //直接转数字
                int.TryParse(item.id, out int num);
                eBuff = (EBuff)num;
            }

            var buffItem = BuffHelper.CreatBuffItem(eBuff);
            return buffItem;
        }

        public Sprite GetBuffSprite()
        {
            var so = RunTimePoolMgr.Inst.staticResSoUsing.buffSpriteSo;
            int index = (int)GetBuffType;
            if (index < 0)
            {
                return null;
            }
            return so.values[index];
        }

    }

    //词条Buff的最基础单位
    [Serializable]
    public struct BuffInfo
    {
        public EBuff eBuff;

        public float[] addInfo;
    }

    public static class BuffHelper
    {
        public static EBuff GetRandomBuff(EBuffType eBuffType)
        {
            List<EBuff> list = new List<EBuff>();
            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (item.GetBuffType() == eBuffType)
                {
                    list.Add(item);
                }
            }
            var get = list.GetRandom();
            return get;
        }


        public static BuffItem CreatBuffItem(EBuff eBuff)
        {
            BuffItem buffItem = new BuffItem();
            buffItem.buffs = new List<BuffInfo>()
            {
                //读取配置
                ConfigMgr.BuffConfigSo.GetBuffInfo(eBuff)
            };
            return buffItem;
        }

        public static BuffItem GenRandomBuffItem(int level, List<BuffInfo> buffs)
        {
            BuffItem item = new BuffItem();
            item.level = level;

            //TODO
            //item.buffs = RandomHelper.GetRandomList<BuffInfo>(buffs, level+1);

            return item;
        }

        public static List<BuffInfo> GetBuffInfos(this List<BuffItem> items)
        {
            List<BuffInfo> list = new List<BuffInfo>();
            foreach (BuffItem item in items)
            {
                if (item.GetBuffType != EBuffType.None)
                {
                    list.AddRange(item.buffs);
                }
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
