using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 行为池
    /// </summary>
    ///<see cref="SequenceFSM"/>
    [CreateAssetMenu(fileName = "ActPoolFSM", menuName = "SO/AI/ActPoolFSM", order = 1)]
    public class ActPoolFSM : MainDataFSM
    {
        public IdleState idleState;

        public FSMPoolType poolType = FSMPoolType.Random;

        [Tooltip("最大使用次数,0默认抽完")]
        public int MaxUseTime;

        //行为池有 概率,次数
        public List<SubStateData> pool = new List<SubStateData>() { new SubStateData() };


        public AIFSMBase CurState { get; set; }
        public AIFSMBase IdleStateInst { get; set; }
        public List<SubStateData> PrefabPool { get; set; }
        public List<SubStateData> InstancePool { get; set; }

        public int UseTime { get; set; }

        public bool IsCheckUseTime => MaxUseTime > 0;

        public override void OnStart()
        {
            bool isFrist = FristLoad();
            InstancePool.Clear();
            UseTime = 0;

            State = FSMState.Update;

            if (IdleStateInst != null)
            {
                IdleStateInst.ResetFSM();
            }



            if (!HasTarget || (isFrist && RandomHelper.GetRandom(setting.randomIdleStart)))
            {
                //OnExit();
                CurState = IdleStateInst;
                return;
            }
            Debug.Log($"--- ActPoolFSM OnStart {name}");
            foreach (var data in this.PrefabPool)
            {
                data.Reset();
                InstancePool.Add(data);
            }
        }

        private bool FristLoad()
        {
            Debuger.Log($"--- ActPoolFSM FristLoad {IsLoaded}");
            if (IsLoaded)
            {
                return false;
            }
            IsLoaded = true;

            PrefabPool = new List<SubStateData>();
            InstancePool = new List<SubStateData>();

            foreach (var data in this.pool)
            {
                SubStateData subState = new SubStateData();
                subState.maxTime = data.maxTime;
                Debug.Log($"--- SubStateData {data.maxTime} ");
                var so = ScriptableObject.Instantiate(data.state); //as MainFSM;
                so.name = data.state.name;
                so.InitReset(control);
                subState.state = so;
                PrefabPool.Add(subState);
            }
            if (idleState != null)
            {
                IdleStateInst = ScriptableObject.Instantiate(idleState);
                IdleStateInst.name = idleState.name;
                IdleStateInst.InitReset(control);
            }
            return true;
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
                    OnExit();
                    return;
                }
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
            if (InstancePool.Count == 0 || IsUseTimeLess)
            {
                return null;
            }

            SubStateData data;
            int index;
            if (poolType == FSMPoolType.Random)
            {
                data = InstancePool.GetPowerRandom(out index);
            }
            else
            {
                index = 0;
                data = InstancePool[0];
            }

            data.HasUseTime++;
            if (data.HasUseTime >= data.maxTime)
            {
                InstancePool.RemoveAt(index);
            }
            //Debug.Log($"--- GetOne {data.state.name} {data.HasUseTime}/{data.maxTime} InstancePoolCount {InstancePool.Count}");
            UseTime++;
            data.state.ResetFSM();
            return data.state;
        }

        public override void OnExit()
        {
            State = FSMState.Finish;
            Debuger.Log($"--- AIFSM All Finish");
        }

        public override string GetStatePath()
        {
            if (CurState == null)
            {
                return name;
            }
            string subPath = CurState.GetStatePath();
            string path = $"{name}/{subPath}";
            return path;
        }

    }



    [Serializable]
    public class SubStateData : PowerModel
    {
        public AIFSMBase state;
        //配置最大次数
        public int maxTime = 1;

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
