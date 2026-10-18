using System;
using Luban;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static SkillUpgradeSettingReader SkillUpgradeSettingReader =>
            LubanTables.Inst.Tables.SkillUpgradeSettingReader;

        private static CreateEnemyGroupsReader CreateEnemyGroupsReader =>
            LubanTables.Inst.Tables.CreateEnemyGroupsReader;

        private static ItemInfoReader ItemInfoReader =>
            LubanTables.Inst.Tables.ItemInfoReader;

        private static ForgeRelicSettingReader ForgeRelicSettingReader =>
            LubanTables.Inst.Tables.ForgeRelicSettingReader;

        /// <summary>
        /// 获取全部圣器锻造配置。
        /// </summary>
        public static List<ForgeRelicSetting> GetAllForgeRelicSettings()
        {
            return ForgeRelicSettingReader.DataList;
        }

        /// <summary>
        /// 获取指定页面的圣器锻造配置，按行号升序；同一行内保持表内配置顺序。
        /// </summary>
        public static List<ForgeRelicSetting> GetForgeRelicSettings(int page)
        {
            List<ForgeRelicSetting> result = new List<ForgeRelicSetting>();
            List<ForgeRelicSetting> settings = ForgeRelicSettingReader.DataList;
            for (int i = 0; i < settings.Count; i++)
            {
                ForgeRelicSetting setting = settings[i];
                if (setting.Page == page)
                {
                    result.Add(setting);
                }
            }

            //List.Sort 不稳定，会把同一行内的配置顺序打乱；用稳定的 OrderBy 保证同 Row 内维持表内顺序
            return result.OrderBy(setting => setting.Row).ToList();
        }

        /// <summary>
        /// 根据物品 id 获取物品信息配置。
        /// </summary>
        /// <param name="itemId">物品配置 id。</param>
        /// <returns>物品信息配置；未找到时返回 null。</returns>
        public static ItemInfo GetItemInfo(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return ItemInfoReader.GetOrDefault(itemId);
        }

        /// <summary>
        /// 获取全部物品信息配置。
        /// </summary>
        /// <returns>物品信息配置列表。</returns>
        public static List<ItemInfo> GetAllItemInfo()
        {
            return ItemInfoReader.DataList;
        }

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

        public static List<int> GetAllChapters()
        {
            return new List<int>(ChapterSettingReader.DataMap.Keys);
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
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"--- LevelSetting key is null");
                return LevelSettingReader.DataList[0];
            }

            var ret = LevelSettingReader.GetOrDefault(key);
            if (ret == null)
            {
                Debuger.Log($"---! no find LevelSetting {key}");
                ret = LevelSettingReader.DataList[0];
            }

            return ret;
        }

        /// <summary>
        /// 获取关卡配置; 未找到时返回 null, 不做兜底
        /// 用于"配置为空即回退默认值"的场景(如地图名), 避免拿到其他关卡的配置
        /// </summary>
        public static LevelSetting GetLevelSettingOrDefault(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return LevelSettingReader.GetOrDefault(key);
        }

        public static List<LevelSetting> GetAllLevelSetting()
        {
            return LevelSettingReader.DataList;
        }

        public static SkillSetting GetSkillSetting(string skillId, int subSkillId)
        {
            string subKey = $"{skillId}_{subSkillId}";
            //子节点找不到则找父节点
            string mainKey = skillId.ToString();

            return GetSkillSetting(subKey, mainKey);
        }

        public static SkillSetting GetDefaultSkillSetting()
        {
            return SkillSettingReader.DataList[0];
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

        public static SkillUpgradeSetting GetSkillUpgradeSetting(string skillId)
        {
            var ret = SkillUpgradeSettingReader.GetOrDefault(skillId);
            if (ret == null)
            {
                //默认值
                ret = SkillUpgradeSettingReader.DataList[0];
            }

            return ret;
        }

        public static List<XiaoCao.Item> GetSkillUpgradeItems(string skillId, int lv)
        {
            var setting = GetSkillUpgradeSetting(skillId);

            int lvIndex = Mathf.Clamp(lv - 1, 0, setting.Cost.Count - 1);
            int coin = setting.Cost[lvIndex];

            XiaoCao.Item item = new XiaoCao.Item(XiaoCao.ItemType.Coin, nameof(XiaoCao.ItemType.Coin), coin);

            return new List<XiaoCao.Item>() { item };
        }

        public static List<XiaoCao.Item> GetRoleUpgradeItems(int lv)
        {
            int coin = (lv / 10 + 1) * 100;

            XiaoCao.Item item = new XiaoCao.Item(XiaoCao.ItemType.Coin, nameof(XiaoCao.ItemType.Coin), coin);

            return new List<XiaoCao.Item>() { item };
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

        /// <summary>
        /// 标记缓存失效, 下次访问 <see cref="Tables"/> 时重新加载。
        /// </summary>
        public void Reset()
        {
            _init = false;
        }

        /// <summary>
        /// 立即重新加载配置(运行时热重载): 丢弃已缓存的 Tables, 重新读取 bytes。
        /// </summary>
        public void Reload()
        {
            _init = false;
            Load();
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

            return new ByteBuf(FileTool.ReadByte(path));
        }

        private static string GetLubanPath(string file)
        {
            return $"{XCPathConfig.GetGameConfigDir()}/Luban/{file}.bytes";
        }
    }
}
