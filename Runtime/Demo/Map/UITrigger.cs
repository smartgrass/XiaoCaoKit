using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    public class UITrigger : MonoBehaviour
    {
        public TriggerUIType type;

        [Button]
        private void Trigger()
        {
            //情况1. 完成关卡离开
            //GameMgr.Inst.FinishLevel(sceneId);

            //2 进入关卡 ,显示选关界面
            UIMgr.Inst.ShowLevelView();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tags.PLAYER))
            {
                if (type == TriggerUIType.LevelPanel)
                {
                    Trigger();
                }
            }
        }
    }

    public enum TriggerUIType
    {
        LevelPanel,
    }

}
