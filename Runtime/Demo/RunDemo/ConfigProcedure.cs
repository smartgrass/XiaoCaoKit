using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace XiaoCao
{
    public class ResProcedure : ProcedureBase
    {
        public override bool LoadOnlyOnce => true;

        public override void Start()
        {
            Run().Forget();
        }

        public async UniTask Run()
        {
            await ResMgr.InitYooAssetAll();
            IsFinish = true;
            Debuger.Log("==== InitYooAsset Finish ====");
        }
    }

    public class ConfigProcedure : ProcedureBase
    {
        public override bool LoadOnlyOnce => true;

        public override void Start()
        {
            Run().Forget();
        }

        public async UniTask Run()
        {
            if (DebugSetting.IsMobilePlatform)
            {
                Debug.Log($"-- MobilePlatform");
                Debug.Log($"--- GetStreamingAssetsPath {XCPathConfig.GetStreamingAssetsPath()}");
                Debug.Log($"--- GetGameConfigDir {XCPathConfig.GetGameConfigDir()}");
                Debug.Log($"--- GetExtraPackageDir {XCPathConfig.GetExtraPackageDir()}");

                if (DebugSetting.IsNeedUnCompressedZip)
                {
                    Debug.Log($"--- NeedUnCompressedZip");
                    await UnCompressedZip();
                }
            }

            if (Application.isEditor)
            {
                GameAllData.GameAllDataInit();
            }

            GameSetting.GetGameVersion();
            

            if (GameSetting.VersionType == GameVersionType.Debug)
            {
                OpenLogConsole();
            }
            
            LocalizeMgr localizeMgr = LocalizeMgr.Inst;
            ConfigMgr.Init();
            IsFinish = true;
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

    public class PlayerDataProcedure : ProcedureBase
    {
        public override bool LoadOnlyOnce => true;

        public override void Start()
        {
            Debuger.Log($" LoadPlayerData");
            //读取存档数据
            var data0 = SaveMgr.ReadData<PlayerSaveData>(out bool isSuc);

            data0.CheckNull();
            
            if (Application.isEditor && "IsKaiLe".GetKeyBool())
            {
                data0.lv = PlayerPrefs.GetInt("playerLv");
            }

            if (DebugSetting.IsSkillEditor &&
                ConfigMgr.MainCfg.TryGetValue("Setting", "PlayerStartLevel", out string lvStr))
            {
                if (!string.IsNullOrEmpty(lvStr))
                {
                    data0.lv = int.Parse(lvStr);
                }

                Debug.Log($"--- PlayerStartLevel {lvStr}");
            }
            else
            {
                Debug.Log($"--- scene {SceneManager.GetActiveScene().name}");
            }

            if (!isSuc)
            {
                Debuger.Log($"--- creat newData");
                SaveMgr.SaveData(data0);
            }

            GameAllData.playerSaveData = data0;

            IsFinish = true;
        }
    }


    public class PlayerProcedure : ProcedureBase
    {
        public override void Start()
        {
            LoadPlayerData();
            IsFinish = true;
        }

        private void LoadPlayerData()
        {
            //TODO 重复加载处理...
            if (IsReload)
            {
                //Entity 需要替换, 但同时 id保持不变         
            }

            int playerId = IsReload ? GameDataCommon.Current.LocalPlayerId : IdMgr.GetPlayerId();

            Player0 player = EntityMgr.Inst.CreatEntity<Player0>(playerId);

            var data0 = GameAllData.playerSaveData;

            player.Init(data0, true);
        }
    }

    public class PreLoadPoolProcedure : ProcedureBase
    {
        public override bool LoadOnlyOnce => true;

        public override void Start()
        {
            var inst = XCCommandBinder.Inst;
            var buff = BuffBinder.Inst;
            var pool = RunTimePoolMgr.Inst;
            DebugCostTime.StartTime(4);
            // Shader.WarmupAllShaders(); //
            DebugCostTime.StopTime("WarmupAllShaders",4);
            IsFinish = true;
        }
    }

    //标记加载完毕
    public class ToRunningStateProcedure : ProcedureBase
    {
        public override void Start()
        {
            MapMgr.Inst.SetPlayerStartPos();

            GameMgr.Inst.SetGameState(GameState.Running);
            
            BlackScreen.FadeOutAnim();

            GameEvent.Send(EGameEvent.GameStartFinsh.Int());

            if (!IsReload)
            {
                DelayRun().Forget();
            }

            IsFinish = true;

            var uiMgr = UIMgr.Inst;
        }

        public async UniTask DelayRun()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            var sound = SoundMgr.Inst;
            Debug.Log($"--- SoundMgr Init");
        }
    }

    public class MapProcedure : ProcedureBase
    {
        public override void Start()
        {
            Run().Forget();
        }

        public async UniTask Run()
        {
            await MapMgr.Inst.LoadLevelObject();
            IsFinish = true;
            Debuger.Log("==== MapProcedure Finish ====");
        }
    }
}