namespace XiaoCao
{

    public class MiniTimer
    {
        public float total = 1;
        public float timer;

        public void SetTime(float t)
        {
            total = t;
            timer = total;
        }

        public bool CheckUpdate()
        {
            if (timer > 0)
            {
                timer -= XCTime.deltaTime;
            }
            return timer <= 0;
        }

        public bool IsFinish;

    }

}
