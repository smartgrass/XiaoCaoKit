using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CharacterCaptureManager : MonoSingleton<CharacterCaptureManager>, IClearCache
{
    private static readonly Dictionary<string, ModelLoader> ModelLoaderDic = new Dictionary<string, ModelLoader>();

    //用于防止模型生成位置重叠
    private int _index;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        ModelLoaderDic.Clear();
    }

    /// <summary>
    /// 获取角色头像精灵
    /// </summary>
    /// <returns>角色头像精灵</returns>
    internal Texture GetSpeakerAvatar(string speakerName, string addTag)
    {
        if (string.IsNullOrEmpty(speakerName))
        {
            Debug.LogError("Speaker name is null or empty");
            return null;
        }

        if (speakerName == "Null")
        {
            return null;
        }

        var modelLoader = GetModelLoader(speakerName, addTag);

        return modelLoader.GetTexture();
    }

    /// <summary>
    /// 获取模型加载器
    /// </summary>
    /// <returns>模型加载器实例</returns>
    private ModelLoader GetModelLoader(string speakerName, string addTag)
    {
        if (string.IsNullOrEmpty(speakerName))
        {
            return null;
        }

        if (ModelLoaderDic.TryGetValue(speakerName, out var loader) && loader.addTag == addTag)
        {
            return loader;
        }
        else
        {
            return LoadNew(speakerName, addTag);
        }
    }

    private ModelLoader LoadNew(string speakerName, string addTag)
    {
        if (ModelLoaderDic.TryGetValue(speakerName, out var modelLoader))
        {
            return modelLoader;
        }

        ModelLoader loader = new ModelLoader();
        loader.addTag = addTag;
        loader.Init(speakerName);
        ModelLoaderDic[speakerName] = loader;
        return loader;
    }

    public static void Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        ModelLoaderDic.Remove(key);
    }

    /// <summary>
    /// 清理指定来源创建的角色捕获相机
    /// </summary>
    public void ClearCameras(string camTag)
    {
        var keysToRemove = new List<string>();
        foreach (var kv in ModelLoaderDic)
        {
            if (!string.IsNullOrEmpty(camTag) && kv.Value.addTag != camTag)
            {
                continue;
            }

            keysToRemove.Add(kv.Key);
        }

        foreach (var key in keysToRemove)
        {
            if (!ModelLoaderDic.TryGetValue(key, out var loader))
            {
                continue;
            }

            ModelLoaderDic.Remove(key);
            if (loader.cameraCapture)
            {
                Destroy(loader.cameraCapture.gameObject);
            }
        }
    }
}
