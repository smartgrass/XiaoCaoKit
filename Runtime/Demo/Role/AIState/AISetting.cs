using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 保存公用数据
    /// </summary>
    [CreateAssetMenu(fileName = "AISetting", menuName = "SO/AI/AISetting", order = 0)]
    public class AISetting: ScriptableObject
    {
        public string des;

        [Tooltip("基础躲避/游荡时长(秒), 非战斗以及战斗中未触发倒退时使用")]
        public float idleTime = 2f;

        [Header("倒退移动时长(秒)")]
        [Tooltip("仅当战斗中触发 isLookAtTargetOnHide(看向目标倒退)时生效, 在 x~y 之间随机; 其余情况仍用 idleTime")]
        public Vector2 hideTimeRange = new Vector2(0.8f, 1.5f);

        public float sleepTime = 0.5f;

        public float moveSpeed = 3f;
        public float walkSR = 0.35f; //SR = SpeedRate
        public float walkAnimSR = 0.5f;
        [Header("退出idle循环概率,相当于攻击欲望")]
        public float idleExitRate = 0.5f;
        [Header("战斗中 Idle 提前退出")]
        [Tooltip("大于 0 时生效，MaxIdleTimeOnFight 在Fight状态最多能停留的idle时间")]
        public float maxIdleTimeOnFight = -1f;
        [Header("开头直接进入Idle概率")]
        public float randomIdleStart = 0.5f;

        public LookAtTargetOnHideMode isLookAtTargetOnHide = LookAtTargetOnHideMode.Off;

        [Header("视觉范围")]
        public float seeR = 18;
        public float seeAngle = 40;

    }

    public enum LookAtTargetOnHideMode
    {
        Off = 0,
        Half = 1,//每两次执行一次
        On = 2,
    }
}
