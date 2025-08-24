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

        public static bool IsMobilePlatform
        {

            get
            {
#if UNITY_ANDROID
                //UNITY_ANDROID & Offline则模拟移动平台
                if (GetEPlayMode() == EPlayMode.OfflinePlayMode)
                {
                    return true;
                }

                Debug.Log($"--- android-editor");
#endif
                return Application.isMobilePlatform;
            }
        }

        public static bool IsNeedUnCompressedZip
        {
            get
            {
                //判断playerPrefr
                string time = PlayerPrefs.GetString(LocalizeKey.BuildTime, "");

                if (ConfigMgr.StaticSettingSo.buildTime != time)
                {
                    return true;
                }

                if (!FileTool.IsFileExist(XCPathConfig.GetGameConfigDir()))
                {
                    Debug.Log($"--- no file Exist {XCPathConfig.GetGameConfigDir()}");
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
                    if (ConfigMgr.MainCfg.GetValue("Setting", "DebugLog", "0") == "1")
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

        //Demo专用配置
        public static bool IsSkillEditor
        {
            get => SceneManager.GetActiveScene().name.StartsWith("SkillEditor");
        }

        public static GameVersionType GetGameVersionType => GameSetting.VersionType;

        public static int PauseFrame;
    }

    public static class DebugCostTime
    {
        //分配不同表,尽量同时不占用
        public static List<Stopwatch> stopwatchs = new List<Stopwatch>(){
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