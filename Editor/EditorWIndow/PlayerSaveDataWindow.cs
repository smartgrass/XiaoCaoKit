using System.Collections.Generic;
using AssetEditor.Editor;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

public class PlayerSaveDataWindow : XiaoCaoWindow
{
    public PlayerSaveData playerSaveData;

    [SerializeField] private int progressChapter;
    [SerializeField] private int progressLevel = 1;

    public override bool IsDebugView => true;

    public override object DrawDebugTarget => playerSaveData;

    public override void OnEnable()
    {
        EnsurePlayerSaveDataLoaded();
        base.OnEnable();
    }

    public override void DrawHead()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("调试修改存档进度", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("章节从 0 开始，首章首关为 0 / 1。点击修改后，会把该章节的最高已通关关卡推进到输入的关卡序号。", MessageType.Info);

        progressChapter = EditorGUILayout.IntField("章节序号", progressChapter);
        progressLevel = EditorGUILayout.IntField("关卡序号", progressLevel);

        using (new EditorGUI.DisabledScope(progressChapter < 0 || progressLevel < 1))
        {
            if (GUILayout.Button("修改进度"))
            {
                ModifyProgress();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    [MenuItem(XCEditorTools.PlayerSaveDataWindow)]
    public static PlayerSaveDataWindow Open()
    {
        return OpenWindow<PlayerSaveDataWindow>("玩家存档面板");
    }

    [Button(Pos: 1)]
    private void Read()
    {
        LoadPlayerSaveData();
    }

    [Button(Pos: 1)]
    private void Save()
    {
        EnsurePlayerSaveDataLoaded();
        SaveMgr.SaveData(playerSaveData);
        SyncRuntimeSaveData();
        Debug.Log("-- save");
    }

    [Button("重置", Pos: 1)]
    private void ResetData()
    {
        playerSaveData = new PlayerSaveData();
        InitializePlayerSaveData(playerSaveData);
        Save();
    }

    [Button("解锁全部关卡", Pos: 2)]
    private void UnlockAllLevel()
    {
        EnsurePlayerSaveDataLoaded();
        GameDebugTool.UnlockAllLevel(playerSaveData);
        Save();
    }

    private void ModifyProgress()
    {
        if (progressChapter < 0 || progressLevel < 1)
        {
            Debug.LogError($"-- invalid progress input chapter={progressChapter} level={progressLevel}");
            return;
        }

        EnsurePlayerSaveDataLoaded();
        playerSaveData.levelPassData.SetPassState(progressChapter, progressLevel);
        Save();
        Debug.Log($"-- set progress to chapter={progressChapter} level={progressLevel}");
    }

    private void EnsurePlayerSaveDataLoaded()
    {
        if (playerSaveData == null)
        {
            LoadPlayerSaveData(false);
        }
        else
        {
            InitializePlayerSaveData(playerSaveData);
        }
    }

    private void LoadPlayerSaveData(bool isLog = true)
    {
        playerSaveData = SaveMgr.ReadData<PlayerSaveData>(out var isSuc);
        InitializePlayerSaveData(playerSaveData);

        if (isLog)
        {
            Debug.Log($"-- read {isSuc} ");
        }
    }

    private void SyncRuntimeSaveData()
    {
        if (Application.isPlaying)
        {
            GameAllData.playerSaveData = playerSaveData;
        }
    }

    private static void InitializePlayerSaveData(PlayerSaveData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.storyProgress == null)
        {
            data.storyProgress = new StoryProgress();
        }

        if (data.levelPassData == null)
        {
            data.levelPassData = new LevelPassData();
        }
        data.levelPassData.CheckNull();

        if (data.levelPassData.chapterPassDic == null)
        {
            data.levelPassData.chapterPassDic = new Dictionary<int, int>();
        }

        if (data.skillUnlockDic == null)
        {
            data.skillUnlockDic = new Dictionary<string, int>();
        }

        if (data.skillBarSetting == null)
        {
            data.skillBarSetting = new List<string>();
        }

        if (data.inventory == null)
        {
            data.inventory = new Inventory();
        }

        if (data.holdItems == null)
        {
            data.holdItems = new List<Item>();
        }

        if (data.equippedHolyRelics == null)
        {
            data.equippedHolyRelics = new List<Item>();
        }
    }
}
