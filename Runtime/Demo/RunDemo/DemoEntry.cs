using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using NaughtyAttributes;
using TMPro;
using XiaoCao;

public class DemoEntry : MonoBehaviour
{
    private bool isInit = false;

    public GameObject touchText;
    public TMP_Text loadingText;

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void OnEnterBtn()
    {
        if (isInit)
        {
            return;
        }

        touchText.SetActive(false);
        loadingText.gameObject.SetActive(true);

        StartCoroutine(LoadingAnim());
        OnEnterBtnAsync();
        isInit = true;
    }

    async UniTask OnEnterBtnAsync()
    {
        ProcedureMgr procedureMgr = ProcedureMgr.Inst;

        procedureMgr.InitOnce();

        await procedureMgr.Run();

        UpdateLoadingText();
        
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

    IEnumerator LoadingAnim()
    {
        var wait = new WaitForSeconds(0.1f);
        while (!ProcedureMgr.Inst.isFinish)
        {
            UpdateLoadingText();
            yield return wait;
        }
    }

    private void UpdateLoadingText()
    {
        float process = ProcedureMgr.Inst.GetProcess();
        loadingText.text = $"加载中...{process * 100:f0}%";
    }
}