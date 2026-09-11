#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace XiaoCaoEditor
{
    /// <summary>
    /// 读取和修改角色动画机中的基础动画配置。
    /// </summary>
    public class BasicAnimationConfigWindow : EditorWindow
    {
        private const string MenuPath = "XiaoCao/资源工具/基础动画配置";
        private const string WindowTitle = "基础动画配置";

        /// <summary>
        /// 保存非基础 Clip 资源及其在动画机中的所有引用位置。
        /// </summary>
        private sealed class ExtraClipInfo
        {
            public AnimationClip Clip;
            public readonly List<string> Locations = new List<string>();
        }

        private AnimatorController controllerAsset;

        private AnimationClip idleClip;
        private AnimationClip walkClip;
        private AnimationClip runClip;
        private AnimationClip hitClip;
        private AnimationClip breakClip;
        private AnimationClip deadClip;
        private AnimationClip recoveryClip;

        private BlendTree idleBlendTree;
        private AnimatorState hitState;
        private AnimatorState breakState;
        private AnimatorState deadState;
        private AnimatorState recoveryState;

        private bool hasReadConfiguration;
        private string statusMessage = "请选择一个 AnimatorController，然后点击“读取”。";
        private MessageType statusType = MessageType.Info;
        private Vector2 scrollPosition;
        private readonly List<ExtraClipInfo> extraClipInfos = new List<ExtraClipInfo>();

        /// <summary>
        /// 打开基础动画配置窗口。
        /// </summary>
        [MenuItem(MenuPath, priority = 130)]
        public static void Open()
        {
            BasicAnimationConfigWindow window = GetWindow<BasicAnimationConfigWindow>(WindowTitle);
            window.minSize = new Vector2(520f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(520f, 460f);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawControllerSelector();
            DrawToolbar();

            if (hasReadConfiguration)
            {
                DrawAnimationFields();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "读取后会显示 Idle、Walk、Run、Hit、Break、Dead 和 Recovery 的动画槽位。",
                    MessageType.Info);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawControllerSelector()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("动画机资源", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            controllerAsset = EditorGUILayout.ObjectField(
                "AnimatorController",
                controllerAsset,
                typeof(AnimatorController),
                false) as AnimatorController;
            if (EditorGUI.EndChangeCheck())
            {
                ClearReadConfiguration();
                statusMessage = "动画机已变更，请点击“读取”。";
                statusType = MessageType.Info;
            }

            if (controllerAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(controllerAsset);
                EditorGUILayout.LabelField("资源路径", assetPath, EditorStyles.miniLabel);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(controllerAsset == null))
                {
                    if (GUILayout.Button("读取", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    {
                        ReadConfiguration();
                    }
                }

                using (new EditorGUI.DisabledScope(controllerAsset == null || !hasReadConfiguration))
                {
                    if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    {
                        SaveConfiguration();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// 绘制读取到的基础动画槽位。
        /// </summary>
        private void DrawAnimationFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("基础动画", EditorStyles.boldLabel);

            idleClip = DrawClipField("Idle", idleClip, "Idle BlendTree 第 2 个动画");

            walkClip = DrawClipField("Walk", walkClip, "Idle BlendTree 第 1、3 个动画");
            runClip = DrawClipField("Run", runClip, "Idle BlendTree 第 4 个动画");
            hitClip = DrawClipField("Hit", hitClip, "HitState 状态机中的 Hit");
            breakClip = DrawClipField("Break", breakClip, "BreakState 状态机中的首个动画");
            deadClip = DrawClipField("Dead", deadClip, "Dead 状态");
            recoveryClip = DrawClipField("Recovery", recoveryClip, "Recovery 状态");

            DrawExtraClipFields();
        }

        private static AnimationClip DrawClipField(string label, AnimationClip clip, string sourceDescription)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                AnimationClip newClip = EditorGUILayout.ObjectField(
                    label,
                    clip,
                    typeof(AnimationClip),
                    false) as AnimationClip;

                GUILayout.Label(sourceDescription, EditorStyles.miniLabel, GUILayout.Width(230f));
                return newClip;
            }
        }

        /// <summary>
        /// 绘制非基础 Clip 数量和动画机位置。
        /// </summary>
        private void DrawExtraClipFields()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                string.Format("非基础动画 Clip（唯一 {0} 个）", extraClipInfos.Count),
                EditorStyles.boldLabel);

            if (extraClipInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("没有读取到除基础动画之外的 Clip。", MessageType.Info);
                return;
            }

            for (int i = 0; i < extraClipInfos.Count; i++)
            {
                ExtraClipInfo clipInfo = extraClipInfos[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField(
                        "Clip",
                        clipInfo.Clip,
                        typeof(AnimationClip),
                        false);

                    for (int locationIndex = 0; locationIndex < clipInfo.Locations.Count; locationIndex++)
                    {
                        EditorGUILayout.LabelField(
                            locationIndex == 0 ? "动画机位置" : string.Empty,
                            clipInfo.Locations[locationIndex],
                            EditorStyles.miniLabel);
                    }
                }
            }
        }

        /// <summary>
        /// 从动画机的基础层读取约定位置上的基础动画。
        /// </summary>
        private void ReadConfiguration()
        {
            ClearReadConfiguration();

            AnimatorController controller = controllerAsset;
            if (controller == null)
            {
                statusMessage = "当前资源不是可编辑的 AnimatorController，无法读取动画配置。";
                statusType = MessageType.Error;
                return;
            }

            if (controller.layers == null || controller.layers.Length == 0 ||
                controller.layers[0].stateMachine == null)
            {
                statusMessage = "AnimatorController 没有可读取的基础层状态机。";
                statusType = MessageType.Error;
                return;
            }

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            List<string> missingItems = new List<string>();

            AnimatorState idleState = FindStateRecursive(rootStateMachine, "Idle");
            idleBlendTree = FindIdleBlendTree(idleState == null ? null : idleState.motion);
            if (idleBlendTree == null)
            {
                missingItems.Add("Idle BlendTree");
            }
            else
            {
                ChildMotion[] children = idleBlendTree.children;
                if (children == null || children.Length < 4)
                {
                    missingItems.Add("Idle BlendTree 的四个动画");
                }
                else
                {
                    walkClip = children[0].motion as AnimationClip;
                    idleClip = children[1].motion as AnimationClip;
                    runClip = children[3].motion as AnimationClip;

                    if (children[0].motion != children[2].motion)
                    {
                        missingItems.Add("Walk 的第 1、3 个动画一致性");
                    }
                }
            }

            AnimatorStateMachine hitStateMachine = FindStateMachineRecursive(rootStateMachine, "HitState");
            hitState = FindStateInStateMachine(hitStateMachine, "Hit");
            if (hitState == null)
            {
                hitState = FindFirstStateWithClip(hitStateMachine);
            }

            if (hitState == null)
            {
                missingItems.Add("HitState/Hit");
            }
            else
            {
                hitClip = hitState.motion as AnimationClip;
                if (hitClip == null)
                {
                    missingItems.Add("Hit 动画");
                }
            }

            AnimatorStateMachine breakStateMachine = FindStateMachineRecursive(rootStateMachine, "BreakState");
            breakState = FindFirstStateWithClip(breakStateMachine);
            if (breakState == null)
            {
                missingItems.Add("BreakState 的首个动画");
            }
            else
            {
                breakClip = breakState.motion as AnimationClip;
                if (breakClip == null)
                {
                    missingItems.Add("Break 动画");
                }
            }

            deadState = FindStateRecursive(rootStateMachine, "Dead");
            if (deadState == null)
            {
                missingItems.Add("Dead");
            }
            else
            {
                deadClip = deadState.motion as AnimationClip;
                if (deadClip == null)
                {
                    missingItems.Add("Dead 动画");
                }
            }

            recoveryState = FindStateRecursive(rootStateMachine, "Recovery");
            if (recoveryState == null)
            {
                missingItems.Add("Recovery");
            }
            else
            {
                recoveryClip = recoveryState.motion as AnimationClip;
                if (recoveryClip == null)
                {
                    missingItems.Add("Recovery 动画");
                }
            }

            hasReadConfiguration = true;
            if (missingItems.Count == 0)
            {
                statusMessage = "读取完成，可以修改动画后点击“保存”。";
                statusType = MessageType.Info;
            }
            else
            {
                statusMessage = "读取完成，但未找到：" + string.Join("、", missingItems);
                statusType = MessageType.Warning;
            }

            CollectExtraClipInfos(controller);
        }

        /// <summary>
        /// 将面板中的动画写回 AnimatorController，并保存资源。
        /// </summary>
        private void SaveConfiguration()
        {
            AnimatorController controller = controllerAsset;
            if (controller == null)
            {
                statusMessage = "当前资源不是可编辑的 AnimatorController，无法保存。";
                statusType = MessageType.Error;
                return;
            }

            RecordUndoTargets(controller);

            List<string> missingItems = new List<string>();
            if (idleBlendTree == null)
            {
                missingItems.Add("Idle BlendTree");
            }
            else
            {
                ChildMotion[] children = idleBlendTree.children;
                if (children == null || children.Length < 4)
                {
                    missingItems.Add("Idle BlendTree 的四个动画");
                }
                else
                {
                    children[0].motion = walkClip;
                    children[1].motion = idleClip;
                    children[2].motion = walkClip;
                    children[3].motion = runClip;
                    idleBlendTree.children = children;
                }
            }

            if (hitState == null)
            {
                missingItems.Add("HitState/Hit");
            }
            else
            {
                hitState.motion = hitClip;
            }

            if (breakState == null)
            {
                missingItems.Add("BreakState 的首个动画");
            }
            else
            {
                breakState.motion = breakClip;
            }

            if (deadState == null)
            {
                missingItems.Add("Dead");
            }
            else
            {
                deadState.motion = deadClip;
            }

            if (recoveryState == null)
            {
                missingItems.Add("Recovery");
            }
            else
            {
                recoveryState.motion = recoveryClip;
            }

            if (missingItems.Count > 0)
            {
                statusMessage = "保存已跳过未找到的配置：" + string.Join("、", missingItems);
                statusType = MessageType.Warning;
            }

            SetDirtyTargets();
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (missingItems.Count == 0)
            {
                statusMessage = "动画配置已保存。";
                statusType = MessageType.Info;
            }

            CollectExtraClipInfos(controller);
        }

        /// <summary>
        /// 在写回控制器前记录可撤销对象。
        /// </summary>
        private void RecordUndoTargets(AnimatorController controller)
        {
            Undo.RecordObject(controller, "保存基础动画配置");
            RecordUndoTarget(idleBlendTree, "Idle BlendTree");
            RecordUndoTarget(hitState, "Hit 状态");
            RecordUndoTarget(breakState, "Break 状态");
            RecordUndoTarget(deadState, "Dead 状态");
            RecordUndoTarget(recoveryState, "Recovery 状态");
        }

        private void SetDirtyTargets()
        {
            SetDirtyTarget(idleBlendTree);
            SetDirtyTarget(hitState);
            SetDirtyTarget(breakState);
            SetDirtyTarget(deadState);
            SetDirtyTarget(recoveryState);
        }

        private static void SetDirtyTarget(UnityEngine.Object target)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private static void RecordUndoTarget(UnityEngine.Object target, string targetName)
        {
            if (target != null)
            {
                Undo.RecordObject(target, "修改" + targetName);
            }
        }

        private void ClearReadConfiguration()
        {
            hasReadConfiguration = false;
            idleBlendTree = null;
            hitState = null;
            breakState = null;
            deadState = null;
            recoveryState = null;
            extraClipInfos.Clear();

            idleClip = null;
            walkClip = null;
            runClip = null;
            hitClip = null;
            breakClip = null;
            deadClip = null;
            recoveryClip = null;
        }

        /// <summary>
        /// 遍历动画机并收集未被基础动画配置使用的 Clip 及其位置。
        /// </summary>
        private void CollectExtraClipInfos(AnimatorController controller)
        {
            extraClipInfos.Clear();

            HashSet<AnimationClip> basicClips = new HashSet<AnimationClip>();
            AddBasicClip(basicClips, idleClip);
            AddBasicClip(basicClips, walkClip);
            AddBasicClip(basicClips, runClip);
            AddBasicClip(basicClips, hitClip);
            AddBasicClip(basicClips, breakClip);
            AddBasicClip(basicClips, deadClip);
            AddBasicClip(basicClips, recoveryClip);

            Dictionary<AnimationClip, ExtraClipInfo> infoByClip =
                new Dictionary<AnimationClip, ExtraClipInfo>();

            if (controller.layers == null)
            {
                return;
            }

            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                AnimatorControllerLayer layer = controller.layers[layerIndex];
                if (layer.stateMachine == null)
                {
                    continue;
                }

                string layerPath = string.IsNullOrEmpty(layer.name)
                    ? string.Format("Layer[{0}]", layerIndex + 1)
                    : layer.name;
                CollectStateMachineClips(
                    layer.stateMachine,
                    layerPath,
                    basicClips,
                    infoByClip,
                    new HashSet<Motion>());
            }
        }

        /// <summary>
        /// 将已读取的基础动画加入排除集合。
        /// </summary>
        private static void AddBasicClip(HashSet<AnimationClip> basicClips, AnimationClip clip)
        {
            if (clip != null)
            {
                basicClips.Add(clip);
            }
        }

        /// <summary>
        /// 递归遍历状态机中的状态和子状态机。
        /// </summary>
        private void CollectStateMachineClips(
            AnimatorStateMachine stateMachine,
            string stateMachinePath,
            HashSet<AnimationClip> basicClips,
            Dictionary<AnimationClip, ExtraClipInfo> infoByClip,
            HashSet<Motion> activeMotions)
        {
            ChildAnimatorState[] states = stateMachine.states;
            if (states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    AnimatorState state = states[i].state;
                    if (state == null)
                    {
                        continue;
                    }

                    string stateName = string.IsNullOrEmpty(state.name)
                        ? string.Format("State[{0}]", i + 1)
                        : state.name;
                    CollectMotionClips(
                        state.motion,
                        stateMachinePath + "/" + stateName,
                        basicClips,
                        infoByClip,
                        activeMotions);
                }
            }

            ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
            if (childStateMachines == null)
            {
                return;
            }

            for (int i = 0; i < childStateMachines.Length; i++)
            {
                AnimatorStateMachine childStateMachine = childStateMachines[i].stateMachine;
                if (childStateMachine == null)
                {
                    continue;
                }

                string childName = string.IsNullOrEmpty(childStateMachine.name)
                    ? string.Format("StateMachine[{0}]", i + 1)
                    : childStateMachine.name;
                CollectStateMachineClips(
                    childStateMachine,
                    stateMachinePath + "/" + childName,
                    basicClips,
                    infoByClip,
                    activeMotions);
            }
        }

        /// <summary>
        /// 递归展开 Motion 和 BlendTree，记录 Clip 的动画机路径。
        /// </summary>
        private void CollectMotionClips(
            Motion motion,
            string motionPath,
            HashSet<AnimationClip> basicClips,
            Dictionary<AnimationClip, ExtraClipInfo> infoByClip,
            HashSet<Motion> activeMotions)
        {
            if (motion == null)
            {
                return;
            }

            AnimationClip clip = motion as AnimationClip;
            if (clip != null)
            {
                if (basicClips.Contains(clip))
                {
                    return;
                }

                if (!infoByClip.TryGetValue(clip, out ExtraClipInfo clipInfo))
                {
                    clipInfo = new ExtraClipInfo
                    {
                        Clip = clip
                    };
                    infoByClip.Add(clip, clipInfo);
                    extraClipInfos.Add(clipInfo);
                }

                if (!clipInfo.Locations.Contains(motionPath))
                {
                    clipInfo.Locations.Add(motionPath);
                }

                return;
            }

            BlendTree blendTree = motion as BlendTree;
            if (blendTree == null || !activeMotions.Add(blendTree))
            {
                return;
            }

            ChildMotion[] children = blendTree.children;
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    CollectMotionClips(
                        children[i].motion,
                        string.Format(
                            "{0}/{1}[{2}]",
                            motionPath,
                            string.IsNullOrEmpty(blendTree.name) ? "BlendTree" : blendTree.name,
                            i + 1),
                        basicClips,
                        infoByClip,
                        activeMotions);
                }
            }

            activeMotions.Remove(blendTree);
        }

        private static AnimatorStateMachine FindStateMachineRecursive(
            AnimatorStateMachine stateMachine,
            string stateMachineName)
        {
            if (stateMachine == null)
            {
                return null;
            }

            if (string.Equals(stateMachine.name, stateMachineName, StringComparison.OrdinalIgnoreCase))
            {
                return stateMachine;
            }

            ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
            if (childStateMachines == null)
            {
                return null;
            }

            for (int i = 0; i < childStateMachines.Length; i++)
            {
                AnimatorStateMachine result = FindStateMachineRecursive(
                    childStateMachines[i].stateMachine,
                    stateMachineName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static AnimatorState FindStateRecursive(AnimatorStateMachine stateMachine, string stateName)
        {
            if (stateMachine == null)
            {
                return null;
            }

            AnimatorState state = FindStateInStateMachine(stateMachine, stateName);
            if (state != null)
            {
                return state;
            }

            ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
            if (childStateMachines == null)
            {
                return null;
            }

            for (int i = 0; i < childStateMachines.Length; i++)
            {
                state = FindStateRecursive(childStateMachines[i].stateMachine, stateName);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        private static AnimatorState FindStateInStateMachine(
            AnimatorStateMachine stateMachine,
            string stateName)
        {
            if (stateMachine == null || stateMachine.states == null)
            {
                return null;
            }

            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    string.Equals(states[i].state.name, stateName, StringComparison.OrdinalIgnoreCase))
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static AnimatorState FindFirstStateWithClip(AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null || stateMachine.states == null)
            {
                return null;
            }

            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.motion is AnimationClip)
                {
                    return state;
                }
            }

            ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
            if (childStateMachines == null)
            {
                return null;
            }

            for (int i = 0; i < childStateMachines.Length; i++)
            {
                AnimatorState state = FindFirstStateWithClip(childStateMachines[i].stateMachine);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        /// <summary>
        /// 从 Idle 状态的 Motion 中定位包含四个基础动画的 BlendTree。
        /// </summary>
        private static BlendTree FindIdleBlendTree(Motion motion)
        {
            BlendTree rootBlendTree = motion as BlendTree;
            if (rootBlendTree == null)
            {
                return null;
            }

            ChildMotion[] rootChildren = rootBlendTree.children;
            if (rootChildren != null && rootChildren.Length >= 4)
            {
                return rootBlendTree;
            }

            BlendTree fourChildBlendTree = FindBlendTreeWithAtLeastFourChildren(rootBlendTree);
            if (fourChildBlendTree != null)
            {
                return fourChildBlendTree;
            }

            BlendTree namedIdleBlendTree = FindBlendTreeByName(rootBlendTree, "Idle");
            if (namedIdleBlendTree != null)
            {
                return namedIdleBlendTree;
            }

            return rootBlendTree;
        }

        private static BlendTree FindBlendTreeByName(BlendTree blendTree, string blendTreeName)
        {
            if (blendTree == null)
            {
                return null;
            }

            if (string.Equals(blendTree.name, blendTreeName, StringComparison.OrdinalIgnoreCase))
            {
                return blendTree;
            }

            ChildMotion[] children = blendTree.children;
            if (children == null)
            {
                return null;
            }

            for (int i = 0; i < children.Length; i++)
            {
                BlendTree childBlendTree = children[i].motion as BlendTree;
                BlendTree result = FindBlendTreeByName(childBlendTree, blendTreeName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static BlendTree FindBlendTreeWithAtLeastFourChildren(BlendTree blendTree)
        {
            if (blendTree == null)
            {
                return null;
            }

            ChildMotion[] children = blendTree.children;
            if (children != null && children.Length >= 4)
            {
                return blendTree;
            }

            if (children == null)
            {
                return null;
            }

            for (int i = 0; i < children.Length; i++)
            {
                BlendTree childBlendTree = children[i].motion as BlendTree;
                BlendTree result = FindBlendTreeWithAtLeastFourChildren(childBlendTree);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
#endif
