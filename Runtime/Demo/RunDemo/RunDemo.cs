using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XiaoCao
{

    public class RunDemo
    {
        public async UniTask Run()
        {
            if (DebugSetting.IsMobilePlatform)
            {
                Debug.Log($"--- GetStreamingAssetsPath {XCPathConfig.GetStreamingAssetsPath()}");
                Debug.Log($"--- GetGameConfigDir {XCPathConfig.GetGameConfigDir()}");
                Debug.Log($"--- GetExtraPackageDir {XCPathConfig.GetExtraPackageDir()}");

                if (DebugSetting.IsNeedUnCompressedZip)
                {
                    Debug.Log($"--- NeedUnCompressedZip");
                    await UnCompressedZip();
                }
            }

            OpenLogConsole();

            if (Application.isEditor)
            {
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

        public async UniTask UnCompressedZip()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Debuger.Log($"--- 开始解压");
            string fileName = "ExtraRes.zip";
            //string zipPath = XCPathConfig.GetExtraResZipPath();
            //WWW读取并复制
            DebugCostTime.StartTime();
            await FileTool.CopyStreamingAssetsFileToPersistentData(fileName);
            DebugCostTime.StopTime($"CopyStreamingAssetsFile {fileName}");

            string destPath = Path.Combine(Application.persistentDataPath, fileName);
            DebugCostTime.StartTime();
            await ZipHelper.ExtractZip(destPath, Application.persistentDataPath);
            DebugCostTime.StopTime($"ExtractZip {fileName}");

            //删除Zip
            File.Delete(destPath);
            
        }

        private static void OpenLogConsole()
        {
            DebugCostTime.StartTime();
            var prefab = Resources.Load<GameObject>("IngameDebugConsole");
            var con = GameObject.Instantiate(prefab);
            DebugCostTime.StopTime("OpenLogConsole");
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

