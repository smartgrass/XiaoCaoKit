using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class SkillEditorLuncher : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"--- SkillEditorLuncher Awake");
        RunDemo demo = new RunDemo();
        demo.Run().Forget();
    }

}
