using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XiaoCao;

/// <summary>
/// Loading 场景里的场景加载器: 异步加载目标场景, 并在激活场景之前先盖上黑屏,
/// 保证切换顺序是"黑屏 -> 淡出显示新场景", 不会先闪一帧新场景画面再变黑。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public string sceneToLoad; // 要加载的场景名称  
    public TextMeshProUGUI text;
    public Slider slider;

    private AsyncOperation asyncLoad;

    // allowSceneActivation = false 时进度最多到 0.9, 此时资源已加载完但场景还没激活
    private const float ReadyProgress = 0.9f;

    //进度值有浮点误差, 留一点余量判断"已就绪", 避免死等
    private const float ReadyEpsilon = 0.001f;

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
        Debug.Log($"--- Start LoadSceneAsync  {sceneToLoad}");
        // 开始异步加载场景, 但先关掉自动激活, 等黑屏盖好后再手动激活  
        asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;

        // 等待直到场景加载完成(加载完资源后进度会停在 0.9 等激活)  
        while (asyncLoad.progress < ReadyProgress - ReadyEpsilon)
        {
            // 更新进度条和文本  
            float progress = asyncLoad.progress / ReadyProgress;
            slider.value = progress;
            text.text = $"LoadScene progress: {progress * 100:F2}%";
            // 等待下一帧  
            yield return null;
        }

        // 进度条先补满, 再盖黑屏  
        slider.value = 1f;
        text.text = "LoadScene progress: 100.00%";

        // 关键顺序: 先盖黑屏, 再激活新场景。新场景的第一帧就已经被黑屏遮住,
        // 之后由 ToRunningStateProcedure 里的 BlackScreen.FadeOutAnim() 淡出显示  
        BlackScreen.ShowBlack();

        // 等黑屏真正渲染出去(Overlay Canvas 在帧末绘制), 否则新场景首帧仍可能露出来  
        yield return null;

        // 激活新场景  
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"--- LoadScene done {sceneToLoad}");
    }
}
