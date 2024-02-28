using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;
using XiaoCao;

public class Test_AnimPreview : MonoBehaviour
{

#if UNITY_EDITOR

    public GameObject characterPrefab; // 你的角色预制体
    public Object[] dirs; // 包含文件夹路径的Object数组
    public int count = 10;

    private List<string> animationFiles = new List<string>();
    private List<GameObject> characters = new List<GameObject>();
    private int currentAnimationIndex = 0;

    [Button("生成模型", enabledMode: EButtonEnableMode.Playmode)]
    void LoadAnimations()
    {
        GetPaths();
        CreateCharacters();
    }

    private void GetPaths()
    {
        animationFiles.Clear();
        // 从Object[] dirs中获取文件夹路径
        foreach (Object dir in dirs)
        {
            string dirPath = AssetDatabase.GetAssetPath(dir);
            LoadAnimationsFromFolder(dirPath);
        }
    }

    [Button("播放下一组",enabledMode: EButtonEnableMode.Playmode)]
    void PlayRemainingAnimations()
    {
        GetPaths();
        // 检查是否还有未播放的动画
        if (currentAnimationIndex < animationFiles.Count)
        {
            // 给每个角色播放剩余的动画
            for (int i = 0; i < characters.Count; i++)
            {
                Animator animator = characters[i].GetComponent<Animator>();
                SetAnim(i, animator);
            }
        }
    }

    [Button("ResetAll", enabledMode: EButtonEnableMode.Playmode)]
    void ResetAll()
    {
        animationFiles = new List<string>();
        currentAnimationIndex = 0;
        for (int i = 0;i < characters.Count; i++)
        {
            GameObject.Destroy(characters[i]);
        }
        characters.Clear();
    }

    void CreateCharacters()
    {
        int startLen = characters.Count;
        for (int i = startLen; i < count; i++)
        {
            int oneLine = 8;
            int lineIndex = i / oneLine;
            Vector3 pos = new Vector3((i% oneLine) * 2 - 5,0 ,lineIndex * 2);
            characters.Add(Instantiate(characterPrefab, pos, Quaternion.identity, transform));
            // 给每个角色分配一个不同的动画
            Animator animator = characters[i].GetComponent<Animator>();
            animator.enabled = true;
            SetAnim(i, animator);
        }
    }
    void LoadAnimationsFromFolder(string folderPath)
    {
        // 获取文件夹中的所有文件
        string[] files = Directory.GetFiles(folderPath, "*.FBX", SearchOption.AllDirectories);
        animationFiles.AddRange(files);
    }

    private void SetAnim(int i, Animator animator)
    {
        if (currentAnimationIndex < animationFiles.Count)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationFiles[currentAnimationIndex]);
            Debug.Log($"---  clip {clip}");
            animator.runtimeAnimatorController = CreateControllerWithClip(clip);
            characters[i].GetOrAddComponent<Test_ObjUsing>().obj = clip;
            characters[i].gameObject.name = clip.name;
            currentAnimationIndex++;
        }
    }

    RuntimeAnimatorController CreateControllerWithClip(AnimationClip clip)
    {
        // 创建一个新的RuntimeAnimatorController，将动画文件设置为其默认动画
        AnimatorController controller = new AnimatorController();
        controller.AddLayer("1");
        AnimatorControllerLayer layer = controller.layers[0];
       
        // 添加一个新的状态机
        AnimatorStateMachine stateMachine = layer.stateMachine;
        AnimatorState state = stateMachine.AddState("Default");
        state.motion = clip;
        AnimatorStateTransition tr = new AnimatorStateTransition();
        tr.destinationState = state;
        state.AddTransition(tr);
        // 创建RuntimeAnimatorController
        //RuntimeAnimatorController runtimeController = AnimatorController.runti(controller);

        return controller as RuntimeAnimatorController;
    }

#endif
}
