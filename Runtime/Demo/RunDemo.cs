#define EnableLog	
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class RunDemo
    {
        public async UniTask Run()
        {

            //这里做点编辑器开关
            //Debuger.LogLevel = LogLevel.Info;
            Debuger.LogLevel = LogLevel.Error;

            if (Application.isEditor)
            {
                GameData.Init();     
            }

            if (DebugSetting.IsDebug)
            {
                var prefab = Resources.Load<GameObject>("IngameDebugConsole");
                var con = GameObject.Instantiate(prefab);
            }

            Debug.Log($"--- PreLoad Run {Time.frameCount} {Thread.CurrentThread.ManagedThreadId}");

            //初始化
            await ResMgr.InitYooAssetAll();

            Debuger.Log("==== InitYooAsset Finish ====");

            PreLoad();
            //版本判断 无

            //玩家数据
            LoadPlayerData();

            LoadTextConfig();

            GameMgr.Inst.SetGameState(GameState.Running);

            GameEvent.Send(EventType.GameStartFinsh.Int());


            //ui在游戏启动后才执行
            try
            {
                var uiMgr = UIMgr.Inst;
            }
            catch (Exception e)
            {
                Debug.LogError($"--- uiMgr {e}");
            }


            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            var sound = SoundMgr.Inst;

        }

        public void PreLoad()
        {
            var inst = CommandFinder.Inst;
            XCTaskRunner.PreInitPool();
            var pool = RunTimePoolMgr.Inst;
        }


        private void LoadTextConfig()
        {
            ConfigMgr.Init();
//#if UNITY_EDITOR
//            LocalizeMgr.ClearCache();
//#endif
            LocalizeMgr localizeMgr = LocalizeMgr.Inst;
        }

        private void LoadPlayerData()
        {
            Debuger.Log($" LoadPlayerData");

            Player0 player = EntityMgr.Inst.CreatEntity<Player0>();

            //读取存档数据
            var data0 = SaveMgr.ReadData<PlayerSaveData>(out bool isSuc);

            data0.CheckNull();

            data0.prefabId = 0;

            if (Application.isEditor && "IsKaiLe".GetKeyBool())
            {
                data0.lv = PlayerPrefs.GetInt("playerLv");
            }

            GameData.playerSaveData = data0;

            player.Init(data0, true);

            if (!isSuc)
            {
                Debuger.Log($"--- creat newData");
                SaveMgr.SaveData(data0);
            }



            //...

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

