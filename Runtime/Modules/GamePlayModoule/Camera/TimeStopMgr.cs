﻿using System.Collections;
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
            if (BattleData.IsTimeStop)
            {
                return;
            }
            BattleData.IsTimeStop = true;
            this.time = time;
            //1.敌人停止动画
            //2.修改runner的speed [每个敌人都执行]

            GameEvent.Send<bool>(EventType.TimeSpeedStop.Int(),true);
            coroutine = StartCoroutine(IEWaitRecover());
            CamEffectMgr.Inst.OpenEffect(CamEffectMgr.CameraEffect.TimeStop);
        }

        public void RecoverTimeSpeed() {
            if (BattleData.IsTimeStop)
            {
                StopCoroutine(coroutine);
                Recover();
            }
        }

        IEnumerator IEWaitRecover()
        {
            yield return new WaitForSeconds(time);
            Recover();
            Debug.Log($"--- 恢复");
        }
        private static void Recover()
        {
            GameEvent.Send<bool>(EventType.TimeSpeedStop.Int(), false);
            BattleData.IsTimeStop = false;
            CamEffectMgr.Inst.OpenEffect(CamEffectMgr.CameraEffect.None);
        }

    }

}

