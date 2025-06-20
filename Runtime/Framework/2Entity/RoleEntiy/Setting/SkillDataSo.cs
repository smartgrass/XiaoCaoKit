﻿using NaughtyAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/SkillDataSo",fileName = "SkillDataSo")]
    public class SkillDataSo : KeyMapSo<SkillData>
    {
        [Label("角色技能栏默认技能")]
        public List<string> playerDefaultSkills;

        public bool UseTestSkill = false;

        public List<string> testSkills;
    }


    [Serializable]
    public class SkillData : IKey
    {
        public string key;

        public float cd;
        public string Key => key;
    }

}
