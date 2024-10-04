using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    ///<see cref="HideState"/>
    ///<see cref="PreAtkState"/>
    /// AtkState 是一个有前摇 后摇的时间
    [CreateAssetMenu(fileName = "AtkState", menuName = "SO/AI/AtkState", order = 10)]
    public class AtkState : AIFSMBase
    {
        [XCLabel("攻击前摇")]
        public float beforeAtk = 0.5f;

        [XCLabel("攻击后摇")]
        public float afterAtk = 0; //攻击结束的后摇

        public string atkMsg ="0"; //信息

        public int atkState = 0;

        private float Timer { get; set; }

        private bool IsInited { get; set; }

        public override void OnStart()
        {
            State = FSMState.Update;
            Timer = 0;
            atkState = 0;
            if (!IsInited)
            {
                IsInited = true;
                control.owner.OnBreakAct += OnBreak;
            }
        }

        //如果攻击计时被打断时重置一下
        private void OnBreak()
        {
            if (State == FSMState.Update)
            {
                Timer = 0;
            }
        }

        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            Debug.Log($"--- AtkState OnUpdate {State}");
            Timer += Time.deltaTime;
            if (atkState == 0)
            {
                if (Timer > beforeAtk)
                {
                    AtkStart();
                    atkState++;
                    Timer = 0;
                }
            }
            else
            {
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
