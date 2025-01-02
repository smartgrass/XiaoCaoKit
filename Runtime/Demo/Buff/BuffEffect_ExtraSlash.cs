using TEngine;
using UnityEngine;

namespace XiaoCao.Buff
{
    public class BuffEffect_ExtraSlash : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.ExtraSlash;

        public const string BulletPath = "Assets/_Res/SkillPrefab/Buff/B_ExtraSlash.prefab";

        public float Speed = 20;
        
        public AssetPool bulletPool;

        private Player0 player;



        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            GameEvent.AddEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.Int(), OnPlaySkill);
            bulletPool = PoolMgr.Inst.GetOrCreatPool(BulletPath);
            player = TargetId.GetPlayerById();

        }
        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<ObjectData>(EGameEvent.PlayerCreatNorAtk.Int(), OnPlaySkill);
        }

        private void OnPlaySkill(ObjectData data)
        {
            GameObject b = bulletPool.Get();
            b.transform.position = data.Tran.position;
            b.transform.rotation = data.Tran.rotation;
            b.GetComponent<Rigidbody>().linearVelocity = data.Tran.forward * Speed;
            var info = AtkInfoHelper.CreatInfo(player, Buff.ToString());
            b.GetComponent<AtkTrigger>().InitAtkInfo(info);
        }
    }

}
