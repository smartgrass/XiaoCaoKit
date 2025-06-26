using UnityEngine;

namespace XiaoCao
{
    //与ActiveObject的功能相似...后面再考虑扩展
    public class XCCommand_ShowAtkWarmingByMoveToPos : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        public float minSwitchTime = 0.8f;

        private GameObject createObject;

        private string PrefabPath; //"Assets/_Res/SkillPrefab/Player/atk_warming_circle.prefab";

        public void Init(BaseMsg baseMsg)
        {
            PrefabPath = baseMsg.strMsg;
        }

        public void OnFinish(bool hasTrigger)
        {

            if (createObject)
            {
                createObject.gameObject.SetActive(false);
                PoolMgr.Inst.Release(PrefabPath, createObject);
            }
        }

        public void OnTrigger()
        {


            var moveEvents = curEvent.task._events.FindAll(x => x.GetType() == typeof(XCMoveEvent));

            foreach (var item in moveEvents)
            {
                //默认查找 第一最近的MoveEvent
                if (item.Start >= curEvent.Start)
                {
                    CreteWarming(item as XCMoveEvent);
                    return;
                }
            }

        }
        void CreteWarming(XCMoveEvent moveEvent)
        {
            var msg = curEvent.baseMsg;
            createObject = PoolMgr.Inst.GetOrCreatPool(PrefabPath).Get();
            createObject.transform.position = moveEvent.GetEndWoldPos();
            createObject.transform.localScale = Vector3.one * msg.numMsg;
        }



        public void OnUpdate(int frame, float timeSinceTrigger) { }

        public bool IsTargetRoleType(RoleType roleType)
        {
            return true;
        }
    }
}
