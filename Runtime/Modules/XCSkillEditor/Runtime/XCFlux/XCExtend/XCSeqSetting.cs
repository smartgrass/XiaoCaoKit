using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(fileName = "XCSeqSetting", menuName = "XCSeqSetting")]
    public class XCSeqSetting : ScriptableObject
    {
        [Label("描述")]
        public string des;

        public RoleType type;

        [InfoBox("raceId 为职业,影响配置选择,\n如移速,武器种类,技能等等")]
        [Label("raceId")]
        public string raceId;

        public RuntimeAnimatorController targetAnimtorController;
    }
}