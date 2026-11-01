using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 直线子弹: 沿炮口朝向匀速前进, 命中或超时后回收
    /// </summary>
    public class TurretBulletProjectile : TurretProjectile
    {
        [Tooltip("飞行速度(米/秒)")]
        public float speed = 30f;

        private Vector3 moveDir;

        protected override void OnLaunched(float range)
        {
            moveDir = transform.forward;
        }

        protected override void OnMove(float dt)
        {
            transform.position += moveDir * (speed * dt);
        }
    }
}
