using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    public class CheckGameInit : MonoBehaviour
    {
        private void Awake()
        {
            Init().Forget();
        }

        private async UniTask Init()
        {
            await ProcedureMgr.Inst.InitOnce().Run();
        }
    }
}