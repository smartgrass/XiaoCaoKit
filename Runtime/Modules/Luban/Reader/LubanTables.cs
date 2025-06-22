using Luban;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using XiaoCao;

namespace cfg
{
    public class LubanTables : Singleton<LubanTables>
    {
        #region Get
        private static SkillSettingReader SkillSettingReader => LubanTables.Inst.Tables.SkillSettingReader;
        private static LevelSettingReader LevelSettingReader => LubanTables.Inst.Tables.LevelSettingReader;
        private static ChapterSettingReader ChapterSettingReader => LubanTables.Inst.Tables.ChapterSettingReader;
        private static SkillUpgradeSettingReader SkillUpgradeSettingReader => LubanTables.Inst.Tables.SkillUpgradeSettingReader;

        private static CreateEnemyGroupsReader CreateEnemyGroupsReader => LubanTables.Inst.Tables.CreateEnemyGroupsReader;

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

        public static CreateEnemyGroups GetCreateEnemyGroups(string key)
        {
            if (!CreateEnemyGroupsReader.DataMap.ContainsKey(key))
            {
                Debug.LogError($"--- no find group {key}");
            }

            return CreateEnemyGroupsReader.GetOrDefault(key);
        }

        public static LevelSetting GetLevelSetting(string key)
        {
            var ret = LevelSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                ret = LevelSettingReader.DataList[0];
            }
            return ret;
        }


        public static SkillSetting GetSkillSetting(string skillId, int subSkillId)
        {
            string subKey = $"{skillId}_{subSkillId}";
            //子节点找不到则找父节点
            string mainKey = skillId.ToString();

            return GetSkillSetting(subKey, mainKey);
        }

        public static SkillSetting GetSkillSetting(string key, string fallback)
        {
            var ret = SkillSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                ret = SkillSettingReader.GetOrDefault(fallback);
            }
            if (ret == null)
            {
                //默认值
                ret = SkillSettingReader.DataList[0];
            }
            return ret;
        }

        public static List<XiaoCao.Item> GetSkillUpgradeItems(string skillId)
        {
            Debug.LogError("---  TODO");
            return null;
            //var ret = SkillUpgradeSettingReader.GetOrDefault(skillId);
            //if (ret == null)
            //{
            //    return new List<XiaoCao.Item>();
            //}
            //return ret.NeedItems;
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

            return new ByteBuf(FileTool.WWWReadByteSync(path));
        }

        private static string GetLubanPath(string file)
        {
            return $"{XCPathConfig.GetGameConfigDir()}/Luban/{file}.bytes";
        }
    }
}