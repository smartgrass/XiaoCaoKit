using NaughtyAttributes;
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


    ///技能ID规划
    ///string可读性,扩展性强, 但解析上需要字符串比较,如 skill_1, atk_1,roll,skill_1_1 等等
    ///int可读性差,但逻辑上可以大统一,  如 101,181,190
    ///加入raceId前缀,各种族间不相干


    [Serializable]
    public class RaceInfo : IIndex
    {
        public int raceId = 0;
        [Label("描述")]
        public string des;

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
    }

    public static class RaceIdSetting
    {
        public const string README = "平a 100,101,102\r\nRollId 0;\r\nJumpId 50\r\n普通技能 1,2,3,4\n" +
            "为防止重名,每个RaceId增加1000,如RaceId=2,技能则为2001,2002";

        public static int GetNorAckIdFull(int index)
        {
            return 100 + index;
        }

    }
}