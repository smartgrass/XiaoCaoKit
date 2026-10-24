using System;
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
        PlayNextNorAck = 2,
        
        SetNoBusy = 10,
        SetUnMoveTime = 11,

        AddTag = 12,
        SetNoGravityTime = 13,
        [InspectorName("索敌")]
        AutoDirect = 14,
        [InspectorName("关闭身体间碰撞(undo)")]
        NoBodyCollision = 15,
        [InspectorName("CameraShake-int")]
        CameraShake = 16,
        [InspectorName("幻影-模版")]
        BodyPhantom = 17,
        [InspectorName("隐身(float time)")]
        HideRender = 18,
        [InspectorName("动画速度(float speed,undo 敌人不完备)")]
        AnimSpeed = 19,
        [InspectorName("无敌(string maskId)")]
        NoDamage = 20,
        [InspectorName("时停")]
        TheWorld = 21,
        [InspectorName("霸体时间(NoBreakTime)")]
        NoBreakTime = 22,
        SetUnRotate = 23,
        [InspectorName("脱离父节点")]
        ObjectUnFollow = 24,
        [InspectorName("减伤比例-float(0~1)")]
        DamageReduce = 25,


    }

    interface IMsgReceiver
    {
        void ReceiveMsg(EntityMsgType type, int fromId, object msg);
    }

    [Serializable]
    public struct BaseMsg
    {
        public float numMsg;
        public string strMsg;
        [HideInInspector]
        public int state;  //0 ,1 代表进入和退出
    }

    [Serializable]
    public struct XCEventMsg
    {
        public EntityMsgType msgType;
        public BaseMsg baseMsg;
        public bool isUndoOnFinish; //是否反转
    }
}
