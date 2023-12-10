using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(fileName = "XCSeqSetting", menuName = "XCSeqSetting")]
    public class XCSeqSetting : ScriptableObject
    {
        public RoleType type;

        public int index = 0;

        public RuntimeAnimatorController targetAnimtorController;
    
    
    }

    public static class XCSetting
    {
        public static readonly string DataDir = "Assets/_Res/SkillData";
        public static readonly string PerfabDir = "Assets/_Res/SkillPrefab";
        public static readonly int FrameRate = 30;
        public static readonly float FramePerSec = 1f / FrameRate;
    }

}