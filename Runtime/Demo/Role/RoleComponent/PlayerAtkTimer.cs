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
                skillCdData.cd = ConfigMgr.skillDataSo.GetOrDefault(skillIndex).cd;
                dic[skillIndex] = skillCdData;
            }
        }

        public void AddKey(string skillIndex,float cd)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.cd = cd;
                dic[skillIndex] = skillCdData;
            }
            else
            {
                dic[skillIndex].cd = cd;
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

        public float GetWaitTimeProccess(string skillIndex)
        {
            if (string.IsNullOrEmpty(skillIndex))
            {
                return 1;
            }
            CheckDic(skillIndex);
            return dic[skillIndex].GetWaitTimeProccess();
        }

        public float GetWaitTime(string skillIndex)
        {
            CheckDic(skillIndex);
            return dic[skillIndex].GetWaitTime();
        }


        public class SkillCdData
        {
            public float cd = 1;

            public float cdFinishTime { get; set; }

            public bool IsCd => Time.time < cdFinishTime;
            public void EnterCD()
            {
                cdFinishTime = Time.time + cd;
            }

            //剩余时间 / 总cd -> 百分比
            public float GetWaitTimeProccess()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time) / cd;
                }
                return 0;
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
