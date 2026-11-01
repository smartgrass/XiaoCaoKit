using UnityEngine;

namespace XiaoCao
{
    /// <summary>导弹炮台攻击配置: 抛物线弹道, 有锁定目标时追踪</summary>
    [CreateAssetMenu(menuName = "SO/TurretMissileConfig", fileName = "TurretMissileConfig")]
    public class TurretMissileConfig : TurretAttackConfig
    {
        [Tooltip("导弹预制体(需挂 TurretMissileProjectile)")]
        public GameObject missilePrefab;

        public override ETurretProjectileType ProjectileType => ETurretProjectileType.Missile;
    }
}
