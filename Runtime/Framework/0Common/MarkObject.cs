using System.Collections.Generic;
using UnityEngine;

public class MarkObject : MonoBehaviour
{
    public string key;

    public static Dictionary<string,GameObject> MarkDic = new Dictionary<string,GameObject>();

    private void Awake()
    {
        if (!string.IsNullOrEmpty(key))
        {
            MarkDic[key] = gameObject;
        }
    }

    public static bool TryGet(string key,out GameObject game)
    {
        if (MarkDic.ContainsKey(key))
        {
            game = MarkDic[key];
            return true;
        }
        game = null;
        return false;
    }

}
