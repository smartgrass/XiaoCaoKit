using System.Collections.Generic;
// 移除了错误的引用，该命名空间不存在于 Unity.VisualScripting 中
using UnityEngine;
using XiaoCao.Buff;

namespace XiaoCao
{
    public class BuffControl : RoleComponent<Role>
    {
        public PlayerBuffs playerBuffs = new PlayerBuffs();

        public BuffControl(Role _owner) : base(_owner)
        {
            playerBuffs.UpdataAllBuffAct += OnUpdateAllBuff;
            playerBuffs.BuffItemUpdataAct += OnBuffIemUpdate;
        }

        private Dictionary<EBuff, IBuffEffect> buffDic = new();

        public void AddBuff(BuffItem buff)
        {
            playerBuffs.AddBuff(buff);
        }

        public void RemoveAllEffect()
        {
            foreach (IBuffEffect buff in buffDic.Values)
            {
                buff.RemoveEffect();
            }
            buffDic.Clear();
        }

        public void OnUpdateAllBuff()
        {
            Debuger.Log($"--- Re Apply All Buff");
            //先移除
            RemoveAllEffect();

            var EquippedBuffs = playerBuffs.EquippedExBuffs;
            int len = EquippedBuffs.Count;
            for (int i = 0; i < len; i++)
            {
                var buffItem = EquippedBuffs[i];
                if (!buffItem.IsEnable)
                {
                    continue;
                }
                EnableBuff(buffItem);
            }

            EnableBuff(playerBuffs.norBuff);
        }

        public void OnBuffIemUpdate(BuffItem item)
        {
            if (item.GetBuffType == EBuffType.Nor)
            {
                foreach (var buff in item.GetBuffs)
                {
                    if (buffDic.ContainsKey(buff.eBuff))
                    {
                        buffDic[buff.eBuff].RemoveEffect();
                    }
                }
                EnableBuff(playerBuffs.norBuff);
            }
            else
            {
                if (buffDic.ContainsKey(item.GetFirstEBuff))
                {
                    buffDic[item.GetFirstEBuff].RemoveEffect();
                }
                EnableBuff(item);
            }
        }

        private void EnableBuff(BuffItem buffItem)
        {
            int playerId = owner.id;
            foreach (var buff in buffItem.buffs)
            {
                var instance = BuffBinder.Inst.GetOrCreateInstance(buff.eBuff);
                if (instance != null)
                {
                    buffDic[buff.eBuff] = instance;
                    string key = buff.eBuff.ToString();
                    //可能会出现key重复的情况, 需要处理叠加
                    instance.ApplyEffect(key, buff, playerId);
                }
                else
                {
                    //默认做法
                    Debug.LogError($"--- no buffEffect {buff.eBuff}");
                }
            }
        }

        public override void Update()
        {
            foreach (IBuffEffect buff in buffDic.Values)
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
}