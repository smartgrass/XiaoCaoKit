using UnityEngine;

namespace XiaoCao.Buff
{
    /// <summary>
    /// 额外剑气发射工具: Buff 的 ExtraSlash 与技能2空中剑气共用同一种触发方式
    /// </summary>
    public static class ExtraSlashHelper
    {
        public const string BulletPath = "Assets/_Res/SkillPrefab/Buff/B_ExtraSlash.prefab";

        public const float DefaultSpeed = 20;
        public const float DefaultLifeTime = 1.8f;

        /// <summary>
        /// 从 fromTf 位置沿其朝向发射一发剑气, y 方向略下压
        /// </summary>
        public static void FireSlash(Transform fromTf, Player0 player, float speed = DefaultSpeed,
            float lifeTime = DefaultLifeTime)
        {
            GameObject b = PoolMgr.Inst.Get(BulletPath, lifeTime);
            b.transform.position = fromTf.position;
            b.transform.rotation = fromTf.rotation;
            Rigidbody rb = b.GetComponent<Rigidbody>();
            Vector3 dir = fromTf.forward * speed;
            if (dir.y > 0)
            {
                dir.y -= 1;
            }
            else
            {
                dir.y = -1;
            }
            rb.linearVelocity = dir;
            var info = AtkInfoHelper.CreatInfo(player, nameof(EBuff.ExtraSlash));
            var Atker = b.GetComponent<Atker>();
            Atker.InitAtkInfo(info);
            Atker.AddTriggerByCollider();
        }
    }
}
