﻿using DG.Tweening.Plugins.Core.PathCore;
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

        public static string GetRoleBodyPath(string prefabId)
        {
            if (ConfigMgr.GetInitConfig.TryGetValue("Body", $"Body0", out string value))
            {

                return value;
            }
            Debug.LogError($"--- {value} {prefabId}");
            return $"{ResMgr.RESDIR}/Role/Body/{prefabId}.prefab";
        }

        public static string GetIdRolePath(RoleType roleType, int prefabId)
        {
            return $"{ResMgr.RESDIR}/Role/{roleType}/{roleType.ToShortName()}_{prefabId}.prefab";
        }


        public static string GetHitEffectPath(int index)
        {
            return $"{HitEffectDir}/Hit_{index}.prefab";
        }
        public static string GetAudioPath(string name)
        {
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

}