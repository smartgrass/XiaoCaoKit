using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public enum ERewardExceType
{
    Buff,
    ExtraItem,
}

/// <summary>
/// 奖励执行脚本: 按奖励类型发放 Buff 或特殊道具(ExtraItem)
/// (配合 PlayerEnterTriggerChecker / ItemCrystal 等触发调用 Execute)
/// </summary>
public class RewardBuffExce : MonoExecute
{
    [Tooltip("奖励类型")]
    public ERewardExceType rewardType = ERewardExceType.Buff;

    [Header("Buff 奖励")]
    [Tooltip("Buff 候选池")]
    public List<EBuff> buffList = new List<EBuff>();

    [Tooltip("获取数量")]
    [MinValue(1)]
    public int num = 1;

    [Tooltip("Buff 等级, 0 时使用配置中的默认等级")]
    public int buffLevel;

    [Header("特殊道具奖励")]
    [Tooltip("特殊道具 id, 可从下拉框选择; None 表示不发放")]
    [Dropdown(nameof(GetExtraItemIdDropdown))]
    public string extraItemId = string.Empty;

    [Tooltip("获取后自动使用; 关闭则放入特殊道具槽, 由玩家手动使用")]
    public bool autoUseExtraItem;

    public override void Execute()
    {
        if (rewardType == ERewardExceType.Buff)
        {
            RewardBuffs();
        }
        else
        {
            RewardExtraItem();
        }
    }

    /// <summary>
    /// 从 Buff 候选池随机取 num 个发给本地玩家
    /// </summary>
    private void RewardBuffs()
    {
        if (buffList.Count == 0)
        {
            Debuger.LogError("--- RewardBuffExce buffList 为空");
            return;
        }

        var player = GameDataCommon.GetPlayer();
        foreach (EBuff eBuff in GetRandomBuffs(num))
        {
            BuffItem buffItem = BuffHelper.CreatBuffItem(eBuff);
            if (buffLevel > 0)
            {
                buffItem.level = buffLevel;
            }

            PlayerHelper.AddBuff(player.id, buffItem, true);
        }
    }

    /// <summary>
    /// 发放下拉框选中的特殊道具, 走战斗内特殊道具流程; 勾选 autoUseExtraItem 时获取后立即使用
    /// </summary>
    private void RewardExtraItem()
    {
        if (string.IsNullOrEmpty(extraItemId))
        {
            Debuger.LogWarning("--- RewardBuffExce extraItemId 为 None, 不发放特殊道具");
            return;
        }

        if (ConfigMgr.Inst.BattleExtraItemConfigSo?.GetConfig(extraItemId) == null)
        {
            Debuger.LogWarning($"--- RewardBuffExce 特殊道具配置不存在 {extraItemId}");
            return;
        }

        new XiaoCao.Item(ItemType.ExtraItem, extraItemId).RewardItem();

        if (!autoUseExtraItem)
        {
            return;
        }

        //CD 中或非战斗状态使用会失败, 此时道具留在槽位由玩家手动使用
        Player0 player = GameDataCommon.LocalPlayer;
        if (player == null || !player.TryUseExtraSkill(extraItemId))
        {
            Debuger.Log($"--- RewardBuffExce {extraItemId} 自动使用失败, 已放入特殊道具槽");
        }
    }

    /// <summary>
    /// 从候选池中随机取 count 个不重复的 Buff, 候选不足时取全部
    /// </summary>
    private List<EBuff> GetRandomBuffs(int count)
    {
        List<EBuff> pool = new List<EBuff>(buffList);
        List<EBuff> result = new List<EBuff>();

        while (result.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// 特殊道具 id 下拉选项, 首项 None(空值) 表示不发放
    /// </summary>
    private DropdownList<string> GetExtraItemIdDropdown()
    {
        DropdownList<string> options = new DropdownList<string>();
        options.Add("None", string.Empty);

        HashSet<string> existIds = new HashSet<string>();
        //配置表改动后可能残留旧 id, 保留在选项中避免显示错位
        AddItemId(options, existIds, extraItemId);

        BattleExtraItemConfigSo config = LoadBattleExtraItemConfig();
        if (config != null && config.list != null)
        {
            foreach (BattleExtraItemSubConfig subConfig in config.list)
            {
                if (subConfig != null)
                {
                    AddItemId(options, existIds, subConfig.id, subConfig.desc);
                }
            }
        }

        return options;
    }

    private void AddItemId(DropdownList<string> options, HashSet<string> existIds, string id,
        string desc = null)
    {
        if (string.IsNullOrEmpty(id) || !existIds.Add(id))
        {
            return;
        }

        options.Add(string.IsNullOrEmpty(desc) ? id : $"{id} ({desc})", id);
    }

    private BattleExtraItemConfigSo LoadBattleExtraItemConfig()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<BattleExtraItemConfigSo>(
                "Assets/XiaoCaoKit/Resources/BattleExtraItemConfigSo.asset");
        }
#endif

        BattleExtraItemConfigSo config = ConfigMgr.Inst.BattleExtraItemConfigSo;
        if (config != null)
        {
            return config;
        }

        return Resources.Load<BattleExtraItemConfigSo>("BattleExtraItemConfigSo");
    }
}
