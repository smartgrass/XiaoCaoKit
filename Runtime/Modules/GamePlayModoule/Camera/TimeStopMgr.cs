using System.Collections;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class TimeStopMgr : MonoSingleton<TimeStopMgr>, IMgr
    {
        public float time;

        public Coroutine coroutine;

        public void StopTimeSpeed(float time)
        {
            if (BattleData.Current.IsTimeStop)
            {
                return;
            }
            BattleData.Current.IsTimeStop = true;
            this.time = time;
            //1.敌人停止动画
            //2.修改runner的speed [每个敌人都执行]

            GameEvent.Send<bool>(EventType.TimeSpeedStop.Int(),true);
            coroutine = StartCoroutine(IEWaitRecover());
        }

        public void RecoverTimeSpeed() {
            if (BattleData.Current.IsTimeStop)
            {
                StopCoroutine(coroutine);
                GameEvent.Send<bool>(EventType.TimeSpeedStop.Int(), false);
                BattleData.Current.IsTimeStop = false;
            }
        }

        IEnumerator IEWaitRecover()
        {
            yield return new WaitForSeconds(time);
            GameEvent.Send<bool>(EventType.TimeSpeedStop.Int(), false);
            BattleData.Current.IsTimeStop = false;
            Debug.Log($"--- 恢复");
        }

    }

}


