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

    //任务调试: 指定要完成的任务id, 留空表示全部任务
    [SerializeField] private string taskDebugId = "";

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
        EditorGUILayout.HelpBox("章节从 0 开始，首章首关为 0 / 1。点击修改后，会把该章节的通关进度直接设置为输入的关卡序号（可前进或回退），并清空所有未通关关卡的星星记录。", MessageType.Info);

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

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("任务调试", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("完成任务会把任务进度直接置为可领取状态(不自动领奖)。任务id留空时作用于全部任务; 清空记录会移除所有任务的进度与领取状态。", MessageType.Info);

        taskDebugId = EditorGUILayout.TextField("任务id", taskDebugId);

        if (GUILayout.Button("完成任务"))
        {
            CompleteTaskDebug();
        }

        if (GUILayout.Button("清空任务记录"))
        {
            EnsurePlayerSaveDataLoaded();
            TaskMgr.DebugClearAll(playerSaveData);
            Save();
            Debug.Log("-- task records cleared");
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 调试: 完成指定任务(或全部任务), 完成后写存档并同步运行时数据
    /// </summary>
    private void CompleteTaskDebug()
    {
        EnsurePlayerSaveDataLoaded();
        if (string.IsNullOrEmpty(taskDebugId))
        {
            TaskMgr.DebugCompleteAll(playerSaveData);
            Debug.Log("-- all tasks completed");
        }
        else if (!TaskMgr.DebugCompleteTask(playerSaveData, taskDebugId))
        {
            Debug.LogError($"-- no task config {taskDebugId}");
            return;
        }
        else
        {
            Debug.Log($"-- task completed {taskDebugId}");
        }
        Save();
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
        playerSaveData = PlayerSaveData.CreateAndCheckNull();
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
        //覆盖式设置进度, 允许回退
        playerSaveData.levelPassData.SetPassProgress(progressChapter, progressLevel);
        //清空未通关关卡残留的星星记录(无死亡通关/最快耗时)
        int clearCount = playerSaveData.levelPassData.ClearStarStatesOfUnpassLevels();
        Save();
        Debug.Log($"-- set progress to chapter={progressChapter} level={progressLevel}, cleared {clearCount} unpass star states");
    }

    private void EnsurePlayerSaveDataLoaded()
    {
        if (playerSaveData == null)
        {
            LoadPlayerSaveData(false);
        }
    }

    private void LoadPlayerSaveData(bool isLog = true)
    {
        playerSaveData = SaveMgr.ReadData<PlayerSaveData>(out var isSuc);
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
}
