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
        [TypeLabel(typeof(int))]
        StartSkill
    }

    interface IMsgReceiver
    {
        void ReceiveMsg(EntityMsgType type, int fromId, object msg);
    }
}