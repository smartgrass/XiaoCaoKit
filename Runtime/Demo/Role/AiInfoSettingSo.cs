using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public string des;

        public int Id => id;

        public AIRoleConfig config;

        public List<AiAct> actPool = new List<AiAct>() { new AiAct() };

        public AiAct idleAct;
    }

    [Serializable]
    public class AIRoleConfig
    {
        public float moveSpeed = 3.5f;
        public float walkSR = 0.35f; //SR = SpeedRate
        public float walkAnimSR = 0.5f;
        [XCLabel("攻击欲望")]
        public float atkDesire = 0.5f;
    }
    

    [Serializable]
    public class AiAct : PowerModel
    {
        public ActMsgType actType;//事件      
        public string actMsg; //信息
        public float distance = 3; //执行距离

        public float moveTime = 1.5f; //追踪时间
        public float minMoveTime = 0.3f; //追踪时间
        [XCLabel("攻击前摇")]
        public float aimTime = 0f;
        [XCLabel("攻击结束的后摇")]
        public float endWaitTime = 0; //攻击结束的后摇
        [XCLabel("躲避时长")]
        public float hideTime = 0.5f; //结束后躲避时间,默认是后退

        [XCLabel("躲避时是否盯着目标")]
        public bool isLookAtTargetOnHide = false; // 躲避时是否盯着目标


        public int maxUseTime = 2; //一个组内最多使用次数
        [XCLabel("行为名")]
        public string actName = "ack1"; //id
        [XCLabel("下一个行为")]
        public string nextActName;

        public bool HasNextAct => !string.IsNullOrEmpty(nextActName);
    }


    public enum ActMsgType
    {
        Skill,
        Idle, //不使用技能
        OtherSkill,

    }

}
