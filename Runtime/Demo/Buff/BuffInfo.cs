using cfg;
using OdinSerializer;
using System;
using System.Collections.Generic;
// 移除了错误的引用，该命名空间不存在于 Unity.VisualScripting 中
using static Cinemachine.DocumentationSortingAttribute;

namespace XiaoCao
{
    //词条Buff的最基础单位
    [Serializable]
    public struct BuffInfo
    {
        public EBuff eBuff;

        public float[] addInfo;
    }
    [XCHelper]
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
                ConfigMgr.Inst.BuffConfigSo.GetBuffInfo(eBuff)
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