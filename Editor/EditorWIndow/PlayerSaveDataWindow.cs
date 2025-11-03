using System;
using AssetEditor.Editor;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

public class PlayerSaveDataWindow : XiaoCaoWindow
{
    public PlayerSaveData playerSaveData;

    public override bool IsDebugView => true;

    public override object DrawDebugTarget => playerSaveData;

    [MenuItem(XCEditorTools.PlayerSaveDataWindow)]
    public static PlayerSaveDataWindow Open()
    {
        return OpenWindow<PlayerSaveDataWindow>("玩家存档面板");
    }

    [Button(Pos: 1)]
    void Read()
    {
        playerSaveData = SaveMgr.ReadData<PlayerSaveData>(out var isSuc);
        if (!isSuc)
        {
            playerSaveData = new PlayerSaveData();
        }

        Debug.Log($"-- read {isSuc} ");
    }

    [Button(Pos: 1)]
    void Save()
    {
        SaveMgr.SaveData(playerSaveData);
        Debug.Log($"-- save");
    }

    
    [Button("重置",Pos: 1)]
    private void ResetData()
    {
        playerSaveData = new PlayerSaveData();
        Save();
    }
}