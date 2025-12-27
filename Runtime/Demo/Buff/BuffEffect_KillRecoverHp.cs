using TEngine;

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
            GameEvent.AddEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
        }


        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.EnemyDeadEvent.ToInt(), OnEnemyDeadEvent);
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
}