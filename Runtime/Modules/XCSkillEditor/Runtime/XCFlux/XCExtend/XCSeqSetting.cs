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
        public RuntimeAnimatorController targetAnimtorController;
    }

    public static class XCSetting
    {
        public static readonly string DataDir = "Assets/_RawFile/SkillData";
        public static readonly string PerfabDir = "Assets/_Res/SkillPrefab";
        public static readonly int FrameRate = 30;
        public static readonly float FramePerSec = 1f / FrameRate;

        public static string GetSkillDataPath(RoleType roleType, int skillId)
        {
            return $"{DataDir}/{roleType.ToString()}/{skillId}.data";
        }
    }

}