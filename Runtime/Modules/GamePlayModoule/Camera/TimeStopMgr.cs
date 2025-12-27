using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class TimeStopMgr : MonoSingleton<TimeStopMgr>, IMgr
    {
        private float timer;

        public Coroutine coroutine;

        public void StopTimeSpeed(float time = 5)
        {
            if (BattleData.IsTimeStop)
            {
                timer = Mathf.Max(time, timer);
                return;
            }

            timer = time;
            BattleData.IsTimeStop = true;
            //1.敌人停止动画
            //2.修改runner的speed [每个敌人都执行]

            GameEvent.Send<bool>(EGameEvent.TimeSpeedStop.ToInt(), true);
            coroutine = StartCoroutine(IEWaitRecover());
            CamEffectMgr.Inst.SetOpenEffect(CamEffectMgr.CameraEffect.TimeStop);
            XCTime.timeScale = 2;
        }

        public void RecoverTimeSpeed()
        {
            if (BattleData.IsTimeStop)
            {
                StopCoroutine(coroutine);
                Recover();
            }
        }

        IEnumerator IEWaitRecover()
        {
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            Recover();
            Debug.Log($"--- 恢复");
        }

        private static void Recover()
        {
            GameEvent.Send<bool>(EGameEvent.TimeSpeedStop.ToInt(), false);
            BattleData.IsTimeStop = false;
            CamEffectMgr.Inst.SetOpenEffect(CamEffectMgr.CameraEffect.TimeStop, false);
            XCTime.timeScale = 1;
        }

        public static HashSet<string> uiStopTimeTag = new HashSet<string>();

        public static void UIStopTime(bool isOn, string tag)
        {
            if (isOn)
            {
                uiStopTimeTag.Add(tag);
                Time.timeScale = 0;
            }
            else
            {
                uiStopTimeTag.Remove(tag);
                if (uiStopTimeTag.Count == 0)
                {
                    Time.timeScale = 1;
                }
            }
        }
    }
}