using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;


public class DebugGUI : MonoBehaviour
{
    // 静态方法，用于在外部调用时自动获取单例对象并设置调试信息
    [Conditional("DEBUG")]
    public static void ShowInfo(string key, object value)
    {
        GetInstance().SetDebugInfo(key, value);
    }
#if DEBUG
    private GUIStyle guiStyle = new GUIStyle();
    private Dictionary<string, object> debugInfo = new Dictionary<string, object>();
    private float lastClearTime;
    private float clearInterval = 10f;

    private float startY = 10f;
    private float lineHeight = 20f;

    // 单例实例
    private static DebugGUI instance;

    // 获取DebugGUI实例的方法
    public static DebugGUI GetInstance()
    {
        if (instance == null)
        {
            instance = new GameObject("DebugGUI").AddComponent<DebugGUI>();
            DontDestroyOnLoad(instance.gameObject);
        }
        return instance;
    }

    void OnGUI()
    {
        guiStyle.fontSize = 18;
        guiStyle.normal.textColor = Color.white;

        // 检查是否需要清空信息
        if (Time.time - lastClearTime > clearInterval)
        {
            ClearDebugInfo();
            lastClearTime = Time.time;
        }

        // 调用示例
        SetDebugInfo("FPS", 1 / Time.deltaTime);
        // 添加更多的调试信息...

        // 显示所有信息
        foreach (var kvp in debugInfo)
        {
            ShowDebug(kvp.Key, kvp.Value);
        }
    }

    public void SetDebugInfo(string key, object value)
    {
        if (debugInfo.ContainsKey(key))
        {
            debugInfo[key] = value;
        }
        else
        {
            debugInfo.Add(key, value);
        }
    }

    private void ShowDebug(string key, object value)
    {
        GUI.Label(new Rect(10, startY, 300, lineHeight), $"{key}: {value}", guiStyle);
        startY += lineHeight;
    }

    private void ClearDebugInfo()
    {
        debugInfo.Clear();
        startY = 10f; // 重置显示位置
    }
#endif
}
