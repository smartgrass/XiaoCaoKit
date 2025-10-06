using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/LevelUISettingSo")]
    public class LevelUISettingSo : NumMapSo<LevelUIInfo>
    {
    }


    [Serializable]
    public class LevelUIInfo : IIndex
    {
        public int id;
        public int Id => id;

        public List<Vector2> posList = new List<Vector2>();

        public List<Vector2Int> lines = new List<Vector2Int>();
        
        //关卡信息 默认按顺序
    }
}