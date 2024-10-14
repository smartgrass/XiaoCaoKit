using XiaoCao.UI;

namespace XiaoCao{
	public class LocalizeKey{
        //与枚举1,1对应
        public static string[] LanguageShowNames = { "English", "中文" };
        public static string Language = "Language";
		public static string Bgm = "Bgm";




        public static string GetSkillNameKey(int skillId)
        {
            return $"skill_{skillId}";
        }       
        public static string GetSkillDescKey(int skillId)
        {
            return $"skill_{skillId}_desc";
        }
        


        //技能命名 skill_[id]
        //技能命描述 skill_[id]_des
        public static string GetSkillDesc(int skillId, int lv){
            return GetSkillDescKey(skillId).ToLocalizeStr();
            // return "skillId";
        }
    }
}