using System;
using TEngine;
using TMPro;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public abstract class HomeTriggerBase : MonoBehaviour, IMapMsgSender
    {
        public string uiKey;
        public float offsetY = 1.5f;
        private bool hasAddListener = false;
        private HomeTriggerNameText nameTextHelper;
        protected TMP_Text nameText => nameTextHelper?.Text;


        public virtual void OnEnable()
        {
            InitNameRoot();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tags.PLAYER))
            {
                return;
            }

            var idRole = other.GetComponent<IdRole>();
            if (!idRole)
            {
                return;
            }

            var player = idRole.GetEntity();

            if (GameDataCommon.LocalPlayer != player)
            {
                return;
            }

            //显示ui
            UIMgr.Inst.battleHud.ShowTriggerItemTip(uiKey, transform, offsetY);
            nameTextHelper.ShowUI();
            GameEvent.AddEventListener<string>(EGameEvent.HomeItemMsg.ToInt(), ReceiveMsg);
            hasAddListener = true;
        }

        private void OnTriggerExit(Collider other)
        {
            //隐藏ui
            UIMgr.Inst.battleHud.HideTriggerItemTip();
            nameTextHelper.HideUI();
            if (hasAddListener)
            {
                GameEvent.RemoveEventListener<string>(EGameEvent.HomeItemMsg.ToInt(), ReceiveMsg);
                hasAddListener = false;
            }
        }

        public void ReceiveMsg(string msg)
        {
            if (uiKey == msg)
            {
                TriggerSucceed();
            }
        }

        private void InitNameRoot()
        {
            nameTextHelper ??= new HomeTriggerNameText(transform, uiKey);
            nameTextHelper.Ensure(offsetY);
        }


        //确认交互
        public virtual void TriggerSucceed()
        {
        }
    }
}