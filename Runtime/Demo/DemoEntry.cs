using UnityEngine;
using Fantasy.Unity;
using Fantasy.Async;
using Fantasy;

public class DemoEntry : MonoBehaviour
{
    public Scene scene;

    private void Start()
    {
        StartAsync().Coroutine();
    }


    private async FTask StartAsync()
    {

        // 初始化框架
        Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
        // 创建用一个客户端的Scene没如果有个别同学不需要使用框架的Scene
        // 那就把Scene当网络接口使用。
        scene = await Fantasy.Platform.Unity.Entry.CreateScene();
    }
}
