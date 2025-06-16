using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{

    public class RunDemo
    {
        public async UniTask Run()
        {

            if (DebugSetting.IsDebug)
            {
                var prefab = Resources.Load<GameObject>("IngameDebugConsole");
                var con = GameObject.Instantiate(prefab);
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

