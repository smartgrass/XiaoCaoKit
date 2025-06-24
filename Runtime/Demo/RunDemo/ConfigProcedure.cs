using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            LocalizeMgr localizeMgr = LocalizeMgr.Inst;
            ConfigMgr.Init();
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
            Debuger.Log($" LoadPlayerData");
            //TODO 重复加载处理...
            if (IsReload)
            {
                //Entity 需要替换, 但同时 id保持不变         
            }

            int playerId = IsReload ? GameDataCommon.Current.LocalPlayerId : IdMgr.GetPlayerId();

            Player0 player = EntityMgr.Inst.CreatEntity<Player0>(playerId);

            //读取存档数据
            var data0 = SaveMgr.ReadData<PlayerSaveData>(out bool isSuc);

            data0.CheckNull();

            //data0.prefabId

            if (Application.isEditor && "IsKaiLe".GetKeyBool())
            {
                data0.lv = PlayerPrefs.GetInt("playerLv");
            }

            if (DebugSetting.IsSkillEditor && ConfigMgr.MainCfg.TryGetValue("Setting", "PlayerStartLevel", out string lvStr))
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

            GameAllData.playerSaveData = data0;

            player.Init(data0, true);

            if (!isSuc)
            {
                Debuger.Log($"--- creat newData");
                SaveMgr.SaveData(data0);
            }
            //...
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

