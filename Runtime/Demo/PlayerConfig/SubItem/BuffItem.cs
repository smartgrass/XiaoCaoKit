using System;
using System.Collections.Generic;
using UnityEngine;

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
            get { return GetBuffType != EBuffType.None; }
        }

        public bool IsMaxLevel
        {
            get { return level >= ConfigMgr.Inst.BuffConfigSo.GetMaxLevel(GetFirstEBuff); }
        }

        public int GetExBuffMaxLevel
        {
            get => ConfigMgr.Inst.BuffConfigSo.GetMaxLevel(GetFirstEBuff);
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
                buffs[0] = ConfigMgr.Inst.BuffConfigSo.GetExBuffInfoWithLevel(costItem.buffs[0].eBuff, level);
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
            if (item.typeId[0] == '#')
            {
                //根据类型抽取
                var valueString = item.typeId.Substring(1);
                int.TryParse(valueString, out int num);
                EBuffType eBuffType = (EBuffType)num;
                eBuff = BuffHelper.GetRandomBuff(eBuffType);
            }
            else
            {
                //直接转数字
                int.TryParse(item.typeId, out int num);
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

        public Color GetColor()
        {
            var colorSetting = UIPrefabSo.Inst.buffColorSettingSo;
            int len = colorSetting.values.Length;
            if (GetBuffType == EBuffType.None)
            {
                return colorSetting.values[len - 1];
            }

            int index = level;
            if (index < 0)
            {
                index = len - 1;
            }

            if (index >= colorSetting.values.Length)
            {
                index = colorSetting.values.Length - 1;
            }

            return colorSetting.values[index];
        }
    }
}