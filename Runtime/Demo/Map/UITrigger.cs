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
            UIMgr.Inst.ShowView(type);
        }
        [Button]
        private void TriggerExit()
        {
            UIMgr.Inst.HideView(type);

        }
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"--- {other}");
            if (other.CompareTag(Tags.PLAYER))
            {
                Trigger();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            TriggerExit();
        }

    }

    public enum TriggerUIType
    {
        LevelPanel,
    }

}
