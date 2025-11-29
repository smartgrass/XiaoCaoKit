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

        public static readonly string OtherShowing = "DebugGUI/IsOtherShowing";

        //正式环境 | 编辑器模拟正式环境 = OfflinePlayMode + ANDROID
        public static bool IsMobileOffice
        {
            get
            {
#if UNITY_ANDROID
                if (GetEPlayMode() == EPlayMode.OfflinePlayMode)
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

        public static EPlayMode GetEPlayMode()
        {
            EPlayMode playMode = EPlayMode.OfflinePlayMode;
#if UNITY_EDITOR
            playMode = (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorResourceMode");
#else
        playMode = EPlayMode.OfflinePlayMode;
#endif

            return playMode;
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