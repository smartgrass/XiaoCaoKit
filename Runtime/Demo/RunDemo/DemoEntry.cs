using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using NaughtyAttributes;
using XiaoCao;

public class DemoEntry : MonoBehaviour
{

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void OnEnterBtn()
    {
        OnEnterBtnAsync();
    }

    async UniTask OnEnterBtnAsync()
    {
        //读取配置, 判断是否进入新手剧情
        ProcedureMgr procedureMgr = ProcedureMgr.Inst;
        //LoadOnce
        procedureMgr.AddTask(new ConfigProcedure());
        procedureMgr.AddTask(new PlayerDataProcedure());
        
        await procedureMgr.Run();

        MapMgr.Inst.MapName = MapNames.Level0;
        
        GameMgr.Inst.LoadScene(SceneNames.Level);
        

        if (GameAllData.playerSaveData.IsNewPlayer)
        {
            Debug.Log($"-- new player");
        }
    }

    // [Obsolete]
    // private async FTask StartAsync()
    // {
    //     // Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
    //     // scene = await Fantasy.Platform.Unity.Entry.CreateScene();
    // }
}