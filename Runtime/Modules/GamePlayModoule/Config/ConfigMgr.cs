using System;
using UnityEngine;
using UnityEngine.XR;
using XiaoCao;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
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
    }
}
