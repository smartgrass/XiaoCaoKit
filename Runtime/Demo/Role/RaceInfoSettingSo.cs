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
    public class RaceInfoSettingSo : SettingSo<RaceInfo> { }




    [Serializable]
    public class RaceInfo : IIndex
    {
        public int raceId = 0;
        public int Id => raceId;
        public int RollId => 0;
        public int JumpId => 50;

        public List<int> cmdSkillList = new List<int>();

        public List<int> otherSkillList = new List<int>();


        public int GetCmdSkillIndex(int index)
        {
            if (cmdSkillList.Count > index)
            {
                return cmdSkillList[index];
            }
            if (cmdSkillList.Count == 0)
            {
                return -1;
            }
            return cmdSkillList[0];
        }

        public static int GetNorAckIdFull(int index)
        {
            return 100 + index;
        }

    }
}