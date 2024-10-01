using System;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/MoveSettingSo")]
    public class MoveSettingSo : SettingSo<MoveSetting>
    {

    }


    //基础数值
    [Serializable]
    public class MoveSetting : IIndex
    {
        public int id = 0;

        public string des;
        public int Id => id;

        public float baseMoveSpeed = 4;

        public float moveSmooth = 0.05f;

        public float rotationLerp = 0.2f;

        public float angleSpeed = 10;

        public float g = -40;

        public float GOnGroundMult = 0.8f;

        public float MaxGOnGroundMult = 4;
    }
}
