using UnityEngine;
using System.Collections;
using XiaoCao;
using System.Collections.Generic;
using System;
using NaughtyAttributes;

namespace Flux
{
    [FEvent("Game Object/TriggerRangeEvent", typeof(FTriggerRangeTrack))]
    public class FTriggerRangeEvent : FEvent
    {
        [XCLabel("最大命中次数(0=不限)")]
        [Tooltip("命中累计到该次数后结束本触发事件, 并回调 ITriggerEnd(爆炸等); 0 = 不限次数, 命中后不会回调")]
        public int maxTriggetTime = 0; 

        public MeshInfo meshInfo;
        
        public float atkSpan;
        
        [XCLabel("触发对象")]
        [EnumFlags]
        public TriggerTargetType triggerTargetType = TriggerTargetType.Enemy;

        public override XCEvent ToXCEvent()
        {
            var xce = new XCTriggerEvent();
            var fe = this;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.meshInfo = fe.meshInfo;
            xce.atkSpan = fe.atkSpan;
            xce.maxTriggerTime = fe.maxTriggetTime;
            xce.triggerTargetType = fe.triggerTargetType;
            return xce;
        }



    }
    [Serializable]
    public class MeshInfo
    {
        [EnumLabel("类型")]
        public MeshType meshType;

        [Header("[Size],[eulerAngles],[center]")]
        public Vector3[] values = new Vector3[3] { Vector3.one ,Vector3.zero, Vector3.zero};

        public Vector3 GetSize => values[0];
        public Vector3 GetEulerAngles => values[1];
        public Vector3 GetCenter => values[2];

        //扇形
        public float GetRadius => GetSize.x; //半径
        public float GetHight => GetSize.y; //高
        public float GetRadian => GetSize.z;  //弧度
    }

}
