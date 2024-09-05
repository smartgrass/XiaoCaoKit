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
        ///建议加入raceId前缀
        ///int 可读性差, 如 101,181,190
        ///
        ///raceId * 100 + 技能id
        ///玩家raceId = 1 => 100~ 199
        ///敌人raceId = 10 =>  1000~1099
        ///

        ///平a 100~109
        ///特殊技能 110~119 (Roll = 110)
        ///普通技能 131~199

        public static int GetSkillIdFull(int raceId, int index)
        {
            return raceId * 100 + index + 30;
        }
        public static int GetNorAckIdFull(int raceId, int index)
        {
            return raceId * 100 + index;
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


