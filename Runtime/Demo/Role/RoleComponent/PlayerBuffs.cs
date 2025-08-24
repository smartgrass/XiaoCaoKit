using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class PlayerBuffs
    {
        public Action UpdataAllBuffAct;
        public Action<BuffItem> BuffItemUpdataAct;

        public bool NoUpdateAtrribute { get; set; }

        public int MaxEquipped = 5;
        ///<see cref="BuffControl"/>
        // 装备中buffe
        public List<BuffItem> EquippedExBuffs = new List<BuffItem>();
        // 未装备buff
        public List<BuffItem> UnequippedExBuffs = new List<BuffItem>();
        //被动,默认生效
        public BuffItem norBuff = new BuffItem();

        public BuffItem GetValue(bool isEquipped, int index)
        {
            List<BuffItem> sourceList = isEquipped ? EquippedExBuffs : UnequippedExBuffs;
            if (sourceList.Count > index)
            {
                return sourceList[index];
            }
            return default;
        }

        public void AddBuff(BuffItem buff)
        {
            // 如果是被动buff, 则直接累加到被动buff中
            if (buff.GetBuffType == EBuffType.Nor)
            {
                norBuff.CombineItem(buff);
                BuffItemUpdataAct?.Invoke(buff);
                return;
            }

            bool hasSame = false;
            //优先合并同类型纹章
            for (int i = 0; i < EquippedExBuffs.Count; i++)
            {
                if (EquippedExBuffs[i].GetFirstEBuff == buff.GetFirstEBuff)
                {
                    hasSame = true;
                    if (!EquippedExBuffs[i].IsMaxLevel)
                    {
                        EquippedExBuffs[i].CombineItem(buff);
                        BuffItemUpdataAct?.Invoke(EquippedExBuffs[i]);
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!hasSame && MaxEquipped - EquippedExBuffs.Count > 0)
            {
                //装备到空位置
                GetEquipExBuff(buff);
                BuffItemUpdataAct?.Invoke(buff);
                return;
            }

            int findEmpty = -1;
            for (int i = 0; i < UnequippedExBuffs.Count; i++)
            {
                if (UnequippedExBuffs[i].GetBuffType == EBuffType.None)
                {
                    findEmpty = i;
                    UnequippedExBuffs[findEmpty] = buff;
                    return;
                }
            }

            UnequippedExBuffs.Add(buff);
        }

        // 合成: 移除buff-from, 将from第一个词条加在to
        public void CombineItem(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> fromList = isFromEquipped ? EquippedExBuffs : UnequippedExBuffs;
            List<BuffItem> toList = isToEquipped ? EquippedExBuffs : UnequippedExBuffs;

            var toItem = toList[ToIndex];
            var costItem = fromList[FromIndex];

            toItem.CombineItem(costItem);
            toList[ToIndex] = toItem;
            costItem.Clear();
            fromList[FromIndex] = costItem;

            UpdateAllBuffs();
        }

        // 移动buff: 将装备中或未装备中的buff移动到任意位置, 如果该位置已存在buff,则交换两buff位置
        public void MoveBuff(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> sourceList = isFromEquipped ? EquippedExBuffs : UnequippedExBuffs;
            List<BuffItem> baseList = isToEquipped ? EquippedExBuffs : UnequippedExBuffs;

            if (FromIndex < 0 || FromIndex >= sourceList.Count || ToIndex < 0)
            {
                Debug.LogError("索引无效！");
                return;
            }

            if (ToIndex >= baseList.Count)
            {
                // 如果目标索引超出目标列表长度，则扩展列表（这个逻辑可以根据实际需求调整）
                for (int i = baseList.Count; i <= ToIndex; i++)
                {
                    var empty = new BuffItem();
                    empty.Clear();
                    baseList.Add(empty); // 或者创建一个默认的BuffItem实例
                }
            }

            BuffItem temp = sourceList[FromIndex];
            BuffItem temp2 = baseList[ToIndex];

            sourceList[FromIndex] = temp2;
            baseList[ToIndex] = temp;
            Debug.Log($"--- 交换了 From{FromIndex} to{ToIndex}");
            if (!NoUpdateAtrribute)
            {
                UpdateAllBuffs();
            }
        }

        public int FindEmptyIndex(bool Equipped)
        {
            if (Equipped)
            {
                for (int i = 0; i < EquippedExBuffs.Count; i++)
                {
                    if (!EquippedExBuffs[i].IsEnable)
                    {
                        return i;
                    }
                }
                if (EquippedExBuffs.Count < MaxEquipped)
                {
                    return EquippedExBuffs.Count;
                }
                return MaxEquipped - 1;
            }
            else
            {
                for (int i = 0; i < UnequippedExBuffs.Count; i++)
                {
                    if (!UnequippedExBuffs[i].IsEnable)
                    {
                        return i;
                    }
                }
                return UnequippedExBuffs.Count;
            }
        }

        public int FindFirstUnEpuipIndex()
        {
            for (int i = 0; i < UnequippedExBuffs.Count; i++)
            {
                if (UnequippedExBuffs[i].IsEnable)
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 得到并装备Buff
        /// </summary>
        public void GetEquipExBuff(BuffItem buff)
        {
            int toIndex = PlayerHelper.LocalPlayerBuffs.FindEmptyIndex(true);
            if (toIndex < 0)
            {
                return;
            }
            if (toIndex < EquippedExBuffs.Count)
            {
                EquippedExBuffs[toIndex] = buff;
            }
            else
            {
                EquippedExBuffs.Add(buff);
            }
        }

        public void UpdateAllBuffs()
        {
            if (NoUpdateAtrribute)
            {
                return;
            }
            UpdataAllBuffAct.Invoke();
        }

    }
}