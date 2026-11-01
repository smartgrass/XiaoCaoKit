using UnityEngine;

namespace XiaoCao
{
    /// <summary>子弹炮台攻击配置: 直线弹道</summary>
    [CreateAssetMenu(menuName = "SO/TurretBulletConfig", fileName = "TurretBulletConfig")]
    public class TurretBulletConfig : TurretAttackConfig
    {
        [Tooltip("子弹预制体(需挂 TurretBulletProjectile)")]
        public GameObject bulletPrefab;

        public override ETurretProjectileType ProjectileType => ETurretProjectileType.Bullet;
    }
}
