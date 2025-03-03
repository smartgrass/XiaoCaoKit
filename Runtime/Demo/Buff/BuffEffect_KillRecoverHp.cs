
using TEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace XiaoCao.Buff
{

    [XCBuff]
    public class BuffEffect_KillRecoverHp : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.KillRecoverHpMult;

        public float recoverHp;

        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;

            var player = TargetId.GetPlayerById();

            recoverHp = buff.addInfo[0];

            //注册 击杀的事件回调
            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
        }


        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.Int(), OnEnemyDeadEvent);
        }

        void OnEnemyDeadEvent(int id)
        {
            if (EntityMgr.Inst.FindEntity<Enemy0>(id, out Enemy0 enemy))
            {
                var deadInfo = enemy.enemyData.deadInfo;
                if (deadInfo.killerId == TargetId)
                {
                    TriggerBuff();
                }
            }
        }

        void TriggerBuff()
        {
            var player = TargetId.GetPlayerById();
            float delta = player.MaxHp * recoverHp;
            player.ChangeNowValue(ENowAttr.Hp, delta);
        }


    }




    [XCBuff]
    public class BuffEffect_AtkRecoverHp : BuffEffect_BaseAttrModify
    {
        //吸血
        public override EBuff Buff => EBuff.AtkRecoverHpMult;

        public override EAttr EAttr => EAttr.AtkRecoverHp;

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
    }

    [XCBuff]
    public class BuffEffect_AtkAdd : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.AtkMult;
        public override EAttr EAttr => EAttr.Atk;


        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;
            this.Key = key;
            var player = TargetId.GetPlayerById();
            AttributeModifier modifier = new AttributeModifier
            {
                Multiply = buff.addInfo[0]
            };
            player.PlayerAttr.ChangeAttrValue(EAttr, Key, modifier);
        }
    }

    [XCBuff]
    public class BuffEffect_SkillDamageAdd : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.SkillDamageMult;
        //TODO NorAtkSpeedAdd
    }

    [XCBuff]
    public class BuffEffect_NorAtkSpeedAdd : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.NorAtkSpeedAdd;

        public override EAttr EAttr => EAttr.NorAtkSpeedAdd;


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
    }


    [XCBuff]
    public class BuffEffect_MaxHpAdd : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.MaxHpMult;

        public override EAttr EAttr => EAttr.MaxHp;


        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;
            this.Key = key;
            var player = TargetId.GetPlayerById();

            //保留hp的比例
            float percent = player.Hp / player.MaxHp;

            AttributeModifier modifier = new AttributeModifier
            {
                Multiply = buff.addInfo[0]
            };
            player.PlayerAttr.ChangeAttrValue(EAttr, Key, modifier);

            //保留hp的比例
            player.Hp = (int)(player.MaxHp * percent);
        }



    }

    [XCBuff]
    public class BuffEffect_CritAdd : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.CritAdd;

        public override EAttr EAttr => EAttr.Crit;

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
    }

    [XCBuff]
    public class BuffEffect_SkillCDOff : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.SkillCDOff;
        public override EAttr EAttr => EAttr.SkillCDOff;

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
            player.component.atkTimers.UpdataCdOff();
        }

        public override void RemoveEffect()
        {
            base.RemoveEffect();
            var player = TargetId.GetPlayerById();
            player.component.atkTimers.UpdataCdOff();
        }
    }

    [XCBuff]
    public class BuffEffect_MovementSpeedAdd : BuffEffect_BaseAttrModify
    {
        public override EBuff Buff => EBuff.MoveSpeedMultAdd;

        public override EAttr EAttr => EAttr.MoveSpeedMult;

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
    }

    [XCBuff]
    public class BuffEffect_UltimateEnergyRestore : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.UltimateEnergyRestore;
    }
}
