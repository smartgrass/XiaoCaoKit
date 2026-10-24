using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Debug = UnityEngine.Debug;

namespace XiaoCao
{
    public static class DebugSetting
    {
        public static readonly string DebugGUI_IsShow = "DebugGUI_IsShow";

        //正式环境 | 编辑器模拟正式环境 = OfflinePlayMode + ANDROID
        public static bool IsMobileOffice
        {
            get
            {
#if UNITY_ANDROID
                if (GetEPlayMode() == YooAsset.EPlayMode.OfflinePlayMode)
                {
                    return true;
                }
#endif
                return Application.isMobilePlatform;
            }
        }

        public static bool IsNeedUnCompressedZip
        {
            get
            {
                string time = LocalizeKey.BuildTime.GetKeyString();

                if (!FileTool.IsFileExist(XCPathConfig.GetGameConfigDir()))
                {
                    Debug.Log($"--- no file Exist {XCPathConfig.GetGameConfigDir()}");
                    return true;
                }

                if (ConfigMgr.StaticSettingSo.buildTime != time)
                {
                    Debuger.Log($"--- buildTime: {ConfigMgr.StaticSettingSo.buildTime} != {time}");
                    return true;
                }

                return false;
            }
        }

        //TODO Debug分级
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
                    if (ConfigMgr.Inst.MainCfg.GetValue("Setting", "DebugLog", "0") == "1")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 读取当前资源运行模式。
        /// 工具栏存的是模式索引(0=编辑器模拟, 1=单机)，而 v3 的 EPlayMode 在 0 位插入了 None，
        /// 直接强转会错位（0 变成 None，导致初始化参数为空），这里按索引显式映射。
        /// </summary>
        public static EPlayMode GetEPlayMode()
        {
#if UNITY_EDITOR
            int index = UnityEditor.EditorPrefs.GetInt("EditorResourceMode", 0);
            switch (index)
            {
                case 0: return EPlayMode.EditorSimulateMode;
                case 1: return EPlayMode.OfflinePlayMode;
                case 2: return EPlayMode.HostPlayMode;
                default: return EPlayMode.EditorSimulateMode;
            }
#else
            return EPlayMode.OfflinePlayMode;
#endif
        }

        public static GameVersionType GetGameVersionType => GameSetting.VersionType;

        public static int PauseFrame;
    }

    public static class DebugCostTime
    {
        //分配不同表,尽量同时不占用
        public static List<Stopwatch> stopwatchs = new List<Stopwatch>()
        {
            new Stopwatch(),
            new Stopwatch(),
            new Stopwatch(),
            new Stopwatch(),
            new Stopwatch(),
        };


        public static void StartTime(int index = 0)
        {
            if (stopwatchs[index].IsRunning)
            {
                Debug.LogError($"--- stopwatch {index} IsRunning");
            }

            stopwatchs[index].Restart();
        }

        public static void StopTime(string logMsg, int index = 0)
        {
            stopwatchs[index].Stop();
            Debug.Log($"--- cost time {logMsg} {stopwatchs[index].ElapsedMilliseconds}ms");
            stopwatchs[index].Reset();
        }
    }
}