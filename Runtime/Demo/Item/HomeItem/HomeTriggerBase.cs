using TEngine;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public abstract class HomeTriggerBase : MonoBehaviour, IMapMsgSender
    {
        public string uiKey;
        public Vector2 uiOffset = new Vector2(0, 100);
        private bool hasAddListener = false;


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
            UIMgr.Inst.battleHud.ShowTriggerItemTip(uiKey, transform.position, uiOffset);
            GameEvent.AddEventListener<string>(EGameEvent.HomeItemMsg.ToInt(), ReceiveMsg);
            hasAddListener = true;
        }

        private void OnTriggerExit(Collider other)
        {
            //隐藏ui
            UIMgr.Inst.battleHud.HideTriggerItemTip();
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

        //确认交互
        public virtual void TriggerSucceed()
        {
        }
    }
}