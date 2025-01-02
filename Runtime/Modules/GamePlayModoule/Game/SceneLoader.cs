using System.Collections;
using TMPro;
using UnityEngine;  
using UnityEngine.SceneManagement;  
using UnityEngine.UI;
using XiaoCao;

public class SceneLoader : MonoBehaviour  
{  
    public string sceneToLoad; // 要加载的场景名称  
    public TextMeshProUGUI text;
    public Slider slider;
  
    private AsyncOperation asyncLoad;

    private void Awake()
    { 
        LoadScene();
    }

    // 开始加载新场景  
    public void LoadScene()  
    {
        GameMgr.ClearSceneData();
        sceneToLoad = GameDataCommon.Current.NextSceneName;
        StartCoroutine(LoadSceneAsync());  
    }  
  
    // 异步加载场景的协程  
    private IEnumerator LoadSceneAsync()  
    {
        Debug.Log($"---  LoadSceneAsync  {sceneToLoad}");
        // 开始异步加载场景  
        asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);  
  
        // 等待直到场景加载完成  
        while (!asyncLoad.isDone)  
        {  
            // 更新进度条和文本  
            float progress = asyncLoad.progress;
            slider.value = progress;  
            text.text = $"Loading: {progress * 100:F2}%";  
  
            // 等待下一帧  
            yield return null;  
        }  
    }  
}