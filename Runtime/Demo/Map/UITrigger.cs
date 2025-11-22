using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class UITrigger : MonoBehaviour
    {
        public UIPanelType type;

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

    public enum UIPanelType
    {
        Null = -1,
        LevelPanel = 0,
        SettingPanel = 1,
        PlayerPanel = 2,
        BuffSelectPanel = 11,  // 添加Buff选择面板类型
        DebugPanel = 99,
    }
}