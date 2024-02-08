namespace XiaoCao
{
    public static class RaceIdSetting
    {
        public static int GetConfigId(int raceId)
        {
            //0~9 玩家
            if (raceId < 10) 
            {  
                return 0;
            }
            else
            {

                //敌人默认为1, 其他情况另作处理
                return 1;
            }
        }


        ///技能ID规划
        ///raceId * 100 + 技能id
        ///玩家raceId = 1 => 100~ 199
        ///敌人raceId = 10 =>  1000~1099
        ///

        ///玩家普通技能 id  101~149
        ///特殊技能 id > 151~199
        public static int GetFullSkillId(int raceId, int index)
        {
            return raceId * 100 + index;
        }
        public static int GetFullNorAckId(int raceId, int index)
        {
            return raceId * 100 + 50 + index;
        }
        public static int GetRollId(int raceId)
        {
            return raceId * 100 + 60;
        }


        public static string GetSkillAckKey(int ackId,int subAckId)
        {
            if(subAckId == 0)
            {
                return ackId.ToString();
            }
            return $"{ackId}_{subAckId}";
        }
    }

}


