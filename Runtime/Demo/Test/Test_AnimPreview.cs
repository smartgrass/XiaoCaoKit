using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.VersionControl;
#endif
using UnityEngine;
using XiaoCao;

public class Test_AnimPreview : MonoBehaviour
{
#if UNITY_EDITOR
    [ShowNativeProperty]
    public bool GetBool => true;
    public Object[] dirs; // 包含文件夹路径的Object数组
    [HorLayout(true)]

    public GameObject characterPrefab; // 你的角色预制体
    [HorLayout(false)]
    public int count = 10;

    [Label("加载Role_1")]
    public bool isLoadRole1; // 勾选后不使用 characterPrefab, 改用 Role 加载 body 的方式获取 Role_1

    [Label("隐藏武器")]
    public bool isHideWeapon; // 勾选后执行 Npc.SetWeapon(false) 的逻辑, 隐藏模型上的武器

    [Label("行数")]
    //[NewLine]
    //[HorLayout(true)]
    [MiniBtn(nameof(LoadAnimations), "生成模型")]
    public int onelineCount = 7;

    private List<string> animationFiles = new List<string>();
    private List<GameObject> characters = new List<GameObject>();
    public int currentAnimationIndex = 0;

    void LoadAnimations()
    {
        Debug.Log($"--- 生成模型");
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

    [Button("播放下一组", enabledMode: EButtonEnableMode.Playmode)]
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
        else
        {
            Test_EditorNotifyHelper.ShowNotification($"anim end {currentAnimationIndex}");
        }
    }

    [Button("ResetAll", enabledMode: EButtonEnableMode.Playmode)]
    void ResetAll()
    {
        animationFiles = new List<string>();
        currentAnimationIndex = 0;
        for (int i = 0; i < characters.Count; i++)
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
            int lineIndex = i / onelineCount;
            // 使用局部坐标, 让生成位置跟随当前 transform 的位置与朝向
            Vector3 localPos = new Vector3((i % onelineCount) * 2 - 5, 0, lineIndex * 2);
            characters.Add(CreateCharacterInstance(localPos));
            // 给每个角色分配一个不同的动画
            Animator animator = characters[i].GetComponent<Animator>();
            animator.enabled = true;
            SetAnim(i, animator);
            if (isHideWeapon)
            {
                HideWeapon(animator);
            }
        }
    }

    /// <summary>
    /// 隐藏模型武器, 逻辑与 Npc.SetWeapon(false) 一致: 找到武器挂点并隐藏
    /// </summary>
    void HideWeapon(Animator animator)
    {
        var weaponTf = WeaponHelper.FindWeapon(animator);
        weaponTf.gameObject.SetActive(false);
    }


    /// <summary>
    /// 创建预览用的角色实例：默认实例化 characterPrefab，
    /// 勾选 isLoadRole1 时改用 Role 加载 body 的方式获取 Role_1 模型；
    /// 位置与朝向均使用局部坐标, 跟随当前 transform
    /// </summary>
    GameObject CreateCharacterInstance(Vector3 localPos)
    {
        GameObject instance = isLoadRole1
            ? Role.LoadModelByKey("Role_1")
            : Instantiate(characterPrefab, transform);

        instance.transform.SetParent(transform, false);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    /// <summary>
    /// 收集文件夹下的动画资源，兼容 FBX 与 Unity 原生 .anim 文件
    /// </summary>
    void LoadAnimationsFromFolder(string folderPath)
    {
        // 获取文件夹中的所有 FBX 动画文件
        string[] fbxFiles = Directory.GetFiles(folderPath, "*.FBX", SearchOption.AllDirectories);
        animationFiles.AddRange(fbxFiles);

        // 兼容搜索 .anim 文件
        string[] animFiles = Directory.GetFiles(folderPath, "*.anim", SearchOption.AllDirectories);
        animationFiles.AddRange(animFiles);
    }

    private void SetAnim(int i, Animator animator)
    {
        if (currentAnimationIndex < animationFiles.Count)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationFiles[currentAnimationIndex]);
            Debug.Log($"---  clip {clip}");
            animator.runtimeAnimatorController = CreateControllerWithClip(clip);
            characters[i].GetOrAddComponent<Test_ObjUsing>().objects.Add(clip);
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
