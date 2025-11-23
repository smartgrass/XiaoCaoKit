using TEngine;
using UnityEngine;

namespace XiaoCao.Buff
{
    [XCBuff]
    public class BuffEffect_HurtRecover : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.HurtRecover;

        private float recoverPercent;
        private float cdTime;
        private float lastTriggerTime;

        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;

            // 解析参数，第一个是回复百分比，第二个是冷却时间
            recoverPercent = buff.addInfo[0] / 100f;
            cdTime = buff.addInfo.Length > 1 ? buff.addInfo[1] : 0f;
            lastTriggerTime = -cdTime; // 确保第一次触发不会受冷却限制

            // 注册受到伤害事件回调
            GameEvent.AddEventListener<int, bool, AtkInfo>(EGameEvent.RoleHurt.Int(), OnRoleHurt);
        }

        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<int, bool, AtkInfo>(EGameEvent.RoleHurt.Int(), OnRoleHurt);
            base.RemoveEffect();
        }

        private void OnRoleHurt(int entityId, bool isPlayer, AtkInfo info)
        {
            // 检查是否是目标实体受到伤害
            if (entityId != TargetId)
                return;

            // 检查冷却时间
            if (Time.time - lastTriggerTime < cdTime)
                return;

            // 触发回血效果
            TriggerRecover();
        }

        private void TriggerRecover()
        {
            var player = TargetId.GetPlayerById();
            if (player == null)
                return;

            // 更新最后触发时间
            lastTriggerTime = Time.time;

            // 计算回复量并回复生命值
            float healAmount = player.MaxHp * recoverPercent;
            player.ChangeNowValue(ENowAttr.Hp, healAmount);
        }
    }
}