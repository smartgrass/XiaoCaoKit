using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 行为池
    /// </summary>
    [CreateAssetMenu(fileName = "ActPoolFSM", menuName = "SO/AI/ActPoolFSM", order = 1)]
    public class ActPoolFSM : MainDataFSM
    {
        [Tooltip("pool结束时调用")]
        public AIFSMBase EndState;

        public FSMPoolType poolType = FSMPoolType.Random;

        [Tooltip("最大使用次数,0默认抽完")]
        public int MaxUseTime;

        //行为池有 概率,次数
        public List<SubStateData> pool = new List<SubStateData>() { new SubStateData() };


        public AIFSMBase CurState { get; set; }
        public AIFSMBase CurEndState { get; set; }
        public List<SubStateData> PoolInst { get; set; }
        public List<SubStateData> CurPool { get; set; }

        public int UseTime { get; set; }

        public bool IsCheckUseTime => UseTime > 0;

        public override void OnStart()
        {
            FristLoad();
            CurPool.Clear();
            UseTime = 0;
            foreach (var data in this.PoolInst)
            {
                data.Reset();
                CurPool.Add(data);
            }
            State = FSMState.Update;

            if (CurEndState != null )
            {
                CurEndState.ResetFSM();
            }
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
            if (EndState != null)
            {
                CurEndState = ScriptableObject.Instantiate(EndState) ;
                CurEndState.InitReset(control);
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
                CurState = GetOne();
                if (CurState == null)
                {
                    Debug.Log($"--- sub end");
                    OnExit();
                    return;
                }
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

        private AIFSMBase GetOne()
        {
            bool IsUseTimeLess = IsCheckUseTime && UseTime >= MaxUseTime;

            if (CurPool.Count == 0 || IsUseTimeLess)
            {
                if (CurEndState!=null && CurEndState.State == FSMState.None)
                {
                    return CurEndState;
                }
                return null;
            }

            SubStateData data;
            int index;
            if (poolType == FSMPoolType.Random) {
                data = PoolInst.GetRandom(out index);
            }
            else
            {
                index = 0;
                data = PoolInst[0];
            }

            data.HasUseTime++;
            if (data.HasUseTime >= data.maxTime)
            {
                CurPool.RemoveAt(index);
            }
            UseTime++;
            return data.state;
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


    public enum FSMPoolType
    {
        [InspectorName("随机")]
        Random,
        [InspectorName("顺序")]
        Sequence
    }

}
