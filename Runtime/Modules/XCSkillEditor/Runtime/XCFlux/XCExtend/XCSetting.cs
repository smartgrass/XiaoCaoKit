﻿using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.IO;
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
        public static readonly string SpriteDir = "Assets/_Res/SpriteDir";

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
                return $"{Application.dataPath}/ExtraPackage";
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
                return $"{Application.dataPath}/GameConfig";
            }
        }

        public static string GetSkillPrefabDir(string raceId)
        {
            return $"{PerfabDir}/{raceId.ToString()}";
        }
        public static string GetSkillDataPath(string dir, int skillId)
        {
            return $"{DataDir}/{dir.ToString()}/{skillId}.bytes";
        }

        public static string GetRoleBodyPath(string prefabName, RoleType roleType)
        {
            return $"{ResMgr.RESDIR}/Role/Body/{prefabName}.prefab";
        }

        public static string GetIdRolePath(RoleType roleType, int prefabId)
        {
            return $"{ResMgr.RESDIR}/Role/{roleType}/{roleType.ToShortName()}_{prefabId}.prefab";
        }

        public static string GetAIPath(int aiId)
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

    public static class DebugSetting{

        public static readonly string DebugGUI_IsShow = "DebugGUI_IsShow";

        public static readonly string OtherShowing = "DebugGUI/IsOtherShowing";
        public static bool IsDebug
        {
            get
            {
                if (Application.isEditor)
                {
                    if (DebugGUI_IsShow.GetKeyBool())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

    }
}