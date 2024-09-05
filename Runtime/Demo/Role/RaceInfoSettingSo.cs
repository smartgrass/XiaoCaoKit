using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{

    ///<see cref="RaceIdSetting"/>
    [CreateAssetMenu(menuName = "SO/AiInfoSo")]
    public class RaceInfoSettingSo : SettingSo<AiInfo>{}




    [Serializable]
    public class RaceInfo : IIndex
    {
        public int raceId = 0;
        public int Id => raceId;

        public List<int> cmdSkillList = new List<int>();

        public List<int> otherSkillList = new List<int>();

    }
}