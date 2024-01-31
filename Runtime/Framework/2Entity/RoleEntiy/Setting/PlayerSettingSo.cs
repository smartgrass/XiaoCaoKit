using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    public class PlayerSettingSo : SettingSo<PlayerSetting>
    {

    }


    [Serializable]
    public class PlayerSetting
    {
        public int norAtkCount = 3;
        //平a重置时间
        public float resetNorAckTime = 1.5f;
    }

}
