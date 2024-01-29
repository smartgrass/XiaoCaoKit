using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class AI
    {

    }


    [System.Serializable]
    public class ActGroup
    {
        public string des = "";
        //public int maxTime = 20;
        //public float tardis; //目前AI没有根据tardis选择技能组的能力
        [SerializeField]
        public List<AIAct> aIActions;
        [NonSerialized]
        private List<AIAct> enableActs = new List<AIAct>(); //可以选的Act,使用一个都会移除一个,并且计数
        [HideInInspector]
        public bool IsDisable = false;

        public AIAct GetOneAct()
        {
            if (enableActs.Count == 0)
            {
                ReStart();
            }
            int index = 0;
            var res = enableActs.GetRandom(out index);
            res.useTimer++;
            if (res.useTimer >= res.maxUseTime)
            {
                enableActs.RemoveAt(index);
            }
            if (enableActs.Count == 0)
            {
                //切换下一组
                IsDisable = true;
            }
            return res;
        }

        public void ReStart()
        {
            IsDisable = false;
            enableActs = new List<AIAct>(aIActions);
            foreach (var item in enableActs)
            {
                item.Reset();
            }
        }

        public bool IsEmpty => aIActions.Count == 0;
    }

    [System.Serializable]
    public class AIAct : PowerModel
    {
        public string actName; //id
        public ActMsgType actType;//事件      
        public string actMsg = "NorAck"; //信息  当
        public float targetDis = 3; //执行距离


        public float moveTime = 1.5f; //追踪时间
        public float endWaitTime = 0; //攻击结束的后摇
        public float hideTime = 0.5f; //结束后躲避时间,默认是后退
        public bool isHide_LookAt = false; // 躲避时是否盯着目标

        public int maxUseTime = 2; //一个组内最多使用次数
        public string nextActName;
        public int useTimer = 0;
        [ReadOnly]
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
