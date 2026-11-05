using System.Collections.Generic;
using TEngine;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class ActiveByMapMsg : MonoExecute, IMapMsgReceiver
    {
        public GameObject target;

        public string mapMsg;

        public bool isActive = true;

        public bool activechild = true;


        private void Start()
        {
            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReceiveMsg);
        }

        public override void Execute()
        {
            if (target)
            {
                target.SetActive(isActive);
            }

            if (activechild)
            {
                //激活子物体
                int childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    child.gameObject.SetActive(isActive);
                }
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