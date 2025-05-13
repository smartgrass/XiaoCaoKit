using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    ///<see cref="IdleState"/>
    ///<see cref="PreAtkState"/>
    /// AtkState 是一个有前摇 后摇的时间
    [CreateAssetMenu(fileName = "AtkState", menuName = "SO/AI/AtkState", order = 1)]
    public class AtkState : AIFSMBase
    {
        [XCLabel("攻击前摇")]
        public float beforeAtk = 0.5f;

        [XCLabel("攻击后摇")]
        public float afterAtk = 0; //攻击结束的后摇
        [XCLabel("攻击信息")]
        public string atkMsg = "0"; //信息

        [XCLabel("受击计时延迟")]
        public float damageInterrupt = 0.1f;

        [XCLabel("前摇索敌")]
        public bool isAutoLock = true;

        private float Timer { get; set; }
        private bool IsInited { get; set; }

        private int curAtkState = 0;

        private int lastDamageFrame = 0;


        public override void OnStart()
        {
            State = FSMState.Update;
            Timer = 0;
            curAtkState = 0;
            if (!IsInited)
            {
                IsInited = true;
                control.owner.OnDamageAct += OnDamage;
            }
        }

        private void OnDamage(AtkInfo info, bool isBreak)
        {
            if (State == FSMState.Update)
            {
                return;
            }

            if (isBreak)
            {
                //如果攻击计时被打断时重置一下
                Timer = 0;
            }
            else
            {
                //普通受击会小幅度减少攻击计时
                if (lastDamageFrame != Time.frameCount)
                {
                    Timer -= damageInterrupt;
                    lastDamageFrame = Time.frameCount;
                }
            }
        }




        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (curAtkState == 0)
            {
                Timer += Time.deltaTime;
                if (isAutoLock)
                {
                    control.Lock(true);
                    //control.owner.AISetLookTarget(TargetRole.transform);
                    ////control.owner.AIMsg(ActMsgType.AutoLock, atkMsg);

                }
                if (Timer > beforeAtk)
                {
                    AtkStart();
                    curAtkState++;
                    Timer = 0;
                }
            }
            else
            {
                if (control.IsBusy())
                {
                    return;
                }

                Timer += Time.deltaTime;
                if (Timer > afterAtk)
                {
                    OnExit();
                }
            }
        }


        private void AtkStart()
        {
            control.owner.AIMsg(ActMsgType.Skill, atkMsg);
        }
    }
}
