using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    //行为池具体放So
    //运行时作为info读入
    public class AIInfo : ScriptableObject
    {
        public List<AIAct> actPool;

        private void Example()
        {
            ActGroup actGroup = new ActGroup(actPool);
            AIAct getAct = actGroup.GetOne();
        }
    }

    [System.Serializable]
    public class ActGroup
    {
        public List<AIAct> actPool { get; set; }

        public int Count { get; set; }

        public bool IsAllFinish { get; set; }
        public List<AIRuntimeData> RuntimeDatas { get; set; }

        public AIRuntimeData curRuntimeData { get; set; }


        public bool IsEmpty => actPool.Count == 0;

        public ActGroup(List<AIAct> actPool)
        {
            this.actPool = actPool;
            Count = this.actPool.Count;
            RuntimeDatas =new List<AIRuntimeData>();
            CreatRuntimeDatas();
        }

        public void ReInitAll()
        {
            IsAllFinish = false;
            RuntimeDatas.Clear();
            CreatRuntimeDatas();
        }

        public AIAct GetOne()
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
            foreach (AIAct act in this.actPool)
            {
                AIRuntimeData data = new AIRuntimeData()
                {
                    power = act.power,
                    index = i++,
                };
                RuntimeDatas.Add(data);
            }
        }
    }

    [System.Serializable]
    public class AIAct : PowerModel
    {

        public ActMsgType actType;//事件      
        public string actMsg = "NorAck"; //信息  当
        public float targetDis = 3; //执行距离

        public float moveTime = 1.5f; //追踪时间
        public float endWaitTime = 0; //攻击结束的后摇
        public float hideTime = 0.5f; //结束后躲避时间,默认是后退

        public bool isHide_LookAt = false; // 躲避时是否盯着目标


        public int maxUseTime = 2; //一个组内最多使用次数
        public string actName; //id
        public string nextActName;

        public bool HasNextAct
        {
            get => !string.IsNullOrEmpty(nextActName);
        }

    }

    public class AIRuntimeData: PowerModel
    {
        public int index;
        public int useTimer;
        public AIActState state;
        public void Reset()
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

    [System.Serializable]
    public class PowerModel
    {
        public int power = 1; //权重
    }

    public static class RandomHelper
    {
        public static T GetRandom<T>(this List<T> powerModel, out int index) where T : PowerModel
        {
            index = 0;
            int total = 0;
            foreach (var item in powerModel)
            {
                total += item.power;
            }
            if (powerModel.Count == 0)
            {
                index = -1;
                return null;
            }
            if (powerModel.Count == 1 || total == 0)
            {
                return powerModel[0];
            }

            int random = UnityEngine.Random.Range(0, total);
            int rangeMax = 0;

            int length = powerModel.Count;
            for (int i = 0; i < length; i++)
            {
                rangeMax += powerModel[i].power;
                //当随机数小于 rangeMax 说明在范围内
                if (rangeMax > random && powerModel[i].power > 0)
                {
                    index = i;
                    return powerModel[i];
                }
            }
            Debug.LogError("??? power = 0");
            return null;
        }
    }
}
