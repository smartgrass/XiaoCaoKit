using System;
using System.Globalization;
using UnityEngine;
using XiaoCao;

public class ModelLoader
{
    public string roleKey;

    public int offsetIndex;

    public GameObject loadedModel; // 保存当前加载的模型引用

    public CameraCapture cameraCapture;

    // 引用配置数据
    public ModelConfigSo configSo => ConfigMgr.ModelConfigDataSo;

    public void Init(string key)
    {
        roleKey = key;

        // 检查是否有配置纹理
        bool hasConfigTexture = configSo.HasConfigTexture(roleKey);
        //没有配置则 创建相机
        if (!hasConfigTexture)
        {
            CreateCamera();
        }
    }

    public Texture GetTexture()
    {
        var config = configSo.GetOrDefault(roleKey);
        if (config.hasTexture)
        {
            return config.LoadTexture;
        }
        return cameraCapture.CaptureImage();
    }

    internal void CreateCamera()
    {
        GameObject CameraCaptureObject = ResMgr.LoadInstan("Assets/_Res/UI/Talk/CameraCapture.prefab");
        cameraCapture = CameraCaptureObject.GetComponent<CameraCapture>();
        cameraCapture.modelLoader = this;
        CameraCaptureObject.transform.position = new Vector3(0, -200, 0) + Vector3.right * offsetIndex * 50;
        LoadModel();
    }

    private void LoadModel()
    {
        loadedModel = Role.LoadModelByKey(roleKey);
        loadedModel.transform.SetParent(cameraCapture.transform, false);

        // 获取配置信息
        var config = configSo.GetOrDefault(roleKey);
        // 使用配置的位置、旋转和缩放偏移
        loadedModel.transform.localPosition = config.localPosition;
        loadedModel.transform.localEulerAngles = config.localEulerAngles;
        loadedModel.transform.localScale = config.size * Vector3.one;
    }

    /// <summary>
    /// 销毁当前加载的模型
    /// </summary>
    public void DestroyModel()
    {
        if (loadedModel != null)
        {
            GameObject.Destroy(loadedModel);
            loadedModel = null;
        }
    }
}