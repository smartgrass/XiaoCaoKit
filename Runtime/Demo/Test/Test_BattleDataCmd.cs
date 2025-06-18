using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

public class Test_BattleDataCmd : GameStartMono
{
    [SerializeField]
    public List<KeyNum> KeyNums = new List<KeyNum>() {
        new KeyNum() { key = BattleNumKeys.DamageMult_P, value = 1 },
        new KeyNum() { key = BattleNumKeys.DamageMult_E, value = 1 },
    };

    public override void OnGameStart()
    {
        base.OnGameStart();

        SetKeyNums();
    }

    void SetKeyNums()
    {
        foreach (var kv in KeyNums)
        {
            BattleData.Current.tempNumDic[kv.key] = kv.value;
        }
    }
}
[Serializable]
public struct KeyNum
{
    public string key;
    public float value;
}