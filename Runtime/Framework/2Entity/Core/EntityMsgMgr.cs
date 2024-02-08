using UnityEngine;

namespace XiaoCao
{
    public static class EntityMsgMgr
    {
        public static void SendMsg(int from, int to, int type, int msg)
        {


        }

        public static void EntityMsg(this IdComponent idComponent, EntityMsgType type, object msg = null)
        {
            if(EntityMgr.Inst.FindEntity(idComponent.id, out Entity entity))
            {
                //from
                entity.ReceiveMsg(type, idComponent.id, msg);
            }
        }

    }

    //使用TypeLaber标记消息类型
    public enum EntityMsgType
    {
        PlayNextSkill = 1,
        
        SkillFinish_Num = 10,
        SetUnMoveTime = 11,
        AddTag = 12,


    }

    interface IMsgReceiver
    {
        void ReceiveMsg(EntityMsgType type, int fromId, object msg);
    }


    public struct BaseMsg
    {
        public float numMsg;
        public string strMsg;
        [HideInInspector]
        public int state;  //0 ,1
    }

    public struct XCEventMsg
    {
        public EntityMsgType msgType;
        public BaseMsg baseMsg;
        public bool isUndoOnFinish; //是否反转
    }
}