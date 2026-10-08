using System;
using System.IO;
using UnityEngine;
using XiaoCao;

[CreateAssetMenu(fileName = "ModelConfigData", menuName = "SO/ModelConfigData")]
public class RoleTextureConfigSo : KeyMapSo<ModelConfigEntry>
{
    [XCLabel("编辑使用,强制开启相机")]
    public bool IsDebugCamera = false;
    // 检查是否有预配置纹理
    public bool HasConfigTexture(string roleKey)
    {
        var config = GetOrDefault(roleKey);
#if UNITY_EDITOR
        if (IsDebugCamera)
        {
            return false;
        }
        if (config.hasTexture)
        {
            if (!File.Exists(XCPathConfig.GetRoleTexturePath(roleKey)))
            {
                Debug.LogError($"角色{roleKey}的配置纹理不存在");
                config.hasTexture = false;
            }
        }
        
#endif

        return config.hasTexture;
    }
}


[Serializable]
public class ModelConfigEntry : IKey
{
    public string roleKey;
    public string anim;
    public bool hasTexture;

    public Vector3 localPosition;
    public Vector3 localEulerAngles;

    public Vector3 cameraLocalPosition;
    public Vector3 cameraLocalEulerAngles;
    
    public float size = 1;


    public string Key => roleKey;

    public Texture2D LoadTexture
    {
        get { return ResMgr.LoadAseet<Texture2D>(XCPathConfig.GetRoleTexturePath(roleKey)); }
    }
}