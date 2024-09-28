using Luban;
using System.IO;
using UnityEngine;
using XiaoCao;

namespace cfg
{
    public class LubanTables : Singleton<LubanTables>
    {
        #region Get
        private static SkillSettingReader SkillSettingReader => LubanTables.Inst.Tables.SkillSettingReader;
        private static LevelSettingReader LevelSettingReader => LubanTables.Inst.Tables.LevelSettingReader;
        private static ChapterSettingReader ChapterSettingReader => LubanTables.Inst.Tables.ChapterSettingReader;

        public static ChapterSetting GetChapterSetting(int key)
        {
            var ret = ChapterSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                //默认值
                ret = ChapterSettingReader.DataList[0];
            }
            return ret;
        }

        public static LevelSetting GetLevelSetting(int key)
        {
            var ret = LevelSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                ret = LevelSettingReader.DataList[0];
            }
            return ret;
        }


        public static SkillSetting GetSkillSetting(int skillId,int subSkillId)
        {
            string infoKey = GetSkillAckKey(skillId,subSkillId);
            
            return GetSkillSetting(infoKey);
        }
        public static string GetSkillAckKey(int ackId, int subAckId)
        {
            if (subAckId == 0)
            {
                return ackId.ToString();
            }
            return $"{ackId}_{subAckId}";
        }

        public static SkillSetting GetSkillSetting(string key)
        {
            var ret = SkillSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                //默认值
                ret = SkillSettingReader.DataList[0];
            }
            return ret;
        }

        #endregion


        private bool _init = false;

        private Tables _tables;

        public Tables Tables
        {
            get
            {
                if (!_init)
                {
                    Load();
                }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    _init = false;
#endif
                return _tables;
            }
        }

        public void Reset()
        {
            _init = false;
        }

        /// <summary>
        /// 加载配置。
        /// </summary>
        public void Load()
        {
            _tables = new Tables(LoadByteBuf);
            _init = true;
        }

        /// <summary>
        /// 加载二进制配置。
        /// </summary>
        /// <param name="file">FileName</param>
        /// <returns>ByteBuf</returns>

        private ByteBuf LoadByteBuf(string file)
        {
            string path = GetLubanPath(file);

            Debug.Log($"--- {path}");
            return new ByteBuf(File.ReadAllBytes(path));
        }

        private static string GetLubanPath(string file)
        {
            string path;
            if (Application.isEditor)
            {
                path = $"{XCPathConfig.GetGameConfigDir()}/Luban/{file}.bytes";
                if (!File.Exists(path))
                {
                    Debug.LogError($"--- 找不到数据,先生成luban");
                }
            }
            else
            {
                path = $"{XCPathConfig.GetGameConfigDir()}/Luban/bytes/{file}.bytes";
            }
            return path;
        }
    }
}