﻿using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/AiInfoSo")]
    public class AiCmdSettingSo : SettingSo<AiSkillCmdSetting> { }


    [Serializable]
    public class AiSkillCmdSetting : IIndex
    {
        public int CmdSettingId = 0;
        [Label("描述")]
        public string des;

        public int Id => CmdSettingId;

        //出招表
        public List<string> cmdSkillList = new List<string>();


        public string GetCmdSkillByIndex(int index)
        {
            if (cmdSkillList.Count > index)
            {
                return cmdSkillList[index];
            }
            if (cmdSkillList.Count == 0)
            {
                throw new Exception(" cmdSkillList count 0");
            }
            return cmdSkillList[0];
        }
    }

    public static class RaceSetting
    {
        //RaceId 0,是通用的近战种族
        //RaceId 决定动画机,除非指定

        //AiCmdSetting 敌人专属设置√ & 出招表 
        //PlayerSetting 玩家专属的设置

        //MoveSetting  通用配置->直接拽得了


        public const string README = "技能最好使用数字 \r\n普通技能 1,2,3,4\n" +
            "为防止重名,每个RaceId增加100,如RaceId=2,技能则为201,202\n";



    }
}