using System;
using System.Globalization;
using UnityEngine;
using XiaoCao;
using static ModelConfigSo;

[CreateAssetMenu(fileName = "ModelConfigData", menuName = "SO/ModelConfigData")]
public class ModelConfigSo : KeyMapSo<ModelConfigEntry>
{
    // 检查是否有预配置纹理
    public bool HasConfigTexture(string roleKey)
    {
        var config = GetOrDefault(roleKey);
        return config.hasTexture;
    }
}


[Serializable]
public class ModelConfigEntry : IKey
{
    public string roleKey;
    public bool hasTexture;

    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public float size = 1;


    public string Key => roleKey;

    public Texture2D LoadTexture
    {
        get
        {
            return ResMgr.LoadAseet<Texture2D>(XCPathConfig.GetRoleTexturePath(roleKey));
        }
    }
}