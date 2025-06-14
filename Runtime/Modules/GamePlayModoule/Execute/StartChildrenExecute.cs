using Cysharp.Threading.Tasks;
using DG.Tweening.Core;
using UnityEngine;
using XiaoCao;

public class StartChildrenExecute : GameStartMono
{
    public override void OnGameStart()
    {
        base.OnGameStart();
        ExecuteHelper.DoExecuteInChildren(transform); 
    }
}
