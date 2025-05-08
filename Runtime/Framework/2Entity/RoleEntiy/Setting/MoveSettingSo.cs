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

        public Vector3 CamFollewOffset = new Vector3(0,0.83f,0);
    }
}
