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
            player.CreateGameObject();
            //...

        }



        public async Task InitYooAsset()
        {
            ResMgr.Inst.InitYooAsset();
            var loading = ResMgr.Inst.InitPackage();
            await loading.Task;
        }




    }

    public interface RunStep
    { 
    
    }

}
