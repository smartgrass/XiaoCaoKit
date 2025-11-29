using Newtonsoft.Json.Linq;
using System;
using XiaoCao.UI;

namespace XiaoCao
{
    public static class LocalizeKey
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
        public static string MouseView = "MouseView"; //视角跟随鼠标
        public static string MobileInput = "MobileInput"; //移动端输入


        public static string Buff = "Buff";
        public static string BuffEffect = "BuffEffect";
        public static string EquippedBuffEffect = "EquippedBuffEffect";

        public static string BuildTime = "BuildTime";
        public static string SwapCameraSpeed = "SwapCameraSpeed";
        public static string AnglePower = "AnglePower";
        public static string IsExitLevel = "IsExitLevel";
        public static string LevelFinish = "LevelFinish";
        public static string LevelSuccess = "LevelSuccess";
        public static string LevelFail = "LevelFail";
        public static string LevelTimeCount = "LevelTimeCount";
        public static string KillCount = "KillCount";
        
        public static string UnlockAllLevel = "UnlockAllLevel";

        public static string GetLevelName(int chapter,int level)
        {
            string key = $"level_{chapter}_{level}";
            if (LocalizeMgr.Inst.HasKey(key))
            {
                key.ToLocalizeStr();
            }
            string name = $"level_{chapter}".ToLocalizeStr();
            return $"{name}-{level}";
        }
        
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

        #region Item

        public static string GetItemName(this Item item)
        {
            if (item.type == ItemType.Buff)
            {
                string key = GetBuffNameKey(item.ToBuffItem().GetFirstEBuff);
                return key.ToLocalizeStr();
            }

            return item.id.ToLocalizeStr();
        }

        #endregion

        #region Buff

        public static string GetBuffNameKey(EBuff buff)
        {
            if (buff.GetBuffType() == EBuffType.Ex)
            {
                return $"BuffTitle/{buff}";
            }

            return "Buff";
        }

        private static string GetGetBuffInfoKey(EBuff buff)
        {
            return $"Buff/{buff}";
        }

        public static string GetBuffInfoDesc(BuffInfo info)
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
            return FormatWithArray(rawStr, info.addInfo);
        }


        public static string FormatWithArray(string format, float[] values)
        {
            // 将float数组转换为object数组
            object[] objValues = new object[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                objValues[i] = values[i];
            }

            // 使用string.Format处理
            try
            {
                return string.Format(format, objValues);
            }
            catch (FormatException e)
            {
                Debuger.LogError($"格式化失败: {e.Message}");
                return format; // 返回原始格式字符串或自定义错误信息
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