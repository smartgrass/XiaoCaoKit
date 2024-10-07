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

        public float hideTime = 2f;

        public float sleepTime = 0.5f;

        public float moveSpeed = 3f;
        public float walkSR = 0.35f; //SR = SpeedRate
        public float walkAnimSR = 0.5f;
        [XCLabel("攻击欲望")]
        public float idleExitRate = 0.5f;

        public bool isLookAtTargetOnHide;

        [Header("视觉范围")]
        public float seeR = 18;
        public float seeAngle = 40;
    }
}
