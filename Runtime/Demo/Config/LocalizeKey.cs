using Newtonsoft.Json.Linq;
using XiaoCao.UI;

namespace XiaoCao
{
    public class LocalizeKey
    {
        //与枚举1,1对应
        public static string[] LanguageShowNames = { "中文", "English" };
        public static string Language = "Language";
        public static string Bgm = "Bgm";
        public static string RenderQuality = "RenderQuality";
        public static string FrameRate = "FrameRate";
        public static string NoEnough = "NoEnough";
        public static string AutoLockEnemy = "AutoLockEnemy";
        public static string LockCam = "LockCam"; //锁定视角
        public static string SkinList = "SkinList"; //皮肤
        public static string TestEnmeyList = "TestEnmeyList"; //测试敌人
        public static string MouseView = "MouseView";//视角跟随鼠标
        public static string MobileInput = "MobileInput";//移动端输入


        public static string Buff = "Buff";
        public static string BuffEffect = "BuffEffect";
        public static string EquippedBuffEffect = "EquippedBuffEffect";

        public static string BuildTime = "BuildTime";

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
        public static string GetSkillDesc(int skillId, int lv)
        {
            return GetSkillDescKey(skillId).ToLocalizeStr();
            // return "skillId";
        }


        #region Buff

        private static string GetGetBuffInfoKey(EBuff buff)
        {
            return $"Buff/{buff}";
        }

        public static string GetBuffInfoDesc(BuffInfo info)
        {
            try
            {
                int len = info.addInfo.Length;
                if (len == 0)
                {
                    return GetGetBuffInfoKey(info.eBuff).ToLocalizeStr();
                }

                if (SpecialBuffInfoDesc(info, out string ret))
                {
                    return ret;
                }

                string rawStr = GetGetBuffInfoKey(info.eBuff).ToLocalizeStr();
                if (len == 1)
                {
                    return string.Format(rawStr, AutoNumStr(info.addInfo[0]));
                }
                else if (len == 2)
                {
                    return string.Format(rawStr, AutoNumStr(info.addInfo[0]), AutoNumStr(info.addInfo[1]));
                }
                else if (len == 3)
                {
                    return string.Format(rawStr, AutoNumStr(info.addInfo[0]), AutoNumStr(info.addInfo[1]), AutoNumStr(info.addInfo[2]));
                }
                else
                {
                    Debuger.LogError($"--- buff desc error {info.eBuff}");
                }

                return rawStr;
            }
            catch (System.Exception e)
            {
                Debuger.LogError($"--- buff desc error {e}");
                return GetGetBuffInfoKey(info.eBuff).ToLocalizeStr();
            }
        }
        //小于1,默认显示百分比
        private static string AutoNumStr(float num)
        {
            if (num < 1)
            {
                return ((num * 100)).ToString("#.##");
            }
            else
            {
                return num.ToString("#.##");
            }
        }

        //需要手动处理的描述
        private static bool SpecialBuffInfoDesc(BuffInfo info, out string rawStr)
        {
            //if (info.eBuff == EBuff.AtkAddIfBelowHalfHp)
            //{
            //    rawStr = GetGetBuffInfoKey(info.eBuff).ToLocalizeStr();
            //    return true;
            //}

            rawStr = "";
            return false;
        }

        #endregion

    }
}