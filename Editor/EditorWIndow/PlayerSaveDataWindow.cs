using System.Collections.Generic;
using AssetEditor.Editor;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

public class PlayerSaveDataWindow : XiaoCaoWindow
{
    //PlayerSaveData 虽然标了 [Serializable], 但里面有 Dictionary 字段, Unity 序列化不了它:
    //域重载(每次重新编译)后这个字段会变成 null, 再点保存就会把 null 写进存档变成 1 字节坏文件。
    //这里不参与序列化, 需要时由 EnsurePlayerSaveDataLoaded 从磁盘读回来
    [System.NonSerialized] public PlayerSaveData playerSaveData;

    [SerializeField] private int progressChapter;
    [SerializeField] private int progressLevel = 1;

    //任务调试: 指定要完成的任务id, 留空表示全部任务
    [SerializeField] private string taskDebugId = "";

    //支援角色羁绊等级(1~9): 三项细分养成等级由它反算, 玩家侧也只暴露这一层数值
    [SerializeField] private int supportBondLv = 1;

    //矿石调试: 选中的矿石类型
    [SerializeField] private ECrystalType oreType = ECrystalType.B;

    //每个品阶发放的矿石数量
    private const int OreCountPerQuality = 30;

    //矿石品阶上限: 图标与本地化只做到史诗(3), 再高的品阶没有资源
    private const int OreMaxQuality = (int)EQuality.Purple;

    public override bool IsDebugView => true;

    public override object DrawDebugTarget => playerSaveData;

    public override void OnEnable()
    {
        EnsurePlayerSaveDataLoaded();
        SyncSupportBondFields();
        base.OnEnable();
    }

    /// <summary>把存档中当前换算出的羁绊等级同步到窗口输入框</summary>
    private void SyncSupportBondFields()
    {
        if (playerSaveData == null)
        {
            return;
        }
        supportBondLv = SupportRoleSetting.GetBondLevel(
            playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Atk),
            playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Hp),
            playerSaveData.GetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Skill));
    }

    public override void DrawHead()
    {
        //域重载会清掉编辑器里非序列化的字段, 存档文件损坏时也可能读回 null;
        //先兜底再画, 避免下面取 bondExp 时空引用
        EnsurePlayerSaveDataLoaded();

        DrawSection("调试修改存档进度",
            "章节从 0 开始，首章首关为 0 / 1。点击修改后，会把该章节的通关进度直接设置为输入的关卡序号（可前进或回退），并清空所有未通关关卡的星星记录。进度由低到高时会补发跨越关卡的首通奖励（已领过的不重复发放）。",
            () =>
            {
                progressChapter = EditorGUILayout.IntField("章节序号", progressChapter);
                progressLevel = EditorGUILayout.IntField("关卡序号", progressLevel);

                using (new EditorGUI.DisabledScope(progressChapter < 0 || progressLevel < 1))
                {
                    if (GUILayout.Button("修改进度"))
                    {
                        ModifyProgress();
                    }
                }
            });

        DrawSection("任务调试",
            "完成任务会把任务进度直接置为可领取状态(不自动领奖)。任务id留空时作用于全部任务; 清空记录会移除所有任务的进度与领取状态。",
            () =>
            {
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
            });

        DrawSection("支援角色养成",
            "玩家可见的只有羁绊等级(1~9)，三项细分等级(攻击/生存/技巧)由羁绊等级反算后写入存档，不再单独调试。羁绊经验每通关一次 +1，满经验自动升级；直接设置等级会把当前经验清零。下次召唤支援角色时生效。",
            () =>
            {
                supportBondLv = EditorGUILayout.IntSlider("羁绊等级", supportBondLv, 1, SupportRoleSetting.MaxBondLevel);

                //细分等级是反算出来的内部数据, 这里只做显示, 方便核对升级换算
                SupportRoleSetting.GetGrowLevels(supportBondLv, out int atkLv, out int hpLv, out int skillLv);
                EditorGUILayout.LabelField("细分等级", $"攻击 Lv{atkLv} / 生存 Lv{hpLv} / 技巧 Lv{skillLv}");

                //羁绊经验: 通关一次 +1, 满经验自动升羁绊等级
                int bondNeed = SupportRoleSetting.GetNextLevelExp(supportBondLv);
                EditorGUILayout.LabelField("羁绊经验", bondNeed > 0 ? $"{playerSaveData.bondExp}/{bondNeed}" : "已满级");

                if (GUILayout.Button("应用羁绊等级"))
                {
                    ApplySupportBondLevel();
                }

                if (GUILayout.Button("获得 10 点羁绊经验"))
                {
                    AddBondExpDebug(10);
                }
            });

        DrawSection("矿石调试",
            $"按选中类型发放矿石，品阶 0~{OreMaxQuality} 各 {OreCountPerQuality} 个（0 普通 / 1 精良 / 2 稀有 / 3 史诗），直接进背包。",
            () =>
            {
                oreType = (ECrystalType)EditorGUILayout.EnumPopup("矿石类型", oreType);

                if (GUILayout.Button("增加矿石"))
                {
                    AddOreDebug();
                }
            });
    }

    /// <summary>
    /// 画一个调试分组。每个分组自带 Begin/End 与 try/finally:
    /// 某一组里抛异常时只记录日志并正常闭合分组, 不会让整窗的 GUILayout 状态失衡
    /// (失衡的表现就是 GUI Error: Invalid GUILayout state ... Begin/End calls match)
    /// </summary>
    private static void DrawSection(string title, string help, System.Action body)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        try
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(help, MessageType.Info);
            body();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
    }

    /// <summary>
    /// 调试: 按窗口里填的羁绊等级反算三项细分等级并写入存档
    /// </summary>
    private void ApplySupportBondLevel()
    {
        EnsurePlayerSaveDataLoaded();
        SupportRoleSetting.GetGrowLevels(supportBondLv, out int atkLv, out int hpLv, out int skillLv);
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Atk, atkLv);
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Hp, hpLv);
        playerSaveData.SetSupportGrowLevel(SupportRoleSetting.SupportGrowType.Skill, skillLv);
        //直接设定等级时经验归零, 否则带着上一级攒下的经验会立刻再升一级
        playerSaveData.bondExp = 0;
        playerSaveData.ReLoadPlayerAttr();
        Save();
        Debug.Log($"-- support bond level set: bondLv={supportBondLv}, atk={atkLv} hp={hpLv} skill={skillLv}");
    }

    /// <summary>
    /// 调试: 直接发放羁绊经验, 用于验证升级换算与经验条表现
    /// </summary>
    private void AddBondExpDebug(int value)
    {
        EnsurePlayerSaveDataLoaded();
        bool levelUp = playerSaveData.AddBondExp(value, false);
        playerSaveData.ReLoadPlayerAttr();
        Save();
        //升级会同步改写三项子等级, 这里回填输入框避免显示过期
        SyncSupportBondFields();
        Debug.Log($"-- bond exp +{value}, levelUp={levelUp}, bondLv={SupportRoleSetting.GetBondLevel()}");
    }

    /// <summary>
    /// 调试: 按选中类型发放矿石, 每个品阶各 <see cref="OreCountPerQuality"/> 个
    /// </summary>
    private void AddOreDebug()
    {
        EnsurePlayerSaveDataLoaded();
        for (int quality = 0; quality <= OreMaxQuality; quality++)
        {
            //矿石id形如 Crystal_B_1, 末位是品阶(CrystalItem.Create 内部按 '_' 拆解)
            string itemId = $"Crystal_{oreType}_{quality}";
            playerSaveData.inventory.AddItem(new Item(ItemType.Consumable, itemId, OreCountPerQuality, (EQuality)quality));
        }
        Save();
        Debug.Log($"-- ore added: Crystal_{oreType} x{OreCountPerQuality}, quality 0~{OreMaxQuality}");
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

        //Ensure 之后仍为 null 说明存档结构本身有问题, 绝不能把 null 写进文件
        if (playerSaveData == null)
        {
            Debug.LogError("-- save skipped: player save data is null");
            return;
        }

        SaveMgr.SaveData(playerSaveData);
        SyncRuntimeSaveData();
        Debug.Log("-- save");
    }

    [Button("重置", Pos: 1)]
    private void ResetData()
    {
        playerSaveData = PlayerSaveData.CreateAndCheckNull();
        Save();
        //设置虽然存在独立文件里, 但跟着存档走: 重置存档时一起回到默认值
        //(Play 模式下运行中的设置缓存也会同步替换, 不用重启游戏)
        LocalSetting.ResetToDefault();
        Debug.Log("-- reset save data & local setting");
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
                //经验值、心之结晶等直接作用在角色身上, 不进入背包; 存盘由窗口统一处理
                playerSaveData.AddOtherValue(item.typeId, item.num, false);
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
        bool isSuc = false;
        try
        {
            playerSaveData = SaveMgr.ReadData<PlayerSaveData>(out isSuc);
        }
        catch (System.Exception e)
        {
            //文件被截断/格式不对时反序列化也可能直接抛异常, 同样按损坏处理
            Debug.LogException(e);
            playerSaveData = null;
        }

        //存档文件为空/损坏时 Odin 反序列化返回 null(1 字节文件就是被写进去的 null)。
        //不兜底的话 DrawHead 取 bondExp 会空引用, 直接把 OnGUI 的 GUILayout 状态打坏;
        //顺便重建一份默认存档写回去, 让坏文件自愈
        if (playerSaveData == null)
        {
            playerSaveData = PlayerSaveData.CreateAndCheckNull();
            SaveMgr.SaveData(playerSaveData);
            Debug.LogWarning($"-- player save data on disk was empty/corrupt (isSuc={isSuc}), rebuilt a default one");
        }
        else
        {
            //文件不存在时 ReadData 返回的是裸 new, 旧存档也可能缺字段: 统一走一遍修复,
            //和游戏启动流程保持一致(重复调用是幂等的)
            playerSaveData.CheckNull();
        }

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
