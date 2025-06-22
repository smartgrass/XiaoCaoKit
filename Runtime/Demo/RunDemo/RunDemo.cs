using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{

    public class RunDemo
    {
        public async UniTask Run()
        {
#if PLATFORM_ANDROID
            OpenLogConsole();
            Debug.Log($"--- GetStreamingAssetsPath {XCPathConfig.GetStreamingAssetsPath()}");
            Debug.Log($"--- GetGameConfigDir {XCPathConfig.GetGameConfigDir()}");
            Debug.Log($"--- GetExtraPackageDir {XCPathConfig.GetExtraPackageDir()}");
            //await Task.Delay((int)3 * 1000);
#endif
            try
            {
                if (Application.isEditor) {
                    GameAllData.GameAllDataInit();
                }
                GameSetting.GetGameVersion();
                if (DebugSetting.IsDebug)
                {
                    //OpenLogConsole();
                }
                //这里做点编辑器开关
                Debuger.LogLevel = LogLevel.Info;

                ProcedureMgr procedureMgr = ProcedureMgr.Inst;
                //LoadOnce
                procedureMgr.AddTask(new ConfigProcedure());
                procedureMgr.AddTask(new ResProcedure());

                procedureMgr.AddTask(new PreLoadPoolProcedure());
                //Reload
                procedureMgr.AddTask(new MapProcedure());
                procedureMgr.AddTask(new PlayerProcedure());

                procedureMgr.AddTask(new ToRunningStateProcedure());

                await procedureMgr.Run();
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            Debug.Log($"--- RunDemo");
        }

        private static void OpenLogConsole()
        {
            var prefab = Resources.Load<GameObject>("IngameDebugConsole");
            var con = GameObject.Instantiate(prefab);
        }
    }

    public enum RunState
    {
        InitLoadRes,
        PlayerConfig,
        LoadScene,
        LoadPlayer
    }

    public interface RunStep
    {
    }

}

