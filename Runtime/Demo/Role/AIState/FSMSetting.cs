using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 保存公用数据
    /// </summary>
    [CreateAssetMenu(fileName = "FSMSetting", menuName = "SO/AI/FSMSetting", order = 0)]
    public class FSMSetting: ScriptableObject
    {
        public float sleepTime = 0.5f;
        public float hideTime = 2f;

        public float moveSpeed = 3f;
        public float walkSR = 0.35f; //SR = SpeedRate
        public float walkAnimSR = 0.5f;
        [XCLabel("攻击欲望")]
        public float atkDesire = 0.5f;

        public bool isLookAtTargetOnHide;
    }
}
