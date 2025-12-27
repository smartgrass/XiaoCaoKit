using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao.Buff
{
    public class BuffEffect_ExtraSlash : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.ExtraSlash;


        public const string BulletPath = "Assets/_Res/SkillPrefab/Buff/B_ExtraSlash.prefab";

        public float Speed = 20;

        private Player0 player;

        public int maxSlashCount = 10;
        public int triggerCount = 0;
        public int curSlashCount = 0;
        public float lifeTime = 1.8f;

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

            GameObject b = PoolMgr.Inst.Get(BulletPath, lifeTime);
            b.transform.position = data.Tran.position;
            b.transform.rotation = data.Tran.rotation;
            Rigidbody rb = b.GetComponent<Rigidbody>();
            Vector3 dir = data.Tran.forward * Speed;
            if (dir.y > 0)
            {
                dir.y -= 1;
            }
            else
            {
                dir.y = -1;
            }
            rb.linearVelocity = dir;
            // XCTime.DelayRunMono(1.5f, () => { Down(rb); }, rb).Forget();
            var info = AtkInfoHelper.CreatInfo(player, Buff.ToString());
            var Atker = b.GetComponent<Atker>();
            Atker.InitAtkInfo(info);
            Atker.AddTriggerByCollider();
        }

        public void Down(Rigidbody rb)
        {
            rb.linearVelocity += Vector3.down;
        }
    }
}