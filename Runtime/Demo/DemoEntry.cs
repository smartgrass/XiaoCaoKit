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

        // ��ʼ�����
        Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
        // ������һ���ͻ��˵�Sceneû����и���ͬѧ����Ҫʹ�ÿ�ܵ�Scene
        // �ǾͰ�Scene������ӿ�ʹ�á�
        scene = await Fantasy.Platform.Unity.Entry.CreateScene();
    }
}
