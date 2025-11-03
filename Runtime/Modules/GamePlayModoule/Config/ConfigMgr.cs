using System;
using UnityEngine;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
using System.Collections.Generic;
using System.IO;
using DataFormat = OdinSerializer.DataFormat;
using System.Text;
using Newtonsoft.Json;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCao
{
    ///<see cref="MoveSettingSo"/>
    ///<see cref="XiaoCao.PlayerSettingSo"/>
    ///<see cref="LocalizeMgr"/>
    public class ConfigMgr
    {
        #region public

        public static StaticSettingSo StaticSettingSo
        {
            get
            {
                if (_staticSettingSo == null)
                {
                    _staticSettingSo = LoadSoConfig<StaticSettingSo>();
                }

                return _staticSettingSo;
            }
        }

        public static PlayerSettingSo PlayerSettingSo;

        public static SkillDataSo SkillDataSo;

        public static AttrSettingSo CommonSettingSo;

        public static RewardPoolSo EnemyKillRewardSo;

        public static BuffConfigSo BuffConfigSo;

        public static ModelConfigSo ModelConfigDataSo;

        public static List<string> SkinList;

        public static List<string> TestEnmeyList;

        #endregion

        #region private

        private static IniFile _mainConfig;

        private static InitArrayFile _soundCfg;

        private static LocalSetting _localSetting;

        private static StaticSettingSo _staticSettingSo;
        
        private static UIPrefabSo _uiPrefabSo;

        #endregion


        public static void Init()
        {
            var init = MainCfg;
            PlayerSettingSo = ConfigMgr.LoadSoConfig<PlayerSettingSo>();
            CommonSettingSo = ConfigMgr.LoadSoConfig<AttrSettingSo>();
            SkillDataSo = ConfigMgr.LoadSoConfig<SkillDataSo>();
            EnemyKillRewardSo = ConfigMgr.LoadSoConfig<RewardPoolSo>();
            BuffConfigSo = ConfigMgr.LoadSoConfig<BuffConfigSo>();
            ModelConfigDataSo = ConfigMgr.LoadSoConfig<ModelConfigSo>();
            _uiPrefabSo = ConfigMgr.LoadSoConfig<UIPrefabSo>();
            var soundCfg = SoundCfg;
            GetSkinList();
            GetTestEnemyList();
        }

        public static string GetTalkChapter(string chapterId)
        {
            string strFullPath = XCPathConfig.GetGameConfigFile($"Talk/{chapterId}.txt");
            return FileTool.ReadFileString(strFullPath);
        }

        private static void GetSkinList()
        {
            List<string> list = new List<string>();
            list.Add("Body_Skin_0");
            foreach (IniSection section in ConfigMgr.MainCfg.SectionList)
            {
                if (section.SectionName.StartsWith("Mod"))
                {
                    foreach (var key in section.Dic.Keys)
                    {
                        if (key.StartsWith("Role") || key.StartsWith("E_"))
                        {
                            list.Add(key);
                        }
                    }
                }
            }

            SkinList = list;
        }

        private static void GetTestEnemyList()
        {
            var config = ConfigMgr.MainCfg.GetSection("TestEnmey");
            if (config == null)
            {
                TestEnmeyList = new List<string>();
                return;
            }

            List<string> list = new List<string>();
            list.Add("--");
            list.AddRange(config.Dic.Keys);
            TestEnmeyList = list;
        }

        public static string GetSettingSkinName()
        {
            int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
            return GetSkinName(index);
        }

        public static string GetSkinName(int index)
        {
            if (index == 0)
            {
                return "Role_0";
            }

            return SkinList[index % SkinList.Count];
        }

        public static string GetTestEnmeyName(int index)
        {
            if (index == 0)
            {
                return "P_0";
            }

            return TestEnmeyList[index % TestEnmeyList.Count];
        }

        public static string GetConfigPath(Type t)
        {
            return $"{ResMgr.RESDIR}/Config/{t.Name}";
        }

        public static T LoadSoConfig<T>(bool isResources = true, bool autoNew = true) where T : ScriptableObject
        {
            T ret = null;
            Type type = typeof(T);
            if (isResources)
            {
                ret = Resources.Load<T>(type.Name);
            }
            else
            {
                ret = ResMgr.LoadAseet(GetConfigPath(type)) as T;
            }

            if (autoNew && null == ret)
            {
                ret = ScriptableObject.CreateInstance<T>();
#if UNITY_EDITOR
                string path = isResources ? $"Assets/Resources/{type.Name}.asset" : GetConfigPath(type);
                Debug.LogError($"--- CreateAsset {path}");
                AssetDatabase.CreateAsset(ret, path);
#endif
            }

            return ret;
        }

        public static IniFile MainCfg
        {
            get
            {
                if (_mainConfig == null)
                {
                    IniFile ini = new IniFile();
                    ini.LoadFromFile("main.ini");
                    _mainConfig = ini;
                }

                return _mainConfig;
            }
        }

        public static InitArrayFile SoundCfg
        {
            get
            {
                if (_soundCfg == null)
                {
                    InitArrayFile ini = new InitArrayFile();
                    ini.LoadFromFile("sound.ini");
                    _soundCfg = ini;
                }

                return _soundCfg;
            }
        }

        public static LocalSetting LocalSetting
        {
            get
            {
                if (_localSetting == null)
                {
                    _localSetting = LocalSetting.Load();
                }

                return _localSetting;
            }
        }

        private static LocalRoleSetting _localRoleSetting;

        public static LocalRoleSetting LocalRoleSetting
        {
            get
            {
                if (_localRoleSetting == null)
                {
                    _localRoleSetting = LocalRoleSetting.Load();
                }

                return _localRoleSetting;
            }
        }
    }

    //角色相关的本地设置 , 随时清空, 比如技能图标,按键设置
    public class LocalRoleSetting
    {
        public bool saveSkillBar;

        public List<string> skillBarSetting;
        
        public int selectRole;
        
        public static LocalRoleSetting Load()
        {
            var ret = SaveMgr.ReadData<LocalRoleSetting>(out bool isSuc);
            if (!isSuc)
            {
            }

            //修改时, 需要修改saveSkillBar ,恢复默认则清除saveSkillBar
            SkillDataSo dataSo = ConfigMgr.LoadSoConfig<SkillDataSo>();
            if (!ret.saveSkillBar)
            {
                int raceId = 0;
                AiSkillCmdSetting AiCmdSetting = ConfigMgr.LoadSoConfig<AiCmdSettingSo>().GetOrDefault(raceId, 0);
                ret.skillBarSetting = AiCmdSetting.cmdSkillList;
            }

            return ret;
        }

        public int GetFriendRoleIndex()
        {
            //与当前角色不同
            return (selectRole+1) % GetRoleCount();
        }

        //根据关卡进度获取, 最少为2
        public int GetRoleCount()
        {
            return 2;
        }
        
        public void Sava()
        {
            SaveMgr.SaveData<LocalRoleSetting>(this);
        }
        
        public static void Save()
        {
            SaveMgr.SaveData<LocalRoleSetting>(ConfigMgr.LocalRoleSetting);
        }
    }

    public class LocalSetting
    {
        [OdinSerialize] public Dictionary<string, float> floatDic = new Dictionary<string, float>();

        public float GetValue(string key, float defaultValue)
        {
            if (floatDic.TryGetValue(key, out float value))
            {
                return value;
            }

            return defaultValue;
        }

        public bool GetBoolValue(string key)
        {
            float value = GetValue(key, 0);
            return value > 0;
        }

        public void SetBoolValue(string key, bool isOn)
        {
            SetValue(key, isOn ? 1 : 0);
        }

        public void SetValue(string key, float value)
        {
            floatDic[key] = value;
        }

        public static LocalSetting Load()
        {
            var ret = SaveMgr.ReadData<LocalSetting>(out bool isSuc);
            if (!isSuc)
            {
            }

            return ret;
        }

        public static void SaveSetting()
        {
            SaveMgr.SaveData<LocalSetting>(ConfigMgr.LocalSetting);
        }

        public void SetDefaultSetting()
        {
            //TODO
        }
    }

    public enum GameVersionType
    {
        Office, //正式
        Demo, //试玩
        Debug //开发
    }
}


public sealed class OdinPlayerPrefs
{
    #region Singleton

    public const string Name = "OdinPlayerPrefs";

    static OdinPlayerPrefs()
    {
        absoluteDirectoryPath = Path.Combine(Application.persistentDataPath, "User");
        if (!Directory.Exists(absoluteDirectoryPath))
        {
            Directory.CreateDirectory(absoluteDirectoryPath);
        }

        fileFullName = Path.Combine(absoluteDirectoryPath, fileName);
        LoadData();
    }

    public OdinPlayerPrefs()
    {
    }

    #endregion

    public static string absoluteDirectoryPath;
    public const string fileName = "UserConfig";
    public static string fileFullName;

    public static DataFormat dataFormat = DataFormat.Binary;
    private static UserInfo userInfo;

    private const string defaultString = "";
    private const float defaultFloat = 0;
    private const int defaultInt = 0;

    [Serializable]
    public class UserInfo
    {
        public Dictionary<string, string> keyValuePairs_String = new Dictionary<string, string>();
        public Dictionary<string, float> keyValuePairs_Float = new Dictionary<string, float>();
        public Dictionary<string, int> keyValuePairs_Int = new Dictionary<string, int>();
    }


    private static void SaveData()
    {
        byte[] bytes = SerializationUtility.SerializeValue(userInfo, dataFormat);
        File.WriteAllBytes(fileFullName, bytes);
    }

    private static void LoadData()
    {
        if (!File.Exists(fileFullName))
        {
            userInfo = new UserInfo();
            return;
        }

        byte[] bytes = File.ReadAllBytes(fileFullName);
        userInfo = SerializationUtility.DeserializeValue<UserInfo>(bytes, dataFormat);
    }


    public static void DeleteAll()
    {
        userInfo.keyValuePairs_String.Clear();
        userInfo.keyValuePairs_Float.Clear();
        userInfo.keyValuePairs_Int.Clear();
        SaveData();
    }

    public static void DeleteKey(string key)
    {
        bool isNeedSaveData = false;
        if (userInfo.keyValuePairs_String.ContainsKey(key))
        {
            userInfo.keyValuePairs_String.Remove(key);
            isNeedSaveData = true;
        }

        if (userInfo.keyValuePairs_Float.ContainsKey(key))
        {
            userInfo.keyValuePairs_Float.Remove(key);
            isNeedSaveData = true;
        }

        if (userInfo.keyValuePairs_Int.ContainsKey(key))
        {
            userInfo.keyValuePairs_Int.Remove(key);
            isNeedSaveData = true;
        }

        if (isNeedSaveData)
        {
            SaveData();
        }
        else
        {
            Debug.LogWarning($"删除失败，没有找到指定Key:{key}");
        }
    }

    public static float GetFloat(string key)
    {
        return GetFloat(key, defaultFloat);
    }

    public static float GetFloat(string key, float defaultValue)
    {
        if (userInfo.keyValuePairs_Float.ContainsKey(key))
        {
            return userInfo.keyValuePairs_Float[key];
        }
        else
        {
            return defaultValue;
        }
    }

    public static int GetInt(string key)
    {
        return GetInt(key, defaultInt);
    }

    public static int GetInt(string key, int defaultValue)
    {
        if (userInfo.keyValuePairs_Int.ContainsKey(key))
        {
            return userInfo.keyValuePairs_Int[key];
        }
        else
        {
            return defaultValue;
        }
    }

    public static string GetString(string key)
    {
        return GetString(key, defaultString);
    }

    public static string GetString(string key, string defaultValue)
    {
        if (userInfo.keyValuePairs_String.ContainsKey(key))
        {
            return userInfo.keyValuePairs_String[key];
        }
        else
        {
            return defaultValue;
        }
    }

    public static bool HasKey(string key)
    {
        if (userInfo.keyValuePairs_String.ContainsKey(key))
        {
            return true;
        }

        if (userInfo.keyValuePairs_Float.ContainsKey(key))
        {
            return true;
        }

        if (userInfo.keyValuePairs_Int.ContainsKey(key))
        {
            return true;
        }

        return false;
    }

    public static void SetFloat(string key, float value)
    {
        userInfo.keyValuePairs_Float[key] = value;
        SaveData();
    }

    public static void SetInt(string key, int value)
    {
        userInfo.keyValuePairs_Int[key] = value;
        SaveData();
    }

    public static void SetString(string key, string value)
    {
        userInfo.keyValuePairs_String[key] = value;
        SaveData();
    }
}