using System;
using UnityEngine;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
using System.Collections.Generic;
using System.IO;
using DataFormat = OdinSerializer.DataFormat;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace XiaoCao
{
    ///<see cref="MoveSettingSo"/>
    ///<see cref="PlayerSettingSo"/>
    ///<see cref="LocalizeMgr"/>
    public class ConfigMgr
    {
        private static IniFile mainConfig;

        private static InitArrayFile _soundCfg;

        private static LocalSetting _localSetting;

        public static PlayerSettingSo playerSettingSo;

        public static SkillDataSo skillDataSo;

        public static AttrSettingSo commonSettingSo;

        public static RewardPoolSo enemyKillRewardSo;

        public static BuffConfigSo buffConfigSo;

        public static List<string> SkinList;

        public static void Init()
        {
            var init = MainCfg;
            playerSettingSo = ConfigMgr.LoadSoConfig<PlayerSettingSo>();
            commonSettingSo = ConfigMgr.LoadSoConfig<AttrSettingSo>();
            skillDataSo = ConfigMgr.LoadSoConfig<SkillDataSo>();
            enemyKillRewardSo = ConfigMgr.LoadSoConfig<RewardPoolSo>();
            buffConfigSo = ConfigMgr.LoadSoConfig<BuffConfigSo>();
            var soundCfg = SoundCfg;
            GetSkinList();
        }

        private static void GetSkinList()
        {
            List<string> skinList = new List<string>();
            IniSection section = ConfigMgr.MainCfg.GetSection(ResMgr.DefaultPackage);
            string str = section.GetValue("SkinList", "");
            string[] array = str.Split(',');
            if (array.Length != 0)
            {
                skinList.AddRange(str.Split(','));
            }
            else
            {
                skinList.Add(str);
            }
            SkinList = skinList;
        }

        public static string GetSettingSkinName()
        {
            int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
            return GetSkinName(index);
        }

        public static string GetSkinName(int index)
        {
            if (SkinList.Count == 0)
            {
                return "Body_Skin_0";
            }
            return SkinList[index % SkinList.Count];
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
                if (mainConfig == null)
                {
                    IniFile ini = new IniFile();
                    ini.LoadFromFile("main.ini");
                    mainConfig = ini;
                }
                return mainConfig;
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

        public string GetBarSkillId(int index)
        {
            if (skillBarSetting.Count > index)
            {
                return skillBarSetting[index];
            }
            return "";
        }

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
                ret.skillBarSetting = dataSo.playerDefaultSkills;
            }
            if (dataSo.UseTestSkill)
            {
                ret.skillBarSetting = dataSo.testSkills;
            }

            return ret;
        }

        //TODO
        public static void SaveSetting()
        {
            SaveMgr.SaveData<LocalRoleSetting>(ConfigMgr.LocalRoleSetting);
        }
    }

    public class LocalSetting
    {
        [OdinSerialize]
        public Dictionary<string, float> floatDic = new Dictionary<string, float>();

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
    public OdinPlayerPrefs() { }

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


