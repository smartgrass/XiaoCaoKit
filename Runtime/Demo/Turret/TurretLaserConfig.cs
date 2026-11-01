using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 激光炮台攻击配置: 瞬时光束。光束组件 <see cref="TurretLaser"/> 挂在炮台预制体上,
    /// 组件引用不能存进 ScriptableObject, 所以这里只有数值配置
    /// </summary>
    [CreateAssetMenu(menuName = "SO/TurretLaserConfig", fileName = "TurretLaserConfig")]
    public class TurretLaserConfig : TurretAttackConfig
    {
        public override ETurretProjectileType ProjectileType => ETurretProjectileType.Laser;
    }
}
