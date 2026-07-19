using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    //Home界面启动
    public class CheckGameInit : MonoBehaviour
    {
        public bool loadPlayer;

        private void Awake()
        {
            Init().Forget();
        }

        private async UniTask Init()
        {
            await ProcedureMgr.Inst.InitOnce().Run();
            MapMgr.CurLevelName = "Home";
            GameDataCommon.Current.gameStage = EGameStage.Home;
            if (loadPlayer)
            {
                ProcedureMgr.Inst.AddTask(new MapProcedure());
                ProcedureMgr.Inst.AddTask(new PlayerProcedure());
                ProcedureMgr.Inst.AddTask(new ToRunningStateProcedure());
                await ProcedureMgr.Inst.Run();
            }
        }
    }
}