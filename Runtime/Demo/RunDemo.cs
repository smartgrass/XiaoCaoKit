using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    public class RunDemo
    {
        public async void Run()
        {
            Debug.Log($"yns Run");
            //初始化
            await InitYooAsset();

            //版本判断 无

            //玩家数据
            LoadPlayerData();
        }

        private void LoadPlayerData()
        {
            Debug.Log($"yns LoadPlayerData");

            Player0 player = EntityMgr.Instance.CreatEntity<Player0>();

            //SavaMgr
            player.Init(new PlayerData0());
            //...

        }



        public async Task InitYooAsset()
        {
            ResMgr.InitYooAsset();
            await ResMgr.InitPackage().Task;
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
