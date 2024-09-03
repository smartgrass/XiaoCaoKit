using System;
using UnityEngine;
using UnityEngine.XR;
using XiaoCao;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
using System.Collections.Generic;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif
namespace XiaoCao
{
    ///<see cref="MoveSettingSo"/>
    ///<see cref="PlayerSettingSo"/>
    public class ConfigMgr
    {
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

        public static PackageConfig GetPackageConfig()
        {
            string path = Application.dataPath + "Config/PackageConfig.text";
            byte[] bytes = File.ReadAllBytes(path);
            return SerializationUtility.DeserializeValue<PackageConfig>(bytes, DataFormat.JSON);
        }
    }


    public class PackageConfig
    {
        public Dictionary<string, string> config;
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


