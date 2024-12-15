using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace XiaoCao
{

    public class RunDemo
    {
        public async UniTask Run()
        {


            //这里做点编辑器开关
            Debuger.LogLevel = LogLevel.Info;

            ProcedureMgr procedureMgr = ProcedureMgr.Inst;

            procedureMgr.AddTask(new ResProcedure());
            procedureMgr.AddTask(new ConfigProcedure());
            procedureMgr.AddTask(new PreLoadPoolProcedure());
            procedureMgr.AddTask(new PlayerProcedure());
            procedureMgr.AddTask(new ToRunningStateProcedure());

            await procedureMgr.Run();
            Debug.Log($"--- RunDemo");
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

