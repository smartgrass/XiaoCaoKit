using System.Data;

namespace XiaoCao
{
    public static class RaceIdSetting
    {

        // 获取基本配置
        public static int GetConfigId(int raceId)
        {
            //raceId在 0~9 玩家
            if (IsPlayerRace(raceId)) 
            {  
                return 0;
            }
            else
            {

                //敌人默认配置序号1, 其他情况另作处理
                return 1;
            }
        }

        public static bool IsPlayerRace(int raceId)
        {
            return raceId < 10;
        }


        ///技能ID规划
        ///与使用string相比
        ///string可读性,扩展性强, 但解析上需要字符串比较,如 skill_1, atk_1,roll,skill_1_1 等等
        ///int 可读性差,但逻辑上可以大统一,  如 101,181,190
        ///加入raceId前缀,各种族间不相干 roll -> 1/0, skil_1->1/1  ,atk1 =>1/101, 

        ///平a 101,102,103
        ///特殊技能 50 >
        ///普通技能 1,2,3,4

        public static int GetSkillIdFull(int raceId, int index)
        {
            return raceId * 100 + index + 30;
        }

        public static int GetRollSkillId(int raceId)
        {
            return raceId * 100 + 10;
        }


        public static string GetSkillAckKey(int ackId,int subAckId)
        {
            if(subAckId == 0)
            {
                return ackId.ToString();
            }
            return $"{ackId}_{subAckId}";
        }

        public static EActType GetSkillType(int skillId)
        {
            int sub = skillId % 100;
            if (sub < 10)
            {
                return EActType.None;
            }
            else if (sub < 30)
            {
                if (sub == 10)
                {
                    return EActType.Roll;
                }
                else if (sub == 11)
                {
                    return EActType.Jump;
                }
                else
                {
                    return EActType.Other;
                }
            }
            else
            {
                return EActType.Skill;
            }
        }

    }



}


