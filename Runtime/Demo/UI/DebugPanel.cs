using IngameDebugConsole;
using UnityEngine;
using XiaoCao;

public class DebugPanel : SubPanel
{
    private const string LogConsolePrefabName = "IngameDebugConsole";
    private const string InspectorPrefabName = "RuntimeInspector";
    private const string InspectorNodeName = "RuntimeInspector";

    public override void Init()
    {
        AddButton(LocalizeKey.DebugToggleLog, ToggleLogConsole, LocalizeKey.DebugOpenInspector, ToggleRuntimeInspector);
    }

    /// <summary>
    /// 开关 IngameDebugConsole: 已开启则关闭(销毁), 未开启则打开
    /// </summary>
    void ToggleLogConsole()
    {
        if (DebugLogManager.Instance != null)
        {
            // 销毁后 Instance 在下一次访问时即为 null, 可重新创建
            GameObject.Destroy(DebugLogManager.Instance.gameObject);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(LogConsolePrefabName);
        if (prefab == null)
        {
            Debuger.LogError($"未找到 Resources/{LogConsolePrefabName}.prefab");
            return;
        }

        GameObject.Instantiate(prefab);
    }

    /// <summary>
    /// 开关 RuntimeInspector: 已开启则关闭(销毁), 未开启则挂到 midCanvas 下打开
    /// </summary>
    void ToggleRuntimeInspector()
    {
        Transform parent = UIMgr.Inst.midCanvas.transform;

        Transform exist = parent.Find(InspectorNodeName);
        if (exist != null)
        {
            GameObject.Destroy(exist.gameObject);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(InspectorPrefabName);
        if (prefab == null)
        {
            Debuger.LogError($"未找到 Resources/{InspectorPrefabName}.prefab");
            return;
        }

        GameObject instance = GameObject.Instantiate(prefab, parent);
        instance.name = InspectorNodeName;
    }
}
