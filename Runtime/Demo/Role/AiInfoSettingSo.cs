using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/AiInfoSo")]
    public class AiInfoSettingSo : SettingSo<AiInfo>
    {

    }

    //行为池具体放So
    //运行时作为info读入
    [Serializable]
    public class AiInfo : IIndex
    {
        public int id = 0;
        public int Id => id;

        public List<AiAct> actPool;

        private void Example()
        {
            AiActPool poolData = new AiActPool(actPool);
            AiAct getAct = poolData.GetOne();
        }
    }

    [System.Serializable]
    public class AiActPool
    {
        public List<AiAct> actPool { get; set; }

        public int Count { get; set; }
        public bool IsAllFinish { get; set; }
        public List<AiPoolData> RuntimeDatas { get; set; }

        public AiPoolData curRuntimeData { get; set; }


        public bool IsEmpty => actPool.Count == 0;

        public AiActPool(List<AiAct> actPool)
        {
            this.actPool = actPool;
            Count = this.actPool.Count;
            RuntimeDatas = new List<AiPoolData>();
            CreatRuntimeDatas();
        }

        public void ReInitAll()
        {
            IsAllFinish = false;
            RuntimeDatas.Clear();
            CreatRuntimeDatas();
        }

        public AiAct GetOne()
        {
            int index = 0;
            if (RuntimeDatas.Count == 0)
            {
                ReInitAll();
            }

            //随机取出一个
            curRuntimeData = RuntimeDatas.GetRandom(out index);
            var info = actPool[curRuntimeData.index];
            curRuntimeData.useTimer++;

            if (curRuntimeData.useTimer >= info.maxUseTime)
            {
                RuntimeDatas.RemoveAt(index);
            }
            //全用完,切换下一组
            if (RuntimeDatas.Count == 0)
            {
                IsAllFinish = true;
            }
            return info;
        }


        private void CreatRuntimeDatas()
        {
            int i = 0;
            foreach (AiAct act in this.actPool)
            {
                AiPoolData data = new AiPoolData()
                {
                    power = act.power,
                    index = i++,
                };
                RuntimeDatas.Add(data);
            }
        }
    }

    [System.Serializable]
    public class AiAct : PowerModel
    {
        public ActMsgType actType;//事件      
        public string actMsg = "NorAck"; //信息  当
        public float distance = 3; //执行距离

        public float moveTime = 1.5f; //追踪时间
        public float endWaitTime = 0; //攻击结束的后摇
        public float hideTime = 0.5f; //结束后躲避时间,默认是后退

        public bool isLookAtTargetOnHide = false; // 躲避时是否盯着目标


        public int maxUseTime = 2; //一个组内最多使用次数
        public string actName; //id
        public string nextActName;

        public bool HasNextAct
        {
            get => !string.IsNullOrEmpty(nextActName);
        }
    }

    public class AiPoolData : PowerModel, IUsed
    {
        public int index;
        public int useTimer;
        public AIActState state;
        public void Used()
        {
            useTimer = 0;
            state = AIActState.Start;
        }
    }

    public enum ActMsgType
    {
        Skill,
        OtherSkill,
        Move, //不使用技能
    }

    public enum AIActState
    {
        Start,
        MoveTo, //靠近或者瞄准时间
        Acking, //等待技能结束时间
        Hide, //躲避时间
        End, //切换下一技能
    }

}
