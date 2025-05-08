using OdinSerializer;
using UnityEngine;

namespace XiaoCao
{
    ///<see cref="XCPathConfig"/>
    public static class XCSetting
    {
        public static readonly int FrameRate = 30;
        public static readonly float FramePerSec = 1f / FrameRate;
    }

    ///<see cref="PathTool"/>
    public static class XCPathConfig
    {
        public static readonly string DataDir = "Assets/_RawFile/SkillData";
        public static readonly string PerfabDir = "Assets/_Res/SkillPrefab";

        public static readonly string HitEffectDir = "Assets/_Res/SkillPrefab/Hit";
        public static readonly string AudioClipDir = "Assets/_Res/Audio";
        public static readonly string SpriteDir = "Assets/_Res/Sprite";

        public static readonly string AnimControllerDir = "Assets/_Res/Role/AnimController";
        public const string RawFileExtend = ".bytes";
        public const DataFormat RawFileFormat = DataFormat.Binary;


        public static string GetSkillIconPath(string skillId)
        {
            return $"{SpriteDir}/SkillIcon/{skillId}.png";
        }

        //获取动画机
        public static string GetAnimatorControllerPath(string raceId)
        {
            return $"{AnimControllerDir}/{raceId}.controller";
        }

        public static string GetGameConfigFile(string filePath)
        {
            return $"{GetGameConfigDir()}/{filePath}";
        }

        public static string GetExtraPackageDir()
        {
            if (Application.isEditor)
            {
                return $"{PathTool.GetProjectPath()}/Build/ExtraPackage";
            }
            else
            {
                if (!Application.isMobilePlatform)
                {
                    return $"{PathTool.GetProjectPath()}/ExtraPackage";
                }

                return $"{Application.streamingAssetsPath}/yoo/ExtraPackage";
            }
        }
        //BuildTool.AfterBuild 在打包后将复制配置
        public static string GetGameConfigDir()
        {
            if (Application.isEditor)
            {
                return $"{PathTool.GetProjectPath()}/GameConfig";
            }
            else
            {
                return $"{PathTool.GetProjectPath()}/GameConfig";
            }
        }

        public static string GetSkillPrefabDir(string raceId)
        {
            return $"{PerfabDir}/{raceId.ToString()}";
        }
        public static string GetSkillDataPath(string dir, string skillId)
        {
            return $"{DataDir}/{dir.ToString()}/{skillId}{RawFileExtend}";
        }

        public static string GetRoleBodyPath(string prefabName, RoleType roleType)
        {
            return $"{ResMgr.RESDIR}/Role/Body/{prefabName}.prefab";
        }

        public static string GetIdRolePath(RoleType roleType, string prefabId)
        {
            return $"{ResMgr.RESDIR}/Role/{roleType}/{prefabId}.prefab";
        }

        public static string GetAIPath(string aiId)
        {
            return $"{ResMgr.RESDIR}/Role/AI/AI_{aiId}.asset";
        }

        public static string GetHitEffectPath(int index)
        {
            return $"{HitEffectDir}/Hit_{index}.prefab";
        }
        public static string GetAudioPath(string name)
        {
            if (name.StartsWith(AudioClipDir))
            {
                return name;
            }
            return $"{AudioClipDir}/{name}";
        }

        public static string GetLevelImage(int level)
        {
            return $"{SpriteDir}/{level}.png";
        }

        public static string ToShortName(this RoleType roleType)
        {
            string shortName = roleType == RoleType.Enemy ? "E" : "P";
            return shortName;
        }
    }

    public static class DebugSetting
    {

        public static readonly string DebugGUI_IsShow = "DebugGUI_IsShow";

        public static readonly string OtherShowing = "DebugGUI/IsOtherShowing";
        public static bool IsDebug
        {
            get
            {
                //return true;
                if (Application.isEditor)
                {
                    return DebugGUI_IsShow.GetKeyBool();
                }
                else
                {
                    if (ConfigMgr.MainCfg.GetValue("Setting", "DebugLog", "0") == "1")
                    {
                        return true;
                    }
                }
                return false;
            }
        }

    }
}