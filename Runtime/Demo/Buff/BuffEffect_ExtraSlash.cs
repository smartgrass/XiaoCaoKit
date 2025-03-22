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



        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            GameEvent.AddEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.Int(), OnPlaySkill);
            player = TargetId.GetPlayerById();

        }
        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.Int(), OnPlaySkill);
        }

        private void OnPlaySkill(ObjectData data)
        {
            GameObject b = PoolMgr.Inst.Get(BulletPath,4);
            b.transform.position = data.Tran.position;
            b.transform.rotation = data.Tran.rotation;
            b.GetComponent<Rigidbody>().linearVelocity = data.Tran.forward * Speed;
            var info = AtkInfoHelper.CreatInfo(player, Buff.ToString());
            var Atker = b.GetComponent<Atker>();
            Atker.InitAtkInfo(info);
            Atker.AddTriggerByCollider();
        }
    }

}
