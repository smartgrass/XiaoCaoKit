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
    /// <param name="speakerName">角色名称</param>
    /// <returns>角色头像精灵</returns>
    internal Texture GetSpeakerAvatar(string speakerName)
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

        var modelLoader = GetModelLoader(speakerName);

        return modelLoader.GetTexture();
    }

    /// <summary>
    /// 获取模型加载器
    /// </summary>
    /// <param name="speakerName">角色名称</param>
    /// <returns>模型加载器实例</returns>
    private ModelLoader GetModelLoader(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
        {
            return null;
        }

        if (!ModelLoaderDic.TryGetValue(speakerName, out var loader))
        {
            return LoadNew(speakerName);
        }
        else
        {
            return loader;
        }
    }

    private ModelLoader LoadNew(string speakerName)
    {
        if (ModelLoaderDic.TryGetValue(speakerName, out var modelLoader))
        {
            return modelLoader;
        }

        ModelLoader loader = new ModelLoader();
        loader.offsetIndex = _index++;
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
}