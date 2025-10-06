using System;
using System.Globalization;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;

public class ModelLoader
{
    public string roleKey;

    public int offsetIndex;

    public GameObject loadedModel; // 保存当前加载的模型引用

    public CameraCapture cameraCapture;

    // 引用配置数据
    private ModelConfigSo ConfigSo => ConfigMgr.ModelConfigDataSo;

    public void Init(string key)
    {
        roleKey = key;

        // 检查是否有配置纹理
        bool hasConfigTexture = ConfigSo.HasConfigTexture(roleKey);
        //没有配置则 创建相机
        if (!hasConfigTexture)
        {
            CreateCamera();
        }
    }

    public Texture GetTexture()
    {
        var config = ConfigSo.GetOrDefault(roleKey);
        if (config.hasTexture)
        {
            return config.LoadTexture;
        }

        return cameraCapture.CaptureImage();
    }

    internal void CreateCamera()
    {
        GameObject cameraCaptureObject = ResMgr.LoadInstan(CameraCapture.PrefabPath);
        cameraCapture = cameraCaptureObject.GetComponent<CameraCapture>();
        cameraCaptureObject.transform.position = new Vector3(0, -200, 0) + Vector3.right * offsetIndex * 50;
        LoadModel();
        cameraCapture.Model = loadedModel;
        cameraCapture.ModelLoader = this;
    }

    private void LoadModel()
    {
        loadedModel = Role.LoadModelByKey(roleKey);
        loadedModel.transform.SetParent(cameraCapture.transform, false);

        // 获取配置信息
        var config = ConfigSo.GetOrDefault(roleKey);
        // 使用配置的位置、旋转和缩放偏移
        loadedModel.transform.localPosition = config.localPosition;
        loadedModel.transform.localEulerAngles = config.localEulerAngles;
        loadedModel.transform.localScale = config.size * Vector3.one;
        if (!string.IsNullOrEmpty(config.anim))
        {
            string path = XCPathConfig.GetAnimatorControllerPath(config.anim);
            var loadAc = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
            loadedModel.GetComponent<Animator>().runtimeAnimatorController = loadAc;
        }
    }

    /// <summary>
    /// 销毁当前加载的模型
    /// </summary>
    public void DestroyModel()
    {
        if (loadedModel != null)
        {
            Object.Destroy(loadedModel);
            loadedModel = null;
        }
    }
}