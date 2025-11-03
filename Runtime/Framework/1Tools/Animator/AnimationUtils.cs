using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
using System.Linq;
#endif

public static class AnimationUtils
{
    /// <summary>
    /// 静态方法：添加并播放动画片段（会先检查是否已存在）
    /// </summary>
    /// <param name="animator">目标Animator组件</param>
    /// <param name="clip">要添加的动画片段</param>
    /// <param name="layerIndex">动画层索引，默认为0</param>
#if UNITY_EDITOR
    public static void AddAndPlayClip(Animator animator, AnimationClip clip, int layerIndex = 0)
    {
        // 验证参数有效性
        if (animator == null)
        {
            Debug.LogError("Animator组件不能为空！");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("动画片段不能为空！");
            return;
        }

        if (layerIndex < 0 || layerIndex >= animator.layerCount)
        {
            Debug.LogError($"无效的动画层索引：{layerIndex}");
            return;
        }

        // 获取Animator控制器
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("Animator没有关联有效的控制器！");
            return;
        }

        // 检查动画片段是否已存在于控制器中
        if (IsClipExistsInControllerPlay(controller, clip))
        {
            animator.Play(clip.name);
            return;
        }

        // 获取指定层的状态机
        AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;

        // 创建新的动画状态
        AnimatorState newState = stateMachine.AddState(clip.name);
        newState.motion = clip;

        // 创建从默认状态到新状态的过渡（无过渡时间）
        AnimatorStateTransition transition = stateMachine.defaultState.AddTransition(newState);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;

        // 播放新添加的动画
        animator.Play(clip.name, layerIndex);
    }
#endif

    /// <summary>
    /// 检查动画片段是否已存在于控制器中, 根据名字判断
    /// </summary>
#if UNITY_EDITOR
    private static bool IsClipExistsInControllerPlay(AnimatorController controller, AnimationClip clip)
    {
        // 检查所有层中的所有状态
        foreach (var layer in controller.layers)
        {
            // 递归检查状态机中的所有状态
            if (StateMachineContainsClip(layer.stateMachine, clip))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 递归检查状态机中是否包含指定动画片段
    /// </summary>
    private static bool StateMachineContainsClip(AnimatorStateMachine stateMachine, AnimationClip clip)
    {
        // 检查当前状态机中的状态
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion == clip && state.state.motion.name == clip.name)
            {
                return true;
            }
        }

        // 递归检查子状态机
        foreach (var childMachine in stateMachine.stateMachines)
        {
            if (StateMachineContainsClip(childMachine.stateMachine, clip))
            {
                return true;
            }
        }

        return false;
    }
#endif
}
