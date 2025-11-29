using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

public class Test_BattleDataCmd : GameStartMono
{
    [SerializeField]
    [MiniBtn(nameof(SetKeyNums))]
    public List<KeyNum> KeyNums = new List<KeyNum>() {
        new KeyNum() { key = BattleNumKeys.DamageMult_P, value = 1 },
        new KeyNum() { key = BattleNumKeys.DamageMult_E, value = 1 },
    };

    [MiniBtn(nameof(ChangeToEnemy))]
    public string testChangeToEnmey;

    public override void OnGameStart()
    {
        base.OnGameStart();

        SetKeyNums();

        ChangeToEnemy();
    }

    void SetKeyNums()
    {
        foreach (var kv in KeyNums)
        {
            BattleData.Current.tempNumDic[kv.key] = kv.value;
        }
    }


    void ChangeToEnemy()
    {
        if (string.IsNullOrEmpty(testChangeToEnmey))
        {
            return;
        }
        GameDataCommon.Current.Player0.ChangeToTestEnemy(testChangeToEnmey);
    }

}
[Serializable]
public struct KeyNum
{
    public string key;
    public float value;
}