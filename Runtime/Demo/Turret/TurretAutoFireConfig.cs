using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台自动攻击配置: 非玩家角色(支援角色/敌人)操控炮台时的索敌与开火节奏。
    /// 独立成 SO 是为了让子弹/导弹/激光三种弹道共用同一套自动攻击手感,
    /// 不用在每份攻击配置里各存一份相同的参数
    /// </summary>
    [CreateAssetMenu(menuName = "SO/TurretAutoFireConfig", fileName = "TurretAutoFireConfig")]
    public class TurretAutoFireConfig : ScriptableObject
    {
        [Header("索敌")]
        [Tooltip("索敌距离(米); 与攻击范围取较大值作为实际射程")]
        public float searchRange = 80f;

        [Tooltip("索敌角度(半角, 度); 实际会按炮头的偏航上限收窄")]
        public float searchAngle = 80f;

        [Tooltip("瞄准点相对目标根节点的高度")]
        public float aimOffsetY = 1f;

        [Header("开火节奏")]
        [Tooltip("支援角色操控时\"按住攻击\"的持续时间(秒), 期间按攻击冷缩连发")]
        public float friendlyHoldDuration = 1.5f;

        [Tooltip("支援角色操控时\"松开攻击\"的间隔(秒), 期间不开火")]
        public float friendlyReleaseDuration = 1f;

        [Tooltip("敌人操控炮台时\"按住攻击\"的持续时间(秒), 期间按攻击冷缩连发")]
        public float enemyHoldDuration = 1f;

        [Tooltip("敌人操控炮台时\"停顿\"的间隔(秒), 期间不开火")]
        public float enemyReleaseDuration = 3f;

        /// <summary>实际射程: 攻击范围与索敌距离取较大值, 否则会看得到打不到</summary>
        public float GetDetectDistance(float attackRange)
        {
            return Mathf.Max(attackRange, searchRange);
        }

        private static TurretAutoFireConfig fallback;

        /// <summary>没有配置 asset 时的内置默认值, 让调用方不用到处判空</summary>
        public static TurretAutoFireConfig Fallback
        {
            get
            {
                if (fallback == null)
                {
                    fallback = CreateInstance<TurretAutoFireConfig>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }
    }
}
