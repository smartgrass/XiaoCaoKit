using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class RunDemo
    {
        public async void Run()
        {

            //这里做点编辑器开关
            //Debuger.LogLevel = LogLevel.Info;
            Debuger.LogLevel = LogLevel.Error;

            //初始化
            await InitYooAsset();

            Debuger.Log("==== InitYooAsset Finish ====");

            //版本判断 无


            //玩家数据
            LoadPlayerData();

            GameMgr.Inst.SetGameState(GameState.Running);

            GameEvent.Send(EventType.GameStartFinsh.Int());

            //ui在游戏启动后才执行
            try
            {
                var uiMgr = UIMgr.Inst;
            }
            catch (Exception e)
            {
                Debug.LogError($"--- uiMgr");
            }



        }

        private void LoadPlayerData()
        {
            Debuger.Log($" LoadPlayerData");

            Player0 player = EntityMgr.Inst.CreatEntity<Player0>();

            //读取存档数据
            var data0 = SavaMgr.ReadData<PlayerSaveData>(out bool isSuc);

            GameData.playerSaveData = data0;

            player.Init(data0, true);

            if (!isSuc)
            {
                Debuger.Log($"--- creat newData");
                SavaMgr.SavaData(data0);
            }



            //...

        }

        public async Task InitYooAsset()
        {
            ResMgr.InitYooAsset();
            await ResMgr.InitDefaultPackage();
            await ResMgr.InitRawPackage();
            await ResMgr.InitExtraPackage();
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
