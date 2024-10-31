using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class PlayerAtkTimer : PlayerComponent
    {
        public PlayerAtkTimer(Player0 owner) : base(owner) { }
        public PlayerSetting playerSetting => Data_P.playerSetting;

        public Dictionary<string, SkillCdData> dic = new Dictionary<string, SkillCdData>();

        public float norAckTimer;

        public int GetNextNorAckIndex(bool setTime = true)
        {
            int ret = 0;
            if (Time.time < norAckTimer)
            {
                int len = playerSetting.norAtkCount;// norAtkIds.Count;
                ret = (Data_P.curNorAckIndex + 1) % len;
            }
            if (setTime)
            {
                SetNorAckTime();
            }
            return ret;
        }

        public void SetNorAckTime()
        {
            norAckTimer = Time.time + playerSetting.resetNorAckTime;
        }


        private void CheckDic(string skillIndex)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.cd = playerSetting.GetSkillSetting(skillIndex).cd;
                dic[skillIndex] = skillCdData;
            }
        }

        //public bool IsSkillReady(string skillId)
        //{
        //    int hash = skillId.GetHashCode();
        //}

        public bool IsSkillReady(string skillIndex)
        {
            CheckDic(skillIndex);
            return !dic[skillIndex].IsCd;
        }

        public void SetSkillEnterCD(string skillIndex)
        {
            CheckDic(skillIndex);
            dic[skillIndex].EnterCD();
        }

        public float GetProcess(string skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetCurProccess();
        }

        public float GetWaitTime(string skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetWaitTime();
        }


        public class SkillCdData
        {
            public float cd;

            public float cdFinishTime { get; set; }

            public bool IsCd => Time.time < cdFinishTime;
            public void EnterCD()
            {
                cdFinishTime = Time.time + cd;
            }

            public float GetCurProccess()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time) / cd;
                }
                return 1;
            }

            public float GetWaitTime()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time);
                }
                return 0;
            }
        }
    }

}
