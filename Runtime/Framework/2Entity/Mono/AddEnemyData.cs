using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class AddEnemyData : MonoBehaviour
{
    [XCLabel("数值配置(boss为2)")] public int attSettingId = 1;

    [XCLabel("skillCmd配置(较少改动)")] public int cmdSettingId = -1;
#if UNITY_EDITOR
    [OnValueChanged(nameof(Check))]
#endif
    [XCLabel("AI行为配置string")]
    public string aiId = "0";
    //"Debug View"
    [XCLabel("输入aiId刷新,仅用于显示")] [NaughtyAttributes.ReadOnly]
    public ActPoolFSM debugAi;

    /// <summary>
    /// GetAiSkillCmdSetting
    /// </summary>
    /// <param name="fallBackId">RaceId</param>
    /// <returns></returns>
    public AiSkillCmdSetting GetAiSkillCmdSetting(int fallBackId)
    {
        int curCmdSettingId = cmdSettingId >= 0 ? cmdSettingId : fallBackId;
        AiSkillCmdSetting AiCmdSetting = ConfigMgr.LoadSoConfig<AiCmdSettingSo>().GetOrDefault(curCmdSettingId, 0);
        return AiCmdSetting;
    }

#if UNITY_EDITOR
    void Check()
    {
        string configPath = XCPathConfig.GetAIPath(aiId);
        var config = UnityEditor.AssetDatabase.LoadAssetAtPath<ActPoolFSM>(configPath);
        if (config != null)
        {
            debugAi = config;
        }
        else
        {
            Debug.LogError($"-- no {configPath}");
        }
    }
#endif
}