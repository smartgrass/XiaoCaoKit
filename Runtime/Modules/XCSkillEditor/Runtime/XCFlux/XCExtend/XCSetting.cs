using OdinSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (!Application.isMobilePlatform)
            {
                return $"{PathTool.GetProjectPath()}/GameConfig";
            }
            return $"{Application.streamingAssetsPath}/GameConfig";
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



        //Demo专用配置
        public static bool IsSkillEditor
        {
            get => SceneManager.GetActiveScene().name.StartsWith("SkillEditor");
        }

        public static int PauseFrame;
    }
}