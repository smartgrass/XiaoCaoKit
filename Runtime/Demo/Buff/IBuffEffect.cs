﻿using System;

namespace XiaoCao.Buff
{
    public interface IBuffEffect
    {
        public EBuff Buff { get; }

        public int TargetId { get; set; }

        public void ApplyEffect(string key, BuffInfo buff, int targetId);

        void RemoveEffect();
    }

    public class XCBuffAttribute : Attribute{ }

    public  class BaseBuffEffect: IBuffEffect
    {
        public virtual EBuff Buff => EBuff.None;

        public int TargetId { get ; set; }

        public virtual void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
           
        }

        public virtual void RemoveEffect()
        {
            
        }
    }

    public abstract class BuffEffect_BaseAttrModify : BaseBuffEffect
    {
        public string Key { get; set; }

        public virtual EAttr EAttr => throw new System.Exception();

        public override void RemoveEffect()
        {
            var player = TargetId.GetPlayerById();
            player.PlayerAttr.GetAttribute(EAttr).RemoveModifier(Key);
        }

        /* 示例
         public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;
            this.Key = key;
            var player = TargetId.GetPlayerById();
            AttributeModifier modifier = new AttributeModifier
            {
                Add = buff.addInfo[0]
            };
            player.PlayerAttr.ChangeAttrValue(EAttr, Key, modifier);
        }
         */
    }

}