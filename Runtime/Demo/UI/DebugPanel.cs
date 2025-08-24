#if UNITY_EDITOR
#endif
using UnityEngine;
using XiaoCao;

public class DebugPanel : SubPanel
{
    public override void Init()
    {
        AddButton("Open RuntimeInspector", OpenRuntimeInspector, "Btns");
    }

    void OpenRuntimeInspector()
    {
        GameObject runtimeInspectorPrefab = Resources.Load<GameObject>("RuntimeInspector");

        Transform parent = UIMgr.Inst.midCanvas.transform;

        if (parent.Find("RuntimeInspector"))
        {
            return;
        }

        GameObject instance = GameObject.Instantiate(runtimeInspectorPrefab, UIMgr.Inst.midCanvas.transform);
        instance.name = "RuntimeInspector";
    }
}
