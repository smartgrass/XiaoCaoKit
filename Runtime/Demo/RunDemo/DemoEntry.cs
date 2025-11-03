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

        procedureMgr.InitOnce();

        await procedureMgr.Run();

        if (GameAllData.playerSaveData.IsNewPlayer)
        {
            GameMgr.Inst.LoadLevelScene(MapNames.Level1);
            Debug.Log($"-- new player");
        }
        else
        {
            GameMgr.Inst.LoadScene(SceneNames.Home);
        }
    }

    // [Obsolete]
    // private async FTask StartAsync()
    // {
    //     // Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
    //     // scene = await Fantasy.Platform.Unity.Entry.CreateScene();
    // }
}