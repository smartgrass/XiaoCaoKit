using TEngine;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class ActiveByMapMsg : MonoExecute, IMapMsgReceiver
    {
        public string mapMsg;
        
        public bool isActive = true;

        public bool autoStartState = true;
        
        private void Start()
        {
            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
            if (autoStartState)
            {
                gameObject.SetActive(!isActive);
            }
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
        }
        
        public override void Execute()
        {
            gameObject.SetActive(isActive);
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