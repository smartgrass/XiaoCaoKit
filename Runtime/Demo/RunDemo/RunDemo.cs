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
            //这里做点编辑器开关
            Debuger.LogLevel = LogLevel.Info;

            ProcedureMgr procedureMgr = ProcedureMgr.Inst.InitOnce();
            //LoadOnce;
            procedureMgr.AddTask(new PreLoadPoolProcedure());

            //Reload
            procedureMgr.AddTask(new MapProcedure());
            procedureMgr.AddTask(new PlayerProcedure());

            procedureMgr.AddTask(new ToRunningStateProcedure());

            await procedureMgr.Run();
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