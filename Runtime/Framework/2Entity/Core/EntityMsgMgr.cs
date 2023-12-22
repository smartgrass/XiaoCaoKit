namespace XiaoCao
{
    public static class EntityMsgMgr
    {
        public static void SendMsg(int from, int to, int type, int msg)
        {


        }

        public static void EntityMsg(this IdComponent idComponent, EntityMsgType type, object msg = null)
        {
            if(EntityMgr.Instance.FindEntity(idComponent.id, out Entity entity))
            {
                //from
                entity.ReceiveMsg(type, idComponent.id, msg);
            }
        }

    }


    public enum EntityMsgType
    {
        Skill
    }

    interface IMsgReceiver
    {
        void ReceiveMsg(EntityMsgType type, int fromId, object msg);
    }
}