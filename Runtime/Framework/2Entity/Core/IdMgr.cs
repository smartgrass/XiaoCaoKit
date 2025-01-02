namespace XiaoCao
{
    public class IdMgr : Singleton<IdMgr>
    {
        private int value = 100;

        private int playerId = 0;

        public static int GenId()
        {
           return Inst.value++;
        }

        public static int GetPlayerId()
        {
            return Inst.playerId++;
        }
    }
}