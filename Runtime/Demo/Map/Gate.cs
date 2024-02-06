using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    public class Gate : MonoBehaviour
    {
        int sceneId;
        
        [Button]
        private void Trigger()
        {
            //情况1. 完成关卡离开
            GameMgr.Inst.FinishLevel(sceneId);

            //2 进入关卡 ,显示选关界面
            UIMgr.Inst.ShowLevelSelectionView();


        }
    }
}
