using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 行为池
    /// </summary>
    [CreateAssetMenu(fileName = "ActPoolFSM", menuName = "SO/AI/ActPoolFSM", order = 10)]
    public class ActPoolFSM : MainFSM
    {
        public SleepState SleepState;
        //行为池有 概率,次数
        public List<SubStateData> pool = new List<SubStateData>() { new SubStateData() };


        public AIFSMBase CurState { get; set; }
        public List<SubStateData> PoolInst { get; set; }
        public List<SubStateData> CurPool { get; set; }

        public override void OnStart()
        {
            FristLoad();
            CurPool.Clear();

            foreach (var data in this.PoolInst)
            {
                data.Reset();
                CurPool.Add(data);
            }
            State = FSMState.Update;
        }

        private void FristLoad()
        {
            Debug.Log($"--- ActPoolFSM FristLoad {IsLoaded}");
            if (IsLoaded)
            {
                return;
            }
            IsLoaded = true;

            PoolInst = new List<SubStateData>();
            CurPool = new List<SubStateData>();

            foreach (var data in this.pool)
            {
                SubStateData subState = new SubStateData();
                subState.maxTime = data.maxTime;
                MainFSM so = ScriptableObject.Instantiate(data.state) as MainFSM; 
                so.InitReset(control);
                subState.state = so;
                PoolInst.Add(subState);
            }
        }

        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (CurState == null)
            {
                var getData = GetOne();
                if (getData == null)
                {
                    Debug.Log($"--- sub end");
                    OnExit();
                    return;
                }
                CurState = getData.state;

                Debug.Log($"--- CurState {CurState.GetType()}");
            }
            if (CurState.State != FSMState.Finish)
            {
                CurState.OnUpdate();
            }

            if (CurState.State == FSMState.Finish)
            {
                CurState = null;

            }
        }

        private SubStateData GetOne()
        {
            if (CurPool.Count == 0)
            {
                return null;
            }
            var actData = PoolInst.GetRandom(out int index);
            actData.HasUseTime++;
            if (actData.HasUseTime >= actData.maxTime)
            {
                CurPool.RemoveAt(index);
            }
            return actData;
        }

        public override void OnExit()
        {
            State = FSMState.Finish;
            Debug.Log($"--- All Finish");
        }
    }



    [Serializable]
    public class SubStateData : PowerModel
    {
        //配置最大次数
        public int maxTime = 1;
        public AIFSMBase state;

        //Runtime
        public int HasUseTime { get; set; }

        internal void Reset()
        {
            HasUseTime = 0;
            state.ResetFSM();
        }
    }

}
