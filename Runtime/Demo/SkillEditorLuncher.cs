using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class SkillEditorLuncher : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
        RunDemo demo = new RunDemo();
        demo.Run();
    }

}
