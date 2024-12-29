namespace XiaoCao.Buff
{
    public class LoopTimer
    {
        public LoopTimer(float period)
        {
            Period = period;
        }
        public float Timer { get; private set; }

        public float Period;

        public void TickPeriodic(float deltaTime, out bool executePeriodicTick)
        {
            this.Timer -= deltaTime;
            executePeriodicTick = false;
            if (this.Timer <= 0)
            {
                this.Timer = this.Period;
                if (Period > 0)
                {
                    executePeriodicTick = true;
                }
            }
        }

        public void Reset()
        {
            Timer = 0;
        }
    }

}
