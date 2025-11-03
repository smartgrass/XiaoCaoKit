using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;

public class CameraCapture : MonoBehaviour
{
    [Header("相机设置")] public Camera captureCamera; // 用于拍摄的相机
    [Range(128, 2048)] public int textureWidth = 512; // 输出图像宽度
    [Range(128, 2048)] public int textureHeight = 512; // 输出图像高度

    public float modelDistance = 8;
    [ReadOnly]
    public CharacterImage characterImage;

    private RenderTexture _renderTexture; // 临时渲染纹理

    public ModelLoader ModelLoader { get; set; }

    public GameObject Model { get; set; }


    public const string PrefabPath = "Assets/_Res/UI/Talk/CameraCapture.prefab";
    //public Transform modelPR; // 定位模型位置

    private void Awake()
    {
        // 初始化渲染纹理
        _renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        captureCamera.targetTexture = _renderTexture;

        // 配置相机以获得透明背景
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0); // 透明背景
        captureCamera.orthographic = true; // 正交相机，避免透视变形
    }

    public void SetRenderTextureSize(int x, int y)
    {
        Debug.Log($"-- set size {x} {y}");
        textureWidth = x;
        textureHeight = y;
        _renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        captureCamera.targetTexture = _renderTexture;
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

    }


#if UNITY_EDITOR
    private string EditorGetKey => ModelLoader.roleKey;
    
    [Button("保存位置")]
    void EditorSaveTransformInfos()
    {
        SaveTranformConfig();
    }

    void SaveTranformConfig()
    {
        if (string.IsNullOrEmpty(EditorGetKey))
        {
            Debug.LogError("Key is null or empty");
            return;
        }


        // 确保我们正在修改一个可写的资源
        ModelConfigSo so =
            AssetDatabase.LoadAssetAtPath<ModelConfigSo>("Assets/XiaoCaoKit/Resources/ModelConfigSo.asset");
        int existingIndex = -1;
        ModelConfigEntry entry = null;
        for (int i = 0; i < so.array.Length; i++)
        {
            if (so.array[i].roleKey == EditorGetKey)
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
        entry.roleKey = EditorGetKey;

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

        Debug.Log($"Transform info saved for role: {EditorGetKey}");
    }

    [Button("保存位置和图片")]
    void EditorSaveTextureTo()
    {
        string path = XCPathConfig.GetRoleTexturePath(EditorGetKey);

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
        Texture2D capturedTexture = EditorGetTexture(captureCamera.targetTexture);

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

            UnityEditor.AssetDatabase.Refresh();
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

        SaveTranformConfig();
    }

    /// <summary>
    /// 拍摄并转换纹理为Texture2D
    /// </summary>
    /// <returns>Texture2D对象</returns>
    private Texture2D EditorGetTexture(RenderTexture texture)
    {
        // 拍摄并转换纹理为Texture2D
        RenderTexture.active = texture;
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
        return texture2D;
    }

#endif
}