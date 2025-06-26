using OdinSerializer;
using UnityEngine;

namespace XiaoCao
{
    ///<see cref="XCPathConfig"/>
    public static class XCSetting
    {
        public static readonly int FrameRate = 30;
        public static readonly float FramePerSec = 1f / FrameRate;


        public const int PlayerTeam = 1;
        public const int EnmeyTeam = 0;
        //根据阵营分层级
        public static int GetTeamLayer(int team)
        {
            if (team == PlayerTeam)
            {
                return Layers.PLAYER;
            }
            return Layers.ENEMY;
        }

        public static int GetTeamAtkLayer(int team)
        {
            if (team == 1)
            {
                return Layers.PLAYER_ATK;
            }
            return Layers.ENEMY_ATK;
        }

        public static int GetTeamGroundCheckMash(int team)
        {
            if (team == 1)
            {
                return Layers.ENEMY_MASK;
            }
            return Layers.PLAYER_MASK;
        }

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
            string platformName = DebugSetting.IsMobilePlatform ? "Android" : "StandaloneWindows64";
            return $"{GetBuildExtraResDir()}/{platformName}";
        }
        //BuildTool.AfterBuild 在打包后将复制配置
        public static string GetGameConfigDir()
        {
            return $"{GetBuildExtraResDir()}/GameConfig";
        }


        //获取默认资源目录
        public static string GetBuildExtraResDir()
        {
            if (DebugSetting.IsMobilePlatform)
            {
                return $"{Application.persistentDataPath}/ExtraRes";
            }
            return $"{PathTool.GetProjectPath()}/ExtraRes";
        }

        public static string GetExtraResZipPath()
        {
            return $"{Application.streamingAssetsPath}/ExtraRes.zip";
        }

        public static string GetWindowBuildResDir()
        {
            return $"{PathTool.GetProjectPath()}/Build";
        }

        public static string GetStreamingAssetsPath()
        {
            return $"{Application.streamingAssetsPath}";
        }

        public static string GetSkillPrefabDir(string raceId)
        {
            return $"{PerfabDir}/{raceId.ToString()}";
        }
        public static string GetSkillDataPath(string skillId)
        {
            return $"{DataDir}/{skillId}{RawFileExtend}";
        }

        public static string GetRoleBodyPath(string prefabName, RoleType roleType)
        {
            return $"{ResMgr.RESDIR}/Role/Body/{prefabName}.prefab";
        }

        public static string GetIdRolePath(string prefabId)
        {
            return $"{ResMgr.RESDIR}/Role/IdRole/{prefabId}.prefab";
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

    public enum ResTypeP
    {
        Windows,
        Android,
        Editor
    }
}