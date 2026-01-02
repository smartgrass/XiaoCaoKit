using System.Collections.Generic;
using cfg;
using UnityEngine;

namespace XiaoCao
{
    public class PlayerAtkTimer : PlayerComponent
    {
        public PlayerAtkTimer(Player0 owner) : base(owner)
        {
        }

        public PlayerSetting playerSetting => Data_P.playerSetting;

        public Dictionary<string, SkillCdData> dic = new Dictionary<string, SkillCdData>();

        public float GetSkillCDOff => owner.data_R.playerAttr.GetValue(EAttr.SkillCDOff);

        public float norAckTimer;

        public int GetNextNorAckIndex()
        {
            int ret;
            if (Time.time < norAckTimer)
            {
                int len = playerSetting.norAtkCount; // norAtkIds.Count;
                ret = (Data_P.curNorAckIndex + 1) % len;
            }
            else
            {
                //若时间超过，则重置为0
                ret = 0;
            }

            SetNorAckTimer();
            return ret;
        }

        public void UpdataCdOff()
        {
            float cdOff = GetSkillCDOff;
            foreach (var item in dic.Values)
            {
                item.CdOff = cdOff;
            }
        }

        public void SetNorAckTimer()
        {
            norAckTimer = Time.time + playerSetting.resetNorAckTime;
        }


        private void CheckDic(string skillIndex)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.baseCd = LubanTables.GetSkillUpgradeSetting(skillIndex).Cd;
                skillCdData.CdOff = GetSkillCDOff;
                dic[skillIndex] = skillCdData;
            }
        }

        public void AddKey(string skillIndex, float cd)
        {
            if (!dic.ContainsKey(skillIndex))
            {
                SkillCdData skillCdData = new SkillCdData();
                skillCdData.baseCd = cd;
                dic[skillIndex] = skillCdData;
            }
            else
            {
                dic[skillIndex].baseCd = cd;
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

        public void ClearAllCd()
        {
            foreach (var v in dic.Values)
            {
                v.cdFinishTime = 0;
            }
        }

        public class SkillCdData
        {
            public float baseCd = 1;

            public float CD
            {
                get
                {
                    float mult = (1 - CdOff);
                    if (mult < 0.5f)
                    {
                        //冷缩上限
                        mult = 0.5f;
                    }

                    return baseCd * mult;
                }
            }

            public float cdFinishTime { get; set; }

            public float CdOff { get; set; }

            public bool IsCd => Time.time < cdFinishTime;

            public void EnterCD()
            {
                cdFinishTime = Time.time + baseCd;
            }

            //剩余时间 / 总cd -> 百分比
            public float GetWaitTimeProccess()
            {
                if (IsCd)
                {
                    return (cdFinishTime - Time.time) / baseCd;
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