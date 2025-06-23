using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

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
            string fileName = "ExtraRes.zip";
            //string zipPath = XCPathConfig.GetExtraResZipPath();
            //WWW读取并复制
            await FileTool.CopyStreamingAssetsFileToPersistentData(fileName);
            string destPath = Path.Combine(Application.persistentDataPath, fileName);

            await ZipHelper.ExtractZip(destPath, Application.persistentDataPath);

            //删除Zip
            File.Delete(destPath);  

            Debug.Log($"--- 解压完成 {Application.persistentDataPath}/{fileName} ");
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

