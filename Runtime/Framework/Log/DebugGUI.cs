using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


public class DebugGUI : MonoBehaviour
{
    // 静态方法，用于在外部调用时自动获取单例对象并设置调试信息
    [Conditional("DEBUG")]
    public static void Debug(string key, params object[] value)
    {
#if DEBUG
        GetInstance().AddDebugInfo(key, value);
#endif
    }
#if DEBUG

    private GUIStyle guiStyle = new GUIStyle();
    private Dictionary<string, object> debugInfo = new Dictionary<string, object>();
    private float lastClearTime;

    private float SetStartY = 10;
    private float lineCount = 20; //行数决定字体大小
    private float clearInterval = 3;

    private float lineHeight;
    private float startY;

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
        //GUI.skin.font.fontSize * 
        guiStyle.fontSize = (int)(Screen.height / lineCount);
        lineHeight = guiStyle.fontSize;
        guiStyle.normal.textColor = Color.blue;

        // 调用示例
        AddDebugInfo("FPS", 1 / Time.deltaTime);
        // 添加更多的调试信息...
        startY = SetStartY;
        // 显示所有信息
        foreach (var kvp in debugInfo)
        {
            ShowDebug(kvp.Key, kvp.Value);
        }

        // 检查是否需要清空信息
        if (Time.time - lastClearTime > clearInterval)
        {
            ClearDebugInfo();
            lastClearTime = Time.time;
        }
    }

    public void AddDebugInfo(string key, params object[] value)
    {
        if (value != null)
        {
            debugInfo[key] = string.Join(" ", value);
        }
        else
        {
            debugInfo[key] = null;
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
    }
#endif
}