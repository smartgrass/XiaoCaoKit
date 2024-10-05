using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(fileName = "SleepState", menuName = "SO/AI/SleepState", order = 1)]
    public class SleepState : AIFSMBase, ITimeState
    {
        public bool getFromSetting = true;

        private float totalTime = 2;

        public float Timer { get; set; }
        public float TotalTime { get => totalTime; set => totalTime = value; }

        public override void OnStart()
        {
            if (getFromSetting)
            {
                totalTime = setting.sleepTime;
            }
            Timer = TotalTime;
            State = FSMState.Update;
        }
        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (Timer > 0)
            {
                Timer -= XCTime.deltaTime;
            }
            if (Timer <= 0)
            {
                OnExit();
            }
        }
    }

    public interface ITimeState
    {
        public float TotalTime { get; set; }
    }
}
