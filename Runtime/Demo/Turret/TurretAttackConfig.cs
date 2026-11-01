using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台攻击配置基类: 一种弹道(子弹/导弹/激光)对应一个 ScriptableObject 文件,
    /// 射程、冷缩、伤害 skillId、准星检测层等攻击数据都放在这里;
    /// 炮头的转向速度、第一人称视角、后坐力等共用表现仍留在 <see cref="Turret"/> 组件上
    /// </summary>
    public abstract class TurretAttackConfig : ScriptableObject
    {
        [Header("伤害")]
        [Tooltip("伤害/表现使用的 skillId, 读 SkillSetting")]
        public string skillId = "Turret";

        [Header("射程与节奏")]
        [Tooltip("攻击范围, 超出范围的敌人不会被自动瞄准")]
        public float attackRange = 12f;

        [Tooltip("攻击冷缩(秒)")]
        public float attackCooldown = 1f;

        [Header("自动攻击")]
        [Tooltip("非玩家角色(支援角色/敌人)操控时的索敌范围与开火节奏, 三种弹道可共用同一份;" +
                 "留空则使用代码内置默认值")]
        public TurretAutoFireConfig autoFireConfig;

        [Header("准星检测")]
        [Tooltip("准星/发射检测的阻挡层(双方角色层会自动包含); 勾 WallBlock(实体墙)、Ground(地面)、" +
                 "Default(未分层楼板)。Wall 是空气墙, 不挡视线, 请不要勾")]
        public LayerMask aimBlockMask = Layers.WALL_BLOCK_MASK | Layers.GROUND_MASK | Layers.DEFAULT_MASK;

        [Tooltip("准星射线没有命中任何阻挡时的射击落点距离(米)")]
        public float aimDetectDistance = 30f;

        [Header("音效")]
        [Tooltip("开火音效, 直接把 AudioClip 拖进来; 留空则不播")]
        public AudioClip fireSfx;

        [Range(0f, 1f)]
        [Tooltip("开火音效音量")]
        public float fireSfxVolume = 1f;

        [Tooltip("命中场景(墙/地面)时的音效, 直接把 AudioClip 拖进来; 留空则不播")]
        public AudioClip hitSfx;

        [Range(0f, 1f)]
        [Tooltip("命中音效音量")]
        public float hitSfxVolume = 1f;

        /// <summary>该配置对应的弹道类型, 决定炮台走哪条发射分支</summary>
        public abstract ETurretProjectileType ProjectileType { get; }

        /// <summary>准星/发射检测用的层: <see cref="aimBlockMask"/> 再并上双方角色层</summary>
        public int AimRayMask => aimBlockMask.value | Layers.PLAYER_MASK | Layers.ENEMY_MASK;

        /// <summary>自动攻击配置(留空时退回内置默认值, 调用方不用判空)</summary>
        public TurretAutoFireConfig AutoFire =>
            autoFireConfig != null ? autoFireConfig : TurretAutoFireConfig.Fallback;

        /// <summary>非玩家角色操控时的实际射程: 攻击范围与索敌距离取较大值, 否则会看得到打不到</summary>
        public float AiDetectDistance => AutoFire.GetDetectDistance(attackRange);
    }
}
