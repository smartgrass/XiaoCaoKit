namespace XiaoCao
{
    public class IdMgr : Singleton<IdMgr>
    {
        private int value = 0;
        public int GenOne()
        {
            this.value++;
            return value;
        }

        public static int GenId()
        {
           return Inst.GenOne();
        }
    }
}