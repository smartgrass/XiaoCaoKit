using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;

public class CameraCapture : MonoBehaviour
{
    [Header("相机设置")]
    public Camera captureCamera; // 用于拍摄的相机
    [Range(128, 2048)]
    public int textureWidth = 512;  // 输出图像宽度
    [Range(128, 2048)]
    public int textureHeight = 512; // 输出图像高度

    public float modelDistance = 8;

    private RenderTexture renderTexture; // 临时渲染纹理

    public ModelLoader modelLoader { get; set; }

    public string Key => modelLoader.roleKey;

    public GameObject Model => modelLoader.loadedModel;

    //public Transform modelPR; // 定位模型位置

    private void Awake()
    {
        // 初始化渲染纹理
        renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        captureCamera.targetTexture = renderTexture;

        // 配置相机以获得透明背景
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0); // 透明背景
        captureCamera.orthographic = true; // 正交相机，避免透视变形
    }

    /// <summary>
    /// 拍摄角色图像
    /// </summary>
    /// <returns>包含角色的透明背景纹理</returns>
    public Texture CaptureImage()
    {
        return captureCamera.targetTexture;
    }

    private void OnDestroy()
    {
        // 释放资源
        if (renderTexture != null)
            Destroy(renderTexture);
        
        CharacterCaptureManager.Inst.Remove(Key);
    }

    [Button("保存位置")]
    void SaveTranfromInfos()
    {
        SaveTranfromConfig(false);
    }

    void SaveTranfromConfig(bool hasTexture)
    {
        if (string.IsNullOrEmpty(Key))
        {
            Debug.LogError("Key is null or empty");
            return;
        }

#if UNITY_EDITOR
        // 确保我们正在修改一个可写的资源
        ModelConfigSo so = AssetDatabase.LoadAssetAtPath<ModelConfigSo>("Assets/XiaoCaoKit/Resources/ModelConfigSo.asset");
        int existingIndex = -1;
        ModelConfigEntry entry = null;
        for (int i = 0; i < so.array.Length; i++)
        {
            if (so.array[i].roleKey == Key)
            {
                entry = so.array[i];
                existingIndex = i;
                break;
            }
        }

        // 获取子对象中包含"Model"名称的Transform（假设模型是作为子对象放置的）
        Transform modelTransform = Model.transform;

        // 计算相对于相机位置的偏移
        Vector3 localPosition = modelTransform.localPosition;
        Vector3 localEulerAngles = modelTransform.localEulerAngles;

        // 获取模型的缩放信息
        float size = modelTransform.localScale.x; // 假设均匀缩放

        if (existingIndex < 0)
        {
            // 创建新条目
            entry = new ModelConfigEntry();
        }

        entry.localPosition = localPosition;
        entry.localEulerAngles = localEulerAngles;
        entry.size = size;
        entry.roleKey = Key;
        entry.hasTexture = hasTexture;

        if (existingIndex < 0)
        {
            // 扩展数组并添加新条目
            ModelConfigEntry[] newArray = new ModelConfigEntry[so.array.Length + 1];
            System.Array.Copy(so.array, newArray, so.array.Length);
            newArray[so.array.Length] = entry;
            so.array = newArray;
        }

        UnityEditor.EditorUtility.SetDirty(so);

        // 保存更改
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Transform info saved for role: {Key}");
#else
    Debug.LogWarning("SaveTranfromInfos can only be used in Unity Editor");
#endif
    }

    [Button("保存位置和图片")]
    void SaveTextureTo()
    {


        string path = XCPathConfig.GetRoleTexturePath(Key);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Path is null or empty");
            return;
        }

        if (captureCamera == null || captureCamera.targetTexture == null)
        {
            Debug.LogError("Capture camera or target texture is null");
            return;
        }

        // 拍摄图像
        Texture2D capturedTexture = CaptureAndConvertTexture();

        if (capturedTexture == null)
        {
            Debug.LogError("Failed to capture texture");
            return;
        }

        // 确保目录存在
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // 将纹理转换为PNG格式的字节数组
        byte[] pngData = capturedTexture.EncodeToPNG();

        if (pngData == null || pngData.Length == 0)
        {
            Debug.LogError("Failed to encode texture to PNG");
            DestroyImmediate(capturedTexture);
            return;
        }

        try
        {
            // 保存到文件
            System.IO.File.WriteAllBytes(path, pngData);
            Debug.Log($"Texture saved to: {path}");

            // 刷新资源数据库（仅在编辑器中）
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save texture: {e.Message}");
        }
        finally
        {
            // 释放临时纹理
            DestroyImmediate(capturedTexture);
        }

        SaveTranfromConfig(true);
    }

    /// <summary>
    /// 拍摄并转换纹理为Texture2D
    /// </summary>
    /// <returns>Texture2D对象</returns>
    private Texture2D CaptureAndConvertTexture()
    {
        // 保存当前的渲染目标
        RenderTexture currentRT = RenderTexture.active;

        try
        {
            // 设置相机的渲染目标
            RenderTexture.active = captureCamera.targetTexture;

            // 渲染相机视图
            captureCamera.Render();

            // 创建纹理并读取像素
            Texture2D capturedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            capturedTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            capturedTexture.Apply();

            return capturedTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error capturing texture: {e.Message}");
            return null;
        }
        finally
        {
            // 恢复原来的渲染目标
            RenderTexture.active = currentRT;
        }
    }
}
