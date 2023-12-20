using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class SkillEditorDemo : MonoBehaviour
{
    public UnityEvent StartDo;

    private void Awake()
    {
        gameObject.SetActive(false);


        StartDo?.Invoke();

        RunDemo demo = new RunDemo();
        demo.Run();
    }

}
