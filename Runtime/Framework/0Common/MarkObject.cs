using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

public class MarkObject : MonoBehaviour
{
    public string key;

    public static Dictionary<string, GameObject> MarkDic = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (!string.IsNullOrEmpty(key))
        {
            MarkDic[key] = gameObject;
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
            int len = "role".Length;
            string idStr = key.Substring(len - 1, key.Length - len);
            int id = int.Parse(idStr);
            if (RoleMgr.Inst.TryGetRole(id, out Role role))
            {
                return role.gameObject;
            }
        }
        return null;
    }


    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(key))
        {
            MarkDic.Remove(key);
        }
    }

}
