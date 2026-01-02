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
    public class ConfigMgr : Singleton<ConfigMgr>, IClearCache
    {
        #region public

        //在配置加载前加载 需要static
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

        private static StaticSettingSo _staticSettingSo;

        public static void ClearStatic()
        {
            _staticSettingSo = null;
        }

        public PlayerSettingSo PlayerSettingSo;

        public AttrSettingSo CommonSettingSo;

        public RewardPoolSo EnemyKillRewardSo;

        public BuffConfigSo BuffConfigSo;

        public ModelConfigSo ModelConfigDataSo;

        public List<string> SkinList;

        public List<string> TestEnmeyList;

        #endregion

        #region private

        private IniFile _mainConfig;

        public IniFile MainCfg
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

        public InitArrayFile SoundCfg
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

        private InitArrayFile _soundCfg;


        private UIPrefabSo _uiPrefabSo;

        #endregion

        public void Load()
        {
            Debug.Log($"-- ConfigMgr Load");
            PlayerSettingSo = LoadSoConfig<PlayerSettingSo>();
            CommonSettingSo = LoadSoConfig<AttrSettingSo>();
            EnemyKillRewardSo = LoadSoConfig<RewardPoolSo>();
            BuffConfigSo = LoadSoConfig<BuffConfigSo>();
            ModelConfigDataSo = LoadSoConfig<ModelConfigSo>();
            _uiPrefabSo = LoadSoConfig<UIPrefabSo>();
            GetSkinList();
            GetTestEnemyList();
            
            //增加断言 判断SkillDataSo,ModelConfigDataSo不为空
            Debug.Assert(ModelConfigDataSo != null, "ModelConfigDataSo is null");
        }

        public static string GetTalkChapter(string chapterId)
        {
            string strFullPath = XCPathConfig.GetGameConfigFile($"Talk/{chapterId}.txt");
            return FileTool.ReadFileString(strFullPath);
        }

        private void GetSkinList()
        {
            List<string> list = new List<string>();
            list.Add("Body_Skin_0");
            foreach (IniSection section in MainCfg.SectionList)
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

        private void GetTestEnemyList()
        {
            var config = MainCfg.GetSection("TestEnmey");
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

        public string GetSettingSkinName()
        {
            int index = (int)LocalSetting.GetValue(LocalizeKey.SkinList, 0);
            return GetSkinName(index);
        }

        public string GetSkinName(int index)
        {
            if (index == 0)
            {
                return "Role_0";
            }

            return SkinList[index % SkinList.Count];
        }

        public string GetTestEnmeyName(int index)
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


        public LocalSetting LocalSetting
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

        private LocalSetting _localSetting;

        private LocalRoleSetting _localRoleSetting;

        public LocalRoleSetting LocalRoleSetting
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

        public int selectRole;

        public static LocalRoleSetting Load()
        {
            var ret = SaveMgr.ReadData<LocalRoleSetting>(out bool isSuc);
            if (!isSuc)
            {
            }

            return ret;
        }

        public int GetFriendRoleIndex()
        {
            //与当前角色不同
            return (selectRole + 1) % GetRoleCount();
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
            SaveMgr.SaveData<LocalRoleSetting>(ConfigMgr.Inst.LocalRoleSetting);
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
            SaveMgr.SaveData<LocalSetting>(ConfigMgr.Inst.LocalSetting);
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