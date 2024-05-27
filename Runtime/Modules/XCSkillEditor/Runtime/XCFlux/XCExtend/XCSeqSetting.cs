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

    public static class XCPathConfig
    {
        public static readonly string DataDir = "Assets/_RawFile/SkillData";
        public static readonly string PerfabDir = "Assets/_Res/SkillPrefab";

        public static readonly string HitEffectDir = "Assets/_Res/SkillPrefab/Hit";
        public static readonly string SpriteDir = "Assets/_Res/SpriteDir";

        public static string GetSkillPrefabDir(RoleType roleType)
        {
            return $"{PerfabDir}/{roleType.ToString()}";
        }
        public static string GetSkillDataPath(RoleType roleType, int skillId)
        {
            return $"{DataDir}/{roleType.ToString()}/{skillId}.bytes";
        }

        public static string GetRolePrefabPath(RoleType roleType, int prefabId)
        {
            return $"{ResMgr.RESDIR}/Role/{roleType}/{roleType}{prefabId}.prefab";
        }

        public static string GetRoleBasePath(RoleType roleType)
        {
            return $"{ResMgr.RESDIR}/Role/{roleType}/{roleType}.prefab";
        }


        public static string GetHitEffectPath(int index)
        {
            return $"{HitEffectDir}/Hit_{index}.prefab";
        }

        public static string GetLevelImage(int level)
        {
            return $"{SpriteDir}/{level}.png";
;        }
    }

    ///<see cref="XCPathConfig"/>
    public static class XCSetting
    {
        public static readonly int FrameRate = 30;
        public static readonly float FramePerSec = 1f / FrameRate;
    }

}