using System;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/MoveSettingSo")]
    public class MoveSettingSo : ScriptableObject
    {
        public int id = 0;

        public string des;
        public int Id => id;

        public float baseMoveSpeed = 3.8f;

        public float moveSmooth = 0.05f;

        public float rotationLerp = 0.2f;

        public float angleSpeed = 10;

        public float g = -73.04f;

        public float GOnGroundMult = 0.8f;

        public float MaxGOnGroundMult = 4;

        public float idleValue = 0.3f;

        public float fightIdleValue = 0;//TODO

        public float weight = 1; //角色质量, 默认1,减少击退距离

        public Vector3 CamFollewOffset = new Vector3(0,0.83f,0);
        
        public float deadTime = 3;//死亡后回收时间
    }
}
