using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    /// <summary>
    /// 动画混合工具:把两个动画按过渡值线性插值,只混合勾选身体部位的 Humanoid 肌肉曲线,
    /// 其余曲线(非肌肉变换、BlendShape、对象引用等)沿用 A。右侧是实时预览,调参数即刻看到结果。
    /// </summary>
    public partial class XCAnimBlendWindow : EditorWindow
    {
        /// <summary>权重贴近 0 时按"原样复制 A"处理,避免采样把关键帧切线烤平</summary>
        private const float WeightEpsilon = 0.0001f;

        /// <summary>结果时长下限</summary>
        private const float MinLength = 0.01f;

        /// <summary>左侧参数栏宽度</summary>
        private const float LeftColumnWidth = 390f;

        /// <summary>
        /// 可参与混合的身体部位。前 13 位的顺序与 UnityEngine.AvatarMaskBodyPart 一一对应,便于和 AvatarMask 互转;
        /// 之后的位是本工具自己的细分(AvatarMask 没有对应项)。
        /// </summary>
        [System.Flags]
        public enum EBodyPart
        {
            None = 0,
            Root = 1 << 0,
            Body = 1 << 1,
            Head = 1 << 2,
            LeftLeg = 1 << 3,
            RightLeg = 1 << 4,
            LeftArm = 1 << 5,
            RightArm = 1 << 6,
            LeftFingers = 1 << 7,
            RightFingers = 1 << 8,
            LeftFoot = 1 << 9,
            RightFoot = 1 << 10,
            LeftHand = 1 << 11,
            RightHand = 1 << 12,

            /// <summary>
            /// 脖子。AvatarMask 没有单独的脖子位(AvatarMaskBodyPart.LastBodyPart = 13,0~12 已被占满),
            /// 所以这是本工具扩展出来的第 14 位;AvatarMask 里脖子原本归在 Head 下。
            /// </summary>
            Neck = 1 << 13,
        }

        /// <summary>
        /// 部位与 humanoid 肌肉名前缀的对应关系(肌肉名形如 "Chest Front-Back")
        /// 前缀从头匹配,所以 "Chest " 不会命中 "UpperChest ","Left Arm " 不会命中 "Left ForeArm "
        /// </summary>
        private static readonly (EBodyPart Part, string Prefix)[] PartPrefixes =
        {
            (EBodyPart.Root, "RootT"),
            (EBodyPart.Root, "RootQ"),
            (EBodyPart.Body, "Spine "),
            (EBodyPart.Body, "Chest "),
            (EBodyPart.Body, "UpperChest "),
            (EBodyPart.Body, "Left Shoulder "),
            (EBodyPart.Body, "Right Shoulder "),
            (EBodyPart.Neck, "Neck "),
            (EBodyPart.Head, "Head "),
            (EBodyPart.Head, "Left Eye "),
            (EBodyPart.Head, "Right Eye "),
            (EBodyPart.Head, "Jaw "),
            (EBodyPart.LeftArm, "Left Arm "),
            (EBodyPart.LeftArm, "Left ForeArm "),
            (EBodyPart.RightArm, "Right Arm "),
            (EBodyPart.RightArm, "Right ForeArm "),
            (EBodyPart.LeftHand, "Left Hand "),
            (EBodyPart.RightHand, "Right Hand "),
            (EBodyPart.LeftFingers, "Left Thumb "),
            (EBodyPart.LeftFingers, "Left Index "),
            (EBodyPart.LeftFingers, "Left Middle "),
            (EBodyPart.LeftFingers, "Left Ring "),
            (EBodyPart.LeftFingers, "Left Little "),
            (EBodyPart.RightFingers, "Right Thumb "),
            (EBodyPart.RightFingers, "Right Index "),
            (EBodyPart.RightFingers, "Right Middle "),
            (EBodyPart.RightFingers, "Right Ring "),
            (EBodyPart.RightFingers, "Right Little "),
            (EBodyPart.LeftLeg, "Left Upper Leg"),
            (EBodyPart.LeftLeg, "Left Lower Leg"),
            (EBodyPart.RightLeg, "Right Upper Leg"),
            (EBodyPart.RightLeg, "Right Lower Leg"),
            (EBodyPart.LeftFoot, "Left Foot"),
            (EBodyPart.LeftFoot, "Left Toe"),
            (EBodyPart.RightFoot, "Right Foot"),
            (EBodyPart.RightFoot, "Right Toe"),
        };

        [Tooltip("基础动画 A:结果时长、事件、对象曲线与未混合曲线都以它为准")]
        public AnimationClip clipA;

        [Tooltip("目标动画 B:只贡献勾选部位的姿势,权重为 1 时勾选部位完全变成 B")]
        public AnimationClip clipB;

        [Range(0f, 1f)]
        [Tooltip("过渡值:0 = 全 A,1 = 全 B,中间按线性插值")]
        public float weight = 0.5f;

        [Tooltip("参与混合的身体部位(分类与 AvatarMask 一致)")]
        public EBodyPart parts = EBodyPart.Body | EBodyPart.Head | EBodyPart.Neck |
                                 EBodyPart.LeftArm | EBodyPart.RightArm |
                                 EBodyPart.LeftHand | EBodyPart.RightHand |
                                 EBodyPart.LeftLeg | EBodyPart.RightLeg |
                                 EBodyPart.LeftFoot | EBodyPart.RightFoot;

        [Tooltip("可选:指定 AvatarMask 后用下面的按钮一键套用它的部位勾选")]
        public AvatarMask mask;

        [Tooltip("勾选:除上面勾选的部位外,其余 Humanoid 肌肉(手指/根等)也一起混合")]
        public bool blendOtherMuscles;

        [Tooltip("结果时长(秒);填 0 表示与 A 等长")]
        public float targetLength;

        [Tooltip("采样帧率,0 表示跟随 A 的帧率")]
        public float sampleRate;

        [Tooltip("结果动画是否循环播放")]
        public bool loopTime = true;

        [Tooltip("留空则用\"A名_Blend_B名\"并自动追加 _1、_2 ...;填写则用该名字,已存在时覆盖")]
        public string outputName;

        /// <summary>最近一次生成出来的动画</summary>
        [System.NonSerialized] public AnimationClip resultClip;

        /// <summary>最近一次生成的处理报告</summary>
        private string _report;

        private Vector2 _settingsScroll;

        [MenuItem(XCEditorTools.XCAnimBlendWindow)]
        public static XCAnimBlendWindow Open()
        {
            XCAnimBlendWindow window = GetWindow<XCAnimBlendWindow>("动画混合");
            window.minSize = new Vector2(820f, 480f);
            window.Show();
            return window;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(LeftColumnWidth));
            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);
            DrawSettings();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            DrawPreviewPane();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            //播放中保持刷新
            if (_previewPlaying)
            {
                Repaint();
            }
        }

        /// <summary>左侧参数栏</summary>
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("动画混合", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "结果(t) = A(t) × (1 − 过渡值) + B(t) × 过渡值\n" +
                "只混合勾选部位的 Humanoid 肌肉曲线;B 里没有对应曲线的肌肉按静止姿势(0)参与插值。\n" +
                "其余曲线与动画事件一律沿用 A,结果时长默认与 A 等长。",
                MessageType.Info);

            EditorGUILayout.Space(4f);
            clipA = (AnimationClip)EditorGUILayout.ObjectField("动画 A(基准)", clipA, typeof(AnimationClip), false);
            clipB = (AnimationClip)EditorGUILayout.ObjectField("动画 B(目标)", clipB, typeof(AnimationClip), false);

            weight = EditorGUILayout.Slider("过渡值", weight, 0f, 1f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("交换 A / B(权重取反)", GUILayout.Width(180f)))
                {
                    SwapClips();
                }
            }

            EditorGUILayout.Space(4f);
            parts = (EBodyPart)EditorGUILayout.EnumFlagsField("混合部位", parts);
            mask = (AvatarMask)EditorGUILayout.ObjectField("AvatarMask", mask, typeof(AvatarMask), false);
            using (new EditorGUI.DisabledScope(mask == null))
            {
                if (GUILayout.Button("套用 AvatarMask 的部位"))
                {
                    ApplyAvatarMask();
                }
            }

            blendOtherMuscles = EditorGUILayout.Toggle("其余肌肉也混合", blendOtherMuscles);

            EditorGUILayout.Space(4f);
            targetLength = EditorGUILayout.FloatField("结果时长(0=A)", targetLength);
            sampleRate = EditorGUILayout.FloatField("采样帧率(0=跟 A)", sampleRate);
            loopTime = EditorGUILayout.Toggle("循环", loopTime);
            outputName = EditorGUILayout.TextField("输出名", outputName);

            EditorGUILayout.Space(6f);
            if (clipA != null && clipB != null)
            {
                EditorGUILayout.LabelField(
                    $"结果 {GetResultLength():F3}s @ {GetSampleRate():0.#}fps" +
                    $"(A {clipA.length:F3}s / B {clipB.length:F3}s)",
                    EditorStyles.miniLabel);
            }

            using (new EditorGUI.DisabledScope(clipA == null || clipB == null))
            {
                if (GUILayout.Button("混合并保存", GUILayout.Height(30f)))
                {
                    BakeAndSave();
                }
            }

            if (!string.IsNullOrEmpty(_report))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_report, MessageType.None);
            }

            if (resultClip != null)
            {
                EditorGUILayout.BeginHorizontal();
                resultClip = (AnimationClip)EditorGUILayout.ObjectField("生成结果", resultClip, typeof(AnimationClip), false);
                if (GUILayout.Button("定位", GUILayout.Width(46f)))
                {
                    Selection.activeObject = resultClip;
                    EditorGUIUtility.PingObject(resultClip);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>交换两个动画并把过渡值取反</summary>
        private void SwapClips()
        {
            (clipA, clipB) = (clipB, clipA);
            weight = 1f - weight;
        }

        /// <summary>把 AvatarMask 上勾选的部位换成这里的部位位标记</summary>
        private void ApplyAvatarMask()
        {
            EBodyPart result = EBodyPart.None;
            //AvatarMaskBodyPart 的枚举顺序与 EBodyPart 的 bit 顺序一致,可以直接按位平移
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                if (mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i))
                {
                    result |= (EBodyPart)(1 << i);
                }
            }

            //AvatarMask 没有单独的脖子位,它的 Head 实际覆盖头 + 颈,所以套用时把两者一起勾上
            if ((result & EBodyPart.Head) != 0)
            {
                result |= EBodyPart.Neck;
            }

            parts = result;
            if (result == EBodyPart.None)
            {
                Debug.LogWarning("这个 AvatarMask 没有勾选任何 Humanoid 部位");
            }
        }

        /// <summary>按当前配置混合两个动画,另存为新的动画资源</summary>
        private void BakeAndSave()
        {
            if (clipA == null || clipB == null)
            {
                Debug.LogError("请先选择动画 A 与 B");
                return;
            }

            if (clipA == clipB)
            {
                Debug.LogError("A 和 B 是同一个动画,不需要混合");
                return;
            }

            if (parts == EBodyPart.None && !blendOtherMuscles)
            {
                Debug.LogError("至少要勾选一个混合部位,或勾上\"其余肌肉也混合\"");
                return;
            }

            float resultLength = GetResultLength();
            if (resultLength < MinLength)
            {
                Debug.LogError($"结果时长太短({resultLength:F3}s),请填写目标时长或检查 A 的时长");
                return;
            }

            string path = GetOutputPath(out bool overwrite);
            AnimationClip pathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (pathClip == clipA || pathClip == clipB)
            {
                Debug.LogError("输出名不能和 A / B 同名,否则会覆盖掉源动画");
                return;
            }

            AnimationClip target = overwrite ? pathClip : new AnimationClip();
            target.hideFlags &= ~HideFlags.NotEditable;

            _report = FillClip(target, resultLength);
            if (string.IsNullOrEmpty(_report))
            {
                //生成失败,把只读标记还原回去
                target.hideFlags |= HideFlags.NotEditable;
                return;
            }

            target.name = Path.GetFileNameWithoutExtension(path);
            if (overwrite)
            {
                //覆盖时内容直接写回原资源,保住 GUID,已引用它的技能/状态机不用重新绑
                EditorUtility.SetDirty(target);
            }
            else
            {
                AssetDatabase.CreateAsset(target, path);
            }

            //生成物标为只读,避免后续曲线编辑流程误改
            target.hideFlags |= HideFlags.NotEditable;
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            resultClip = target;
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            Debug.Log($"---- {_report}");
        }

        /// <summary>
        /// 生成混合后的动画内容,失败时返回 null。
        /// logError 为 false 时(预览用)只返回失败,不往控制台刷错误。
        /// </summary>
        private string FillClip(AnimationClip target, float resultLength, bool logError = true)
        {
            float rate = GetSampleRate();
            int stepCount = Mathf.Max(2, Mathf.CeilToInt(resultLength * rate));
            bool sameLength = Mathf.Abs(resultLength - clipA.length) <= MinLength;

            //B 里可参与混合的肌肉曲线,按肌肉名索引
            Dictionary<string, AnimationCurve> muscleB = new Dictionary<string, AnimationCurve>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clipB))
            {
                if (!IsBlendable(binding))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clipB, binding);
                if (curve != null && curve.keys.Length > 0)
                {
                    muscleB[binding.propertyName] = curve;
                }
            }

            if (muscleB.Count == 0)
            {
                if (logError)
                {
                    Debug.LogError("B 里没有找到勾选部位的肌肉曲线,请检查部位勾选(注意动画需要是 Humanoid)");
                }

                return null;
            }

            ClearClip(target);
            target.frameRate = rate;
            target.wrapMode = clipA.wrapMode;
            target.legacy = clipA.legacy;

            int blendedCount = 0;
            int copiedCount = 0;

            //A 的曲线:可混合的与 B 插值,其余照抄(结果时长不同则重新采样)
            HashSet<string> blendedMuscles = new HashSet<string>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clipA))
            {
                AnimationCurve source = AnimationUtility.GetEditorCurve(clipA, binding);
                if (source == null || source.keys.Length == 0)
                {
                    continue;
                }

                if (IsBlendable(binding))
                {
                    //B 没有这条肌肉时按静止值 0 参与插值,权重为 0 且等长时原样输出
                    muscleB.TryGetValue(binding.propertyName, out AnimationCurve curveB);
                    AnimationCurve blended = weight <= WeightEpsilon && sameLength
                        ? source
                        : BuildBlendCurve(source, clipA.length, curveB, clipB.length, weight, resultLength, stepCount);
                    AnimationUtility.SetEditorCurve(target, binding, blended);
                    blendedMuscles.Add(binding.propertyName);
                    blendedCount++;
                    continue;
                }

                AnimationUtility.SetEditorCurve(target, binding,
                    sameLength ? source : BuildBlendCurve(source, clipA.length, null, 0f, 0f, resultLength, stepCount));
                copiedCount++;
            }

            //B 独有的肌肉:权重 0 时是静止姿势,所以直接乘权重
            List<string> bOnlyNames = new List<string>();
            foreach (KeyValuePair<string, AnimationCurve> pair in muscleB)
            {
                if (blendedMuscles.Contains(pair.Key) || weight <= WeightEpsilon)
                {
                    continue;
                }

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), pair.Key);
                AnimationUtility.SetEditorCurve(target, binding,
                    BuildBlendCurve(null, 0f, pair.Value, clipB.length, weight, resultLength, stepCount));
                bOnlyNames.Add(pair.Key);
            }

            //事件与对象引用曲线照抄 A
            AnimationUtility.SetAnimationEvents(target, AnimationUtility.GetAnimationEvents(clipA));
            CopyObjectCurves(clipA, target);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clipA);
            settings.startTime = 0f;
            settings.stopTime = resultLength;
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(target, settings);

            target.EnsureQuaternionContinuity();

            return BuildReport(resultLength, rate, blendedCount, copiedCount, bOnlyNames, clipA.length, clipB.length);
        }

        /// <summary>
        /// 按帧采样生成一条插值曲线。
        /// 某一侧没有这条曲线时按静止值 0 处理(肌肉的静止姿势就是 0),权重为 0 时等价于原样复制。
        /// lengthA / lengthB 是两条源动画各自的时长,采样时先把结果时间按比例映射回各自动画的时间轴,
        /// 这样时长不同的动画是"整条等比例拉伸"对齐,而不是短的播完后停在最后一帧。
        /// </summary>
        private static AnimationCurve BuildBlendCurve(AnimationCurve curveA, float lengthA, AnimationCurve curveB,
            float lengthB, float blendWeight, float resultLength, int stepCount)
        {
            Keyframe[] keys = new Keyframe[stepCount + 1];
            for (int i = 0; i <= stepCount; i++)
            {
                float time = resultLength * i / stepCount;
                float valueA = curveA != null ? curveA.Evaluate(ScaleTime(time, lengthA, resultLength)) : 0f;
                float valueB = curveB != null ? curveB.Evaluate(ScaleTime(time, lengthB, resultLength)) : 0f;
                keys[i] = new Keyframe(time, Mathf.LerpUnclamped(valueA, valueB, blendWeight));
            }

            return MakeLinear(new AnimationCurve(keys));
        }

        /// <summary>
        /// 把结果时间轴上的某个时刻,按"源动画时长 / 结果时长"的比例映射回源动画自己的时间轴。
        /// 例:A 2s、B 1s、结果 2s 时,B 在结果 1s 处应该取自己 0.5s 的姿势,而不是 1s(那样 B 后半段会被末值顶住)。
        /// </summary>
        private static float ScaleTime(float time, float sourceLength, float resultLength)
        {
            if (sourceLength <= MinLength || resultLength <= MinLength)
            {
                return time;
            }

            return time * sourceLength / resultLength;
        }

        /// <summary>采样出来的曲线统一用线性插值,避免自动切线在密集关键帧之间过冲</summary>
        private static AnimationCurve MakeLinear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }

            return curve;
        }

        /// <summary>
        /// 判断该绑定是否参与混合:必须是 Humanoid 肌肉曲线,且落在勾选部位上(或打开了"其余肌肉也混合")
        /// </summary>
        private bool IsBlendable(EditorCurveBinding binding)
        {
            //Humanoid 肌肉曲线的绑定类型是 Animator,普通 transform/BlendShape 曲线不是
            if (binding.type != typeof(Animator) || string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            if (blendOtherMuscles)
            {
                return true;
            }

            if (parts == EBodyPart.None)
            {
                return false;
            }

            for (int i = 0; i < PartPrefixes.Length; i++)
            {
                if ((parts & PartPrefixes[i].Part) != 0 && binding.propertyName.StartsWith(PartPrefixes[i].Prefix))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>结果时长:没填就跟 A 等长</summary>
        private float GetResultLength()
        {
            if (targetLength > 0f)
            {
                return targetLength;
            }

            return clipA != null ? clipA.length : 0f;
        }

        /// <summary>采样帧率:没填就跟随 A 的帧率</summary>
        private float GetSampleRate()
        {
            if (sampleRate > 0f)
            {
                return sampleRate;
            }

            return clipA != null && clipA.frameRate > 0f ? clipA.frameRate : 30f;
        }

        /// <summary>清空动画里的浮点曲线、对象引用曲线与事件</summary>
        private static void ClearClip(AnimationClip clip)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < curveBindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, curveBindings[i], null);
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int i = 0; i < objectBindings.Length; i++)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, objectBindings[i], null);
            }

            AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
        }

        /// <summary>照抄对象引用曲线(贴图切换、节点显隐等)</summary>
        private static void CopyObjectCurves(AnimationClip source, AnimationClip target)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int i = 0; i < bindings.Length; i++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, bindings[i]);
                if (keys != null && keys.Length > 0)
                {
                    AnimationUtility.SetObjectReferenceCurve(target, bindings[i], keys);
                }
            }
        }

        /// <summary>拼装处理报告,方便在窗口里直接确认混合结果</summary>
        private string BuildReport(float resultLength, float rate, int blendedCount, int copiedCount,
            List<string> bOnlyNames, float lengthA, float lengthB)
        {
            string report =
                $"插值 {blendedCount} 条肌肉曲线,A 其余 {copiedCount} 条照抄\n" +
                $"结果 {resultLength:F3}s @ {rate:0.#}fps,过渡值 {weight:F2},loopTime = {(loopTime ? 1 : 0)}";

            //时长不一致时 A / B 都会被等比例拉伸到结果时长,短的变慢、长的变快
            if (Mathf.Abs(lengthA - resultLength) > MinLength || Mathf.Abs(lengthB - resultLength) > MinLength)
            {
                report += $"\n已等比例对齐时长:A {lengthA:F3}s / B {lengthB:F3}s → 结果 {resultLength:F3}s";
            }

            if (bOnlyNames.Count > 0)
            {
                report += $"\nB 独有、A 没有的肌肉(按权重写入):{string.Join(", ", bOnlyNames)}";
            }

            return report;
        }

        /// <summary>
        /// 计算输出路径:固定在 A 所在目录。文件名留空时按"A名_Blend_B名"自动追加序号,
        /// 填写文件名时直接用该名字,并通过 overwrite 告知是否覆盖已有资源
        /// </summary>
        private string GetOutputPath(out bool overwrite)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(clipA)).Replace('\\', '/');

            if (!string.IsNullOrEmpty(outputName))
            {
                string namedPath = $"{folder}/{outputName}.anim";
                overwrite = AssetDatabase.LoadAssetAtPath<AnimationClip>(namedPath) != null;
                return namedPath;
            }

            overwrite = false;
            string baseName = $"{clipA.name}_Blend_{clipB.name}";
            string path = $"{folder}/{baseName}.anim";
            for (int index = 1; AssetDatabase.LoadMainAssetAtPath(path) != null; index++)
            {
                path = $"{folder}/{baseName}_{index}.anim";
            }

            return path;
        }
    }
}
