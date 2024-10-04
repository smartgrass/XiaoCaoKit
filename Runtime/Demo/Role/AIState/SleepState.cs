using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/AI/SleepState", order = 10)]
    public class SleepState: AIFSMBase
    {
        public float sleepTime = 2;

        public float timer;

        public override void OnStart()
        {
            timer = sleepTime;
        }
        public override void OnUpdate()
        {
            if (timer > 0)
            {
                timer -= XCTime.deltaTime;
            }
            if (timer <= 0)
            {
                State = FSMState.Finish;
                OnExit();
            }
        }
        public override void OnExit()
        {
            State = FSMState.None;
        }
    }
}
