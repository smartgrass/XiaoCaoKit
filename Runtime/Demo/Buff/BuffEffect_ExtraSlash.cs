using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao.Buff
{
    public class BuffEffect_ExtraSlash : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.ExtraSlash;

        public float Speed = ExtraSlashHelper.DefaultSpeed;

        private Player0 player;

        public int maxSlashCount = 10;
        public int triggerCount = 0;
        public int curSlashCount = 0;
        public float lifeTime = ExtraSlashHelper.DefaultLifeTime;

        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            GameEvent.AddEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.ToInt(), OnPlaySkill);
            player = TargetId.GetPlayerById();
            triggerCount = (int)buff.addInfo[0];
        }

        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.ToInt(), OnPlaySkill);
        }

        private void OnPlaySkill(ObjectData data)
        {
            curSlashCount++;
            if (curSlashCount >= triggerCount)
            {
                //cd刷新
                if (curSlashCount >= maxSlashCount)
                {
                    curSlashCount = 0;
                }
                else
                {
                    return;
                }
            }

            ExtraSlashHelper.FireSlash(data.Tran, player, Speed, lifeTime);
        }
    }
}
