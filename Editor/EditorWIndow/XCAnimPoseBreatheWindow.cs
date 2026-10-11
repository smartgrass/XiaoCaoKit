using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    /// <summary>
    /// 姿势呼吸工具:把呼吸源动画(一般用 idle)里的胸腔/头/肩起伏,以"偏移叠加"的方式加到姿势动画上
    /// </summary>
    public class XCAnimPoseBreatheWindow : XiaoCaoWindow
    {
        /// <summary>可以参与呼吸叠加的身体部位</summary>
        [System.Flags]
        public enum EBreathePart
        {
            None = 0,
            Spine = 1 << 0,
            Chest = 1 << 1,
            UpperChest = 1 << 2,
            Neck = 1 << 3,
            Head = 1 << 4,
            Shoulder = 1 << 5,
        }

        /// <summary>呼吸填充方式:结果时长比一个呼吸周期长时,怎么把呼吸铺满整条动画</summary>
        public enum EFillMode
        {
            /// <summary>平铺重复,呼吸节奏保持源动画的节奏</summary>
            Tile,

            /// <summary>拉伸填满,只走一个呼吸周期,节奏会被改快或改慢</summary>
            Stretch,
        }

        /// <summary>呼吸区间的最短长度,再短就构不成一个呼吸循环</summary>
        private const float MinSegmentLength = 0.1f;

        /// <summary>循环首尾差值超过该值就提示接不上</summary>
        private const float LoopSeamWarnValue = 0.01f;

        /// <summary>
        /// 部位与 humanoid 肌肉名前缀的对应关系(肌肉名形如 "Chest Front-Back")
        /// 前缀从头匹配,所以 "Chest " 不会命中 "UpperChest "
        /// </summary>
        private static readonly (EBreathePart Part, string Prefix)[] PartPrefixes =
        {
            (EBreathePart.Spine, "Spine "),
            (EBreathePart.Chest, "Chest "),
            (EBreathePart.UpperChest, "UpperChest "),
            (EBreathePart.Neck, "Neck "),
            (EBreathePart.Head, "Head "),
            (EBreathePart.Shoulder, "Left Shoulder "),
            (EBreathePart.Shoulder, "Right Shoulder "),
        };

        [Tooltip("姿势动画:呼吸叠加到它的肌肉曲线上,原动画不会被修改")]
        public AnimationClip poseClip;

        [Tooltip("呼吸源动画:一般用 idle,只从它里面取一段循环的起伏")]
        public AnimationClip breatheClip;

        [Tooltip("呼吸区间起点(秒),起点与终点要落在同一个呼吸循环上")]
        public float segmentStart;

        [Tooltip("呼吸区间终点(秒);填 0 表示用呼吸源整段")]
        public float segmentEnd;

        [Tooltip("结果时长(秒);填 0 表示与姿势动画等长")]
        public float targetLength;

        [Tooltip("结果比一个呼吸周期长时:平铺重复 / 拉伸填满")]
        public EFillMode fillMode = EFillMode.Tile;

        [Tooltip("参与呼吸叠加的部位,默认胸腔+上胸+颈+头+左右肩")]
        public EBreathePart parts = EBreathePart.Chest | EBreathePart.UpperChest | EBreathePart.Neck |
                                   EBreathePart.Head | EBreathePart.Shoulder;

        [Range(0f, 2f)]
        [Tooltip("呼吸幅度倍率,1 与源动画一致,越小呼吸越轻")]
        public float strength = 1f;

        [Tooltip("勾选:以呼吸区间首帧为零点(推荐);不勾:以区间均值为零点")]
        public bool useSegmentStartAsBase = true;

        [Tooltip("采样帧率,0 表示跟随呼吸源动画的帧率")]
        public float sampleRate;

        [Tooltip("结果动画是否循环播放")]
        public bool loopTime = true;

        [Tooltip("留空则用\"姿势动画名_Breathe\"并自动追加 _1、_2 ...;填写则用该名字,已存在时覆盖")]
        public string outputName;

        /// <summary>最近一次生成出来的动画</summary>
        private AnimationClip _resultClip;

        /// <summary>最近一次生成的处理报告</summary>
        private string _report;

        [MenuItem(XCEditorTools.XCAnimPoseBreatheWindow)]
        public static XCAnimPoseBreatheWindow Open()
        {
            return OpenWindow<XCAnimPoseBreatheWindow>("给姿势加呼吸");
        }

        public override void DrawHead()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("给姿势加呼吸", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "把呼吸源动画里一段循环的起伏,以\"偏移叠加\"的方式加到姿势动画上:\n" +
                "    结果(t) = 姿势值 + (呼吸源(t) − 基准值) × 幅度\n" +
                "只有勾选部位的肌肉会被叠加,根位移、腿、脚部 IK、手部都保持姿势原样。\n" +
                "结果时长默认与姿势动画等长,一个呼吸周期不够铺满就按周期重复;姿势动画短于结果时长时保持末帧姿势。",
                MessageType.Info);

            if (poseClip != null && breatheClip != null && TryGetSegment(out _, out float cycleLength))
            {
                float resultLength = GetResultLength();
                EditorGUILayout.LabelField(
                    $"呼吸周期 {cycleLength:F3}s × {resultLength / cycleLength:F2},结果 {resultLength:F3}s @ {GetSampleRate():0.#}fps" +
                    $"(姿势 {poseClip.length:F3}s / 呼吸源 {breatheClip.length:F3}s)");
            }

            if (!string.IsNullOrEmpty(_report))
            {
                EditorGUILayout.HelpBox(_report, MessageType.None);
            }

            if (_resultClip != null)
            {
                EditorGUILayout.BeginHorizontal();
                _resultClip = (AnimationClip)EditorGUILayout.ObjectField("生成结果", _resultClip, typeof(AnimationClip), false);
                if (GUILayout.Button("定位", GUILayout.Width(46)))
                {
                    Selection.activeObject = _resultClip;
                    EditorGUIUtility.PingObject(_resultClip);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 直接把呼吸区间设成呼吸源动画的整段
        /// </summary>
        [Button("区间=呼吸源整段")]
        private void UseWholeBreatheClip()
        {
            if (breatheClip == null)
            {
                Debug.LogError("请先选择呼吸源动画");
                return;
            }

            segmentStart = 0f;
            segmentEnd = breatheClip.length;
        }

        /// <summary>
        /// 把呼吸周期吸附成结果时长的整数分之一,这样平铺之后循环衔接处天然无缝
        /// </summary>
        [Button("周期吸附:整除结果时长")]
        private void SnapCycleToResultLength()
        {
            if (poseClip == null || breatheClip == null)
            {
                Debug.LogError("请先选择姿势动画与呼吸源动画");
                return;
            }

            if (!TryGetSegment(out float start, out float cycleLength))
            {
                Debug.LogError("呼吸周期无效,请先调整起点/终点");
                return;
            }

            float resultLength = GetResultLength();
            int cycles = Mathf.Max(1, Mathf.RoundToInt(resultLength / cycleLength));
            float snapped = resultLength / cycles;
            if (start + snapped > breatheClip.length)
            {
                Debug.LogError($"吸附后的周期 {snapped:F3}s 超出呼吸源时长 {breatheClip.length:F3}s,请改起点或目标时长");
                return;
            }

            segmentEnd = start + snapped;
            Debug.Log($"---- 呼吸周期吸附为 {snapped:F3}s({cycles} 个循环铺满 {resultLength:F3}s)");
        }

        /// <summary>
        /// 按当前配置给姿势动画叠加呼吸,另存为新的动画资源
        /// </summary>
        [Button("加呼吸并保存")]
        private void BakeAndSave()
        {
            if (poseClip == null)
            {
                Debug.LogError("请先选择姿势动画");
                return;
            }

            if (breatheClip == null)
            {
                Debug.LogError("请先选择呼吸源动画");
                return;
            }

            if (parts == EBreathePart.None)
            {
                Debug.LogError("至少要勾选一个呼吸部位");
                return;
            }

            if (!TryGetSegment(out float start, out float cycleLength))
            {
                Debug.LogError($"呼吸周期无效,请调整起点/终点(终点填 0 表示用整段),周期至少 {MinSegmentLength}s");
                return;
            }

            float resultLength = GetResultLength();
            if (resultLength < MinSegmentLength)
            {
                Debug.LogError($"结果时长太短({resultLength:F3}s),请填写目标时长或检查姿势动画");
                return;
            }

            float rate = GetSampleRate();
            //按帧率逐帧采样,密集关键帧用线性插值,不会因为自动切线过冲
            int stepCount = Mathf.Max(2, Mathf.CeilToInt(resultLength * rate));

            string path = GetOutputPath(out bool overwrite);
            AnimationClip target = overwrite ? AssetDatabase.LoadAssetAtPath<AnimationClip>(path) : new AnimationClip();
            if (target == poseClip || target == breatheClip)
            {
                Debug.LogError("输出名不能和姿势动画/呼吸源动画同名,否则会覆盖掉源动画");
                return;
            }

            target.hideFlags &= ~HideFlags.NotEditable;

            _report = FillClip(target, start, cycleLength, resultLength, stepCount, rate);
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

            _resultClip = target;
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            Debug.Log($"---- {_report}");
        }

        /// <summary>
        /// 生成"姿势 + 呼吸偏移"的动画内容,失败时返回 null
        /// </summary>
        private string FillClip(AnimationClip target, float start, float cycleLength, float resultLength, int stepCount,
            float rate)
        {
            //呼吸源里的偏移曲线;binding 用于写回,肌肉名用于和姿势曲线配对
            Dictionary<EditorCurveBinding, AnimationCurve> offsetByBinding = new Dictionary<EditorCurveBinding, AnimationCurve>();
            Dictionary<string, AnimationCurve> offsetByMuscle = new Dictionary<string, AnimationCurve>();
            float seamMax = 0f;

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(breatheClip))
            {
                if (!IsBreathePart(binding.propertyName))
                {
                    continue;
                }

                AnimationCurve source = AnimationUtility.GetEditorCurve(breatheClip, binding);
                if (source == null || source.keys.Length == 0)
                {
                    continue;
                }

                AnimationCurve offset = BuildOffsetCurve(source, start, cycleLength, resultLength, stepCount);
                seamMax = Mathf.Max(seamMax, Mathf.Abs(offset.Evaluate(0f) - offset.Evaluate(resultLength)));
                offsetByBinding[binding] = offset;
                offsetByMuscle[binding.propertyName] = offset;
            }

            if (offsetByBinding.Count == 0)
            {
                Debug.LogError("呼吸源动画里没有找到勾选部位的肌肉曲线,请检查部位勾选(注意源动画需要是 Humanoid)");
                return null;
            }

            ClearClip(target);
            target.frameRate = rate;
            target.wrapMode = poseClip.wrapMode;
            target.legacy = poseClip.legacy;

            float poseLength = poseClip.length;
            HashSet<string> blendedMuscles = new HashSet<string>();
            List<string> poseOnlyNames = new List<string>();
            int blendedCount = 0;

            //姿势曲线:呼吸部位叠加偏移,其余原样保留
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(poseClip))
            {
                AnimationCurve source = AnimationUtility.GetEditorCurve(poseClip, binding);
                if (source == null || source.keys.Length == 0)
                {
                    continue;
                }

                if (offsetByMuscle.TryGetValue(binding.propertyName, out AnimationCurve offset))
                {
                    AnimationUtility.SetEditorCurve(target, binding, AddOffset(source, offset, poseLength));
                    blendedMuscles.Add(binding.propertyName);
                    blendedCount++;
                }
                else
                {
                    AnimationUtility.SetEditorCurve(target, binding, source);
                    if (IsBreathePart(binding.propertyName))
                    {
                        //姿势有、呼吸源没有的肌肉(比如源动画没驱动 UpperChest)保持不动
                        poseOnlyNames.Add(binding.propertyName);
                    }
                }
            }

            //姿势没有驱动的呼吸肌肉:直接把偏移写进去(姿势里这些肌肉为零点)
            List<string> addedMuscles = new List<string>();
            foreach (var pair in offsetByBinding)
            {
                if (blendedMuscles.Contains(pair.Key.propertyName))
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(target, pair.Key, pair.Value);
                addedMuscles.Add(pair.Key.propertyName);
            }

            //事件与对象引用曲线照抄姿势动画
            AnimationUtility.SetAnimationEvents(target, AnimationUtility.GetAnimationEvents(poseClip));
            CopyObjectCurves(poseClip, target);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(poseClip);
            settings.startTime = 0f;
            settings.stopTime = resultLength;
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(target, settings);

            target.EnsureQuaternionContinuity();

            return BuildReport(resultLength, cycleLength, rate, blendedCount, addedMuscles.Count, poseOnlyNames, seamMax);
        }

        /// <summary>
        /// 取呼吸源曲线的相对起伏:按帧采样到结果时长上,每个值减去基准值(周期首帧或周期均值)
        /// 平铺模式按呼吸周期循环取值,拉伸模式把一个周期映射到整条结果动画
        /// </summary>
        private AnimationCurve BuildOffsetCurve(AnimationCurve source, float start, float cycleLength,
            float resultLength, int stepCount)
        {
            float baseValue = useSegmentStartAsBase
                ? source.Evaluate(start)
                : GetAverageValue(source, start, cycleLength);

            Keyframe[] keys = new Keyframe[stepCount + 1];
            for (int i = 0; i <= stepCount; i++)
            {
                float time = resultLength * i / stepCount;
                float phase = fillMode == EFillMode.Stretch
                    ? start + cycleLength * time / resultLength
                    : start + Mathf.Repeat(time, cycleLength);
                keys[i] = new Keyframe(time, (source.Evaluate(phase) - baseValue) * strength);
            }

            return MakeLinear(new AnimationCurve(keys));
        }

        /// <summary>
        /// 求呼吸源曲线在一个呼吸周期内的平均值,作为"呼吸中值"基准
        /// </summary>
        private static float GetAverageValue(AnimationCurve source, float start, float cycleLength)
        {
            const int sampleCount = 32;
            float sum = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                sum += source.Evaluate(start + cycleLength * i / sampleCount);
            }

            return sum / sampleCount;
        }

        /// <summary>
        /// 把呼吸偏移加到姿势曲线上:结果(t) = 姿势(min(t, 姿势时长)) + 偏移(t)
        /// </summary>
        private static AnimationCurve AddOffset(AnimationCurve poseCurve, AnimationCurve offset, float poseLength)
        {
            Keyframe[] offsetKeys = offset.keys;
            Keyframe[] keys = new Keyframe[offsetKeys.Length];
            for (int i = 0; i < offsetKeys.Length; i++)
            {
                float time = offsetKeys[i].time;
                //姿势比结果时长短时保持末帧姿势,避免出姿势时长后姿势被外推
                float poseValue = poseCurve.Evaluate(Mathf.Min(time, poseLength));
                keys[i] = new Keyframe(time, poseValue + offsetKeys[i].value);
            }

            return MakeLinear(new AnimationCurve(keys));
        }

        /// <summary>
        /// 采样出来的曲线统一用线性插值,避免自动切线在密集关键帧之间过冲
        /// </summary>
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
        /// 判断肌肉名是否落在勾选的呼吸部位上
        /// </summary>
        private bool IsBreathePart(string muscleName)
        {
            if (string.IsNullOrEmpty(muscleName))
            {
                return false;
            }

            for (int i = 0; i < PartPrefixes.Length; i++)
            {
                if ((parts & PartPrefixes[i].Part) == 0)
                {
                    continue;
                }

                if (muscleName.StartsWith(PartPrefixes[i].Prefix))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 解析呼吸周期:起点与终点夹到呼吸源时长内,终点填 0 表示用整段
        /// </summary>
        private bool TryGetSegment(out float start, out float cycleLength)
        {
            start = 0f;
            cycleLength = 0f;

            if (breatheClip == null)
            {
                return false;
            }

            start = Mathf.Clamp(segmentStart, 0f, breatheClip.length);
            float end = segmentEnd <= 0f ? breatheClip.length : Mathf.Clamp(segmentEnd, 0f, breatheClip.length);
            cycleLength = end - start;
            return cycleLength >= MinSegmentLength;
        }

        /// <summary>
        /// 结果时长:没填就跟姿势动画等长
        /// </summary>
        private float GetResultLength()
        {
            if (targetLength > 0f)
            {
                return targetLength;
            }

            return poseClip != null ? poseClip.length : 0f;
        }

        /// <summary>
        /// 采样帧率:没填就跟随呼吸源动画的帧率
        /// </summary>
        private float GetSampleRate()
        {
            if (sampleRate > 0f)
            {
                return sampleRate;
            }

            return breatheClip != null && breatheClip.frameRate > 0f ? breatheClip.frameRate : 30f;
        }

        /// <summary>
        /// 清空动画里的浮点曲线、对象引用曲线与事件
        /// </summary>
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

        /// <summary>
        /// 照抄对象引用曲线(贴图切换、节点显隐等)
        /// </summary>
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

        /// <summary>
        /// 拼装处理报告,方便在窗口里直接确认叠加结果
        /// </summary>
        private string BuildReport(float resultLength, float cycleLength, float rate, int blendedCount, int addedCount,
            List<string> poseOnlyNames, float seamMax)
        {
            float cycles = resultLength / cycleLength;
            string fillModeName = fillMode == EFillMode.Stretch ? "拉伸" : "平铺";
            string report =
                $"叠加 {blendedCount} 条呼吸曲线,新增 {addedCount} 条姿势未驱动的肌肉\n" +
                $"结果 {resultLength:F3}s @ {rate:0.#}fps,呼吸周期 {cycleLength:F3}s × {cycles:F2}({fillModeName}),loopTime = {(loopTime ? 1 : 0)}";

            if (fillMode == EFillMode.Tile && Mathf.Abs(cycles - Mathf.Round(cycles)) > 0.01f)
            {
                report += "\n注意:呼吸周期没有整除结果时长,平铺后循环衔接处会有相位跳变,可以点\"周期吸附:整除结果时长\"";
            }

            if (poseOnlyNames.Count > 0)
            {
                report += $"\n姿势有、呼吸源没有的肌肉(保持原样):{string.Join(", ", poseOnlyNames)}";
            }

            report += seamMax > LoopSeamWarnValue
                ? $"\n呼吸首尾差 {seamMax:F3},循环衔接处会跳,建议把起止点落在同一个呼吸周期上"
                : $"\n循环首尾最大差 {seamMax:F4},可以无缝循环";

            return report;
        }

        /// <summary>
        /// 计算输出路径:固定在姿势动画所在目录。文件名留空时按"姿势动画名_Breathe"自动追加序号,
        /// 填写文件名时直接用该名字,并通过 overwrite 告知是否覆盖已有资源
        /// </summary>
        private string GetOutputPath(out bool overwrite)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(poseClip)).Replace('\\', '/');

            if (!string.IsNullOrEmpty(outputName))
            {
                string namedPath = $"{folder}/{outputName}.anim";
                overwrite = AssetDatabase.LoadAssetAtPath<AnimationClip>(namedPath) != null;
                return namedPath;
            }

            overwrite = false;
            string baseName = poseClip.name + "_Breathe";
            string path = $"{folder}/{baseName}.anim";
            for (int index = 1; AssetDatabase.LoadMainAssetAtPath(path) != null; index++)
            {
                path = $"{folder}/{baseName}_{index}.anim";
            }

            return path;
        }
    }
}
