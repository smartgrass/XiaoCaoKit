using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao.Buff;

namespace XiaoCao
{
    public class BuffControl : RoleComponent<Role>
    {
        public PlayerBuffs playerBuffs = new PlayerBuffs();

        public BuffControl(Role _owner) : base(_owner)
        {
            playerBuffs.BuffUpdataAct += UpdateAttributeValues;
        }

        private readonly List<IBuffEffect> buffEffectList = new();


        public void AddBuff(BuffItem buff)
        {
            playerBuffs.AddBuff(buff);
        }

        public void RemoveAllEffect()
        {
            foreach (IBuffEffect buff in buffEffectList)
            {
                buff.RemoveEffect();
            }
            buffEffectList.Clear();
        }

        public void UpdateAttributeValues()
        {
            //先移除
            RemoveAllEffect();

            int playerId = owner.id;
            var EquippedBuffs = playerBuffs.EquippedBuffs;
            int len = EquippedBuffs.Count;
            for (int i = 0; i < len; i++)
            {
                if (!EquippedBuffs[i].IsEnable)
                {
                    continue;
                }
                string key = $"EBuff_{i}";
                foreach (var buff in EquippedBuffs[i].buffs)
                {
                    var instance = BuffBinder.Inst.GetOrCreateInstance(buff.eBuff);
                    if (instance != null)
                    {
                        buffEffectList.Add(instance);
                        instance.ApplyEffect(key, buff, playerId);
                    }
                    else
                    {
                        //默认做法
                        Debug.LogError($"--- no buffEffect {buff}");
                    }
                }
            }
        }

        public override void Update()
        {
            foreach (IBuffEffect buff in buffEffectList)
            {
                if (buff.HasLife)
                {
                    buff.Update();
                }
            }
        }

        public override void OnDestroy()
        {
            RemoveAllEffect();
        }

    }
    public class PlayerBuffs
    {
        public Action BuffUpdataAct;

        public bool NoUpdateAtrribute { get; set; }

        public int MaxEquipped = 4;
        // 装备中buffe
        public List<BuffItem> EquippedBuffs = new List<BuffItem>();
        // 未装备buff
        public List<BuffItem> UnequippedBuffs = new List<BuffItem>();

        public BuffItem GetValue(bool isEquipped, int index)
        {
            List<BuffItem> sourceList = isEquipped ? EquippedBuffs : UnequippedBuffs;
            if (sourceList.Count > index)
            {
                return sourceList[index];
            }
            return default;
        }

        public void AddBuff(BuffItem buff)
        {
            int findEmpty = -1;
            for (int i = 0; i < UnequippedBuffs.Count; i++)
            {
                if (UnequippedBuffs[i].GetBuffType == EBuffType.None)
                {
                    findEmpty = i;
                    UnequippedBuffs[findEmpty] = buff;
                    return;
                }
            }

            UnequippedBuffs.Add(buff);
        }

        // 合成: 移除buff-from, 将from第一个词条加在to
        public void UpgradeBuff(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> fromList = isFromEquipped ? EquippedBuffs : UnequippedBuffs;
            List<BuffItem> toList = isToEquipped ? EquippedBuffs : UnequippedBuffs;

            var toItem = toList[ToIndex];
            var costItem = fromList[FromIndex];

            toItem.UpGradeItem(costItem);
            toList[ToIndex] = toItem;
            costItem.Clear();
            fromList[FromIndex] = costItem;

            if (!NoUpdateAtrribute)
            {
                UpdateAttributeValues();
            }
        }

        // 移动buff: 将装备中或未装备中的buff移动到任意位置, 如果该位置已存在buff,则交换两buff位置
        public void MoveBuff(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> sourceList = isFromEquipped ? EquippedBuffs : UnequippedBuffs;
            List<BuffItem> baseList = isToEquipped ? EquippedBuffs : UnequippedBuffs;

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
            Debug.Log($"--- 交换了");
            if (!NoUpdateAtrribute)
            {
                UpdateAttributeValues();
            }
        }

        public int FindEmptyIndex(bool Equipped)
        {
            if (Equipped)
            {
                for (int i = 0; i < EquippedBuffs.Count; i++)
                {
                    if (!EquippedBuffs[i].IsEnable)
                    {
                        return i;
                    }
                }
                if (EquippedBuffs.Count < MaxEquipped)
                {
                    return EquippedBuffs.Count;
                }
                return MaxEquipped - 1;
            }
            else
            {
                for (int i = 0; i < UnequippedBuffs.Count; i++)
                {
                    if (!UnequippedBuffs[i].IsEnable)
                    {
                        return i;
                    }
                }
                return UnequippedBuffs.Count;
            }
        }

        public void UpdateAttributeValues()
        {
            BuffUpdataAct.Invoke();
        }

    }
}