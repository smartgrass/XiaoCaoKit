using UnityEngine;
using UnityEditor;

public class ShaderVariantCollectorSetting : ScriptableObject
{
    private const string DefaultSaveRoot = "Assets/_Res";
    private const string DefaultSavePath = DefaultSaveRoot + "/MyShaderVariants.shadervariants";

    public static string GeFileSavePath(string packageName)
    {
        string key = $"{Application.productName}_{packageName}_GeFileSavePath";
        string savePath = EditorPrefs.GetString(key, GetDefaultSavePath(packageName));
        return string.IsNullOrWhiteSpace(savePath) ? GetDefaultSavePath(packageName) : savePath;
    }

    /// <summary>
    /// 获取包对应的默认着色器变体保存路径。
    /// </summary>
    public static string GetDefaultSavePath(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName) || packageName == ResMgr.DefaultPackage)
        {
            return DefaultSavePath;
        }

        return $"{DefaultSaveRoot}/MyShaderVariants_{packageName}.shadervariants";
    }

    public static void SetFileSavePath(string packageName, string savePath)
    {
        string key = $"{Application.productName}_{packageName}_GeFileSavePath";
        EditorPrefs.SetString(key, savePath);
    }

    public static int GeProcessCapacity(string packageName)
    {
        string key = $"{Application.productName}_{packageName}_GeProcessCapacity";
        return EditorPrefs.GetInt(key, 1000);
    }
    public static void SetProcessCapacity(string packageName, int capacity)
    {
        string key = $"{Application.productName}_{packageName}_GeProcessCapacity";
        EditorPrefs.SetInt(key, capacity);
    }
}
