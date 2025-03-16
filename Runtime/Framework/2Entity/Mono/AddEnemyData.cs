using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class AddEnemyData: MonoBehaviour
{
    [OnValueChanged(nameof(Check))]
    public int aiId = 0;
    [XCLabel("出招表配置,默认走raceId")]
    public int cmdSettingId = -1;

    //"Debug View"
    [XCHeader("当前AI配置,输入aiId刷新")]
    [Label("")]
    [NaughtyAttributes.ReadOnly]
    public ActPoolFSM CurAi;

#if UNITY_EDITOR
    void Check()
    {
        string configPath = XCPathConfig.GetAIPath(aiId);
        CurAi = UnityEditor.AssetDatabase.LoadAssetAtPath<ActPoolFSM>(configPath);
    }
#endif
}
