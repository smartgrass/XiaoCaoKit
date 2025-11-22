using TEngine;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class ActiveByMapMsg : MonoExecute, IMapMsgReceiver
    {
        public string mapMsg;
        
        public bool isActive = true;
        
        private void Start()
        {
            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.Int(), OnReceiveMsg);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.Int(), OnReceiveMsg);
        }
        
        public override void Execute()
        {
            //激活子物体
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(isActive);
            }
        }

        public void OnReceiveMsg(string receiveMsg)
        {
            if (string.IsNullOrEmpty(mapMsg))
            {
                return;
            }
            if (mapMsg == receiveMsg)
            {
                Execute();
            }
        }
    }
}