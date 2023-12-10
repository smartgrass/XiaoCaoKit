using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class SkillEventData : MonoBehaviour
    {
        //BaseSkillID
        public string skillId = "";
        //SubSkillID
        public string skillName = "";

        public float speed = 1f;

        public List<XCEvent> data;

    }

}