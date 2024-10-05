using System;
using UnityEngine;

namespace XiaoCao
{

    public class AIFSMBase : ScriptableObject, IFSM
    {
        //SFM层转换需要自身管理
        public FSMState State { get; set; }

        public AIControl control { get; set; }

        public Transform transform => control.transform;

        public Role TargetRole => control.TargetRole;

        public FSMSetting setting => control.mainDataFSM.setting;

        public bool HasTarget => control.HasTarget;

        public float curDistance => control.curDistance;

        public bool IsInstance { get; set; }

        public void InitReset(AIControl control)
        {
            this.control = control;
            ResetFSM();
        }

        public void ResetFSM()
        {
            State = FSMState.None;
        }

        public virtual void OnExit()
        {
            State = FSMState.Finish;
        }

        public virtual void OnStart()
        {
            State = FSMState.Update;
        }

        public virtual void OnUpdate()
        {
            throw new System.NotImplementedException();
        }
    }


    public enum FSMState
    {
        None,
        Update,
        Finish,
    }

    public interface IFSM
    {
        public void OnStart();
        public void OnUpdate();
        public void OnExit();

        public void ResetFSM();
    }
}
