using UnityEngine;
using System.Collections;
using XiaoCao;
using System.Collections.Generic;
using System;

namespace Flux
{
    [FEvent("Game Object/TriggerRangeEvent", typeof(FTriggerRangeTrack))]
    public class FTriggerRangeEvent : FEvent
    {
        public MeshInfo meshInfo;

        public override XCEvent ToXCEvent()
        {
            var xce = new XCTriggerEvent();
            var fe = this;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.meshInfo = fe.meshInfo;
            return xce;
        }



    }
    [Serializable]
    public class MeshInfo
    {
        [EnumLabel("����")]
        public MeshType meshType;

        [Header("[Size],[eulerAngles],[center]")]
        public Vector3[] values = new Vector3[3] { Vector3.one ,Vector3.zero, Vector3.zero};

        public Vector3 GetSize => values[0];
        public Vector3 GetEulerAngles => values[1];
        public Vector3 GetCenter => values[2];

        //����
        public float GetRadius => GetSize.x; //�뾶
        public float GetHight => GetSize.y; //��
        public float GetRadian => GetSize.z;  //����
    }

}
