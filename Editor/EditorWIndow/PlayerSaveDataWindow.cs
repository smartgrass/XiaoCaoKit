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

    //支援角色三项养成等级(1~5)
    [SerializeField] private int supportAtkLv = 1;
    [SerializeField] private int supportHpLv = 1;
    [SerializeField] private int supportSkillLv = 1;

    public override bool IsDebugView => true;

    public override object DrawDebugTarget => playerSaveData;

    public override void OnEnable()
    {
        EnsurePlayerSaveDataLoaded();
        SyncSupportGrowFields();
        base.OnEnable();
    }

    /// <summary>把存档中当前的支援角色养成等级同步到窗口输入框</summary>
    private void SyncSupportGrowFields()
    {
        if (playerSaveData == null)
        {
            return;
        }
        supportAtkLv = playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Atk);
        supportHpLv = playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Hp);
        supportSkillLv = playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Skill);
    }

    public override void DrawHead()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("调试修改存档进度", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("章节从 0 开始，首章首关为 0 / 1。点击修改后，会把该章节的通关进度直接设置为输入的关卡序号（可前进或回退），并清空所有未通关关卡的星星记录。进度由低到高时会补发跨越关卡的首通奖励（已领过的不重复发放）。", MessageType.Info);

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

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("支援角色养成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("设置支援角色三项养成等级(1~5)。攻击/生存决定继承玩家属性的百分比，技巧决定可解锁技能数(每级1个,上限4个)，技巧满级后支援技能等级由1提升至2。下次召唤支援角色时生效。", MessageType.Info);

        supportAtkLv = EditorGUILayout.IntSlider("攻击等级", supportAtkLv, 1, 5);
        supportHpLv = EditorGUILayout.IntSlider("生存等级", supportHpLv, 1, 5);
        supportSkillLv = EditorGUILayout.IntSlider("技巧等级", supportSkillLv, 1, 5);

        if (GUILayout.Button("应用养成等级"))
        {
            ApplySupportGrowLevels();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 调试: 把窗口里填的三项支援角色养成等级写入存档
    /// </summary>
    private void ApplySupportGrowLevels()
    {
        EnsurePlayerSaveDataLoaded();
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Atk, supportAtkLv);
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Hp, supportHpLv);
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Skill, supportSkillLv);
        Save();
        Debug.Log($"-- support grow levels set: atk={supportAtkLv} hp={supportHpLv} skill={supportSkillLv}");
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
        LevelPassData levelPassData = playerSaveData.levelPassData;
        //修改前该章节已通关到的关卡, 用于判断进度是否由低到高
        int oldLevel = levelPassData.GetPassedLevelIndex(progressChapter);

        //覆盖式设置进度, 允许回退
        levelPassData.SetPassProgress(progressChapter, progressLevel);
        //清空未通关关卡残留的星星记录(无死亡通关/最快耗时)
        int clearCount = levelPassData.ClearStarStatesOfUnpassLevels();

        //进度前进时, 被跨越的关卡等同于已通关, 同步补发首通奖励
        List<string> grantedLevels = progressLevel > oldLevel
            ? GrantFirstRewards(levelPassData, progressChapter, oldLevel, progressLevel)
            : new List<string>();

        Save();
        Debug.Log($"-- set progress to chapter={progressChapter} level={progressLevel}, cleared {clearCount} unpass star states");
        if (grantedLevels.Count > 0)
        {
            Debug.Log($"---- first reward granted to {string.Join(", ", grantedLevels)}");
        }
    }

    /// <summary>
    /// 补发章节内被跨越关卡的首通奖励(只发未领过的关卡), 并标记为已领取
    /// </summary>
    /// <returns>实际补发了首通奖励的关卡名</returns>
    private List<string> GrantFirstRewards(LevelPassData levelPassData, int chapter, int oldLevel, int newLevel)
    {
        List<string> grantedLevels = new List<string>();
        for (int index = oldLevel + 1; index <= newLevel; index++)
        {
            string levelKey = LevelInfo.GetLevelKey(chapter, index);
            //配置里没有该关卡说明关卡序号越界, 直接结束补发(关卡序号是连续的)
            if (cfg.LubanTables.GetLevelSettingOrDefault(levelKey) == null)
            {
                Debug.LogWarning($"-- no level setting {levelKey}, stop granting first rewards");
                break;
            }

            //已领过首通奖励的关卡不重复发放
            if (levelPassData.HasGetFirstReward(chapter, index))
            {
                continue;
            }

            foreach (XiaoCao.Item item in LevelSettingHelper.GetFirstReward(levelKey))
            {
                GrantItemToSaveData(item);
            }

            //发放后立即标记, 避免重打该关卡时再领一次
            levelPassData.SetHasGetFirstReward(chapter, index);
            grantedLevels.Add(levelKey);
        }

        return grantedLevels;
    }

    /// <summary>
    /// 把奖励物品直接结算到当前存档对象上: 编辑模式没有战斗与运行时单例, 不能走 Item.RewardItem
    /// </summary>
    private void GrantItemToSaveData(XiaoCao.Item item)
    {
        switch (item.type)
        {
            case XiaoCao.ItemType.Coin:
                playerSaveData.Coin += item.num;
                break;
            case XiaoCao.ItemType.OtherValue:
                //经验值等直接作用在角色身上, 不进入背包; 存盘由窗口统一处理
                playerSaveData.AddExp(item.num, false);
                break;
            case XiaoCao.ItemType.Consumable:
            case XiaoCao.ItemType.HolyRelic:
                playerSaveData.inventory.AddItem(item);
                break;
            default:
                //Buff / ExtraItem 属于战斗内临时道具, 没有战斗上下文时不发放
                Debug.LogWarning($"-- first reward item skipped in editor: {item.type} {item.typeId} x{item.num}");
                break;
        }
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
