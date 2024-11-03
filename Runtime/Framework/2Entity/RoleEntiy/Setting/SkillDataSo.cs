using System;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/SkillDataSo",fileName = "SkillDataSo")]
    public class SkillDataSo : KeyMapSo<SkillData>
    {
    }


    [Serializable]
    public class SkillData : IKey
    {
        public string id;

        public float cd;

        public string Id => id;
    }

}
