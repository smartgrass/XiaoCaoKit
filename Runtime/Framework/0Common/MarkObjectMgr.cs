using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

public class MarkObjectMgr : IClearCache
{
    private static readonly Dictionary<string, GameObject> MarkDic = new Dictionary<string, GameObject>();

    public static void Add(string key, GameObject game)
    {
        if (!string.IsNullOrEmpty(key))
        {
            MarkDic[key] = game;
        }
    }

    public static void Remove(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            MarkDic.Remove(key);
        }
    }

    public static bool TryGet(string key, out GameObject game)
    {
        if (MarkDic.ContainsKey(key))
        {
            game = MarkDic[key];
            return true;
        }

        game = null;
        return false;
    }

    /// <summary>
    /// 查找标记的GameObject 和 角色
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static GameObject GetById(string key)
    {
        if (TryGet(key, out GameObject game))
        {
            return game;
        }
        else if (key.StartsWith("role"))
        {
            int roleLen = "role".Length;
            string numStr = key.Substring(roleLen, key.Length - roleLen);
            Debug.Log($"-- id {numStr}");
            int id = int.Parse(numStr);
            if (RoleMgr.Inst.TryGetRole(id, out Role role))
            {
                return role.gameObject;
            }
        }

        return null;
    }


    public static void ClearCache()
    {
        MarkDic.Clear();
    }
}