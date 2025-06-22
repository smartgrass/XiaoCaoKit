using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class AddEnemyData : MonoBehaviour
{
#if UNITY_EDITOR
    [OnValueChanged(nameof(Check))]
#endif
    [XCLabel("AI行为配置string")]
    public string aiId = "0";

    [XCLabel("属性配置")]
    public int attSettingId = 1;

    [XCLabel("skillCmd配置(较少改动)")]
    public int cmdSettingId = -1;

    //"Debug View"
    [XCHeader("当前AI配置,输入aiId刷新")]
    [Label("")]
    [NaughtyAttributes.ReadOnly]
    public ActPoolFSM CurAi;

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
        CurAi = UnityEditor.AssetDatabase.LoadAssetAtPath<ActPoolFSM>(configPath);
    }
#endif
}
