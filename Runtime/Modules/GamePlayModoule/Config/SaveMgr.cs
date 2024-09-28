using System;
using UnityEngine;
#if UNITY_EDITOR
#endif
namespace XiaoCao
{
    /// <summary>
    /// 存档
    /// </summary>
    public class SaveMgr
    {
        public static string GetSavaPath(Type t)
        {
            return $"{Application.persistentDataPath}/Data/{t.Name}.data";
        }
        public static T ReadData<T>(out bool isSuc) where T : new()
        {
            Type type = typeof(T);
            string path = GetSavaPath(type);
            if (FileTool.IsFileExist(path))
            {
                isSuc = true;
                return FileTool.DeserializeRead<T>(path);
            }
            else
            {
                isSuc = false;
                return new T();
            }
        }

        public static void SaveData<T>(T data)
        {
            Type type = typeof(T);
            string path = GetSavaPath(type);
            FileTool.CheckFilePathDir(path);
            FileTool.SerializeWrite(path, data);
        }

    }




    public interface ISaveUtility
    {
        void SaveInt(string key, int value);
        void SaveFloat(string key, float value);
        void SaveString(string key, string value);
        void SaveBool(string key, bool value);

        int LoadInt(string key, int defaultValue = 0);
        float LoadFloat(string key, float defaultValue = 0f);
        string LoadString(string key, string defaultValue = default);
        bool LoadBool(string key, bool defaultValue = false);

        void Delete();
    }





}
