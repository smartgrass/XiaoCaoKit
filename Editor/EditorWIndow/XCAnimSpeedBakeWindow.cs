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
    /// 动画速度曲线烘焙工具:按速度曲线对 AnimationClip 做时间重映射,另存为新的动画资源
    /// </summary>
    public class XCAnimSpeedBakeWindow : XiaoCaoWindow
    {
        /// <summary>速度曲线的采样段数,越大时间重映射越精确</summary>
        private const int CurveSampleCount = 512;

        /// <summary>速度倍率的上下限,避免 0 或极端值把时间轴压坏</summary>
        private const float MinSpeed = 0.01f;
        private const float MaxSpeed = 100f;

        /// <summary>烘焙后关键帧的最小时间间隔,避免过密的关键帧堆到同一时刻</summary>
        private const float MinKeyGap = 0.0001f;

        [Tooltip("源动画,烘焙过程不会修改它")]
        public AnimationClip sourceClip;

        [Tooltip("横轴 = 源动画进度 0~1;纵轴 = 速度倍率,1 为原速")]
        [CurveRange(0,0,1,3)]
        public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip("留空则使用\"源动画名_Speed\"并自动追加 _1、_2 ...;填写则用该名字,已存在时覆盖")]
        public string outputName;

        /// <summary>最近一次烘焙出的动画,用于在窗口里直接查看和定位</summary>
        private AnimationClip _bakedClip;

        private string _lastReport;

        [MenuItem(XCEditorTools.XCAnimSpeedBakeWindow)]
        public static XCAnimSpeedBakeWindow Open()
        {
            return OpenWindow<XCAnimSpeedBakeWindow>("动画速度曲线烘焙");
        }

        public override void DrawHead()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("动画速度曲线烘焙", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "按速度曲线重映射动画时间轴并生成新的 .anim,源动画保持不变。\n" +
                "曲线横轴 = 源动画进度 0~1,纵轴 = 速度倍率:值为 2 表示该段播放速度翻倍,烘焙后总时长相应变短。\n" +
                "新动画固定放在源动画所在目录:文件名留空时用\"源动画名_Speed\"并自动追加 _1、_2 ...;填写文件名时直接覆盖同名资源(资源引用不会断)。",
                MessageType.Info);

            if (sourceClip != null && HasValidCurve())
            {
                SpeedTimeMap map = new SpeedTimeMap(sourceClip.length, speedCurve);
                EditorGUILayout.LabelField($"源时长 {sourceClip.length:F3}s → 烘焙后 {map.Length:F3}s(平均速度 {sourceClip.length / map.Length:F2})");
            }

            if (!string.IsNullOrEmpty(_lastReport))
            {
                EditorGUILayout.HelpBox(_lastReport, MessageType.None);
            }

            if (_bakedClip != null)
            {
                EditorGUILayout.BeginHorizontal();
                _bakedClip = (AnimationClip)EditorGUILayout.ObjectField("烘焙结果", _bakedClip, typeof(AnimationClip), false);
                if (GUILayout.Button("定位", GUILayout.Width(46)))
                {
                    Selection.activeObject = _bakedClip;
                    EditorGUIUtility.PingObject(_bakedClip);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 按当前配置烘焙动画:文件名留空时另存为新资源,指定文件名时覆盖同名资源
        /// </summary>
        [Button("烘焙并保存")]
        private void BakeAndSave()
        {
            if (sourceClip == null)
            {
                Debug.LogError("请先选择源动画");
                return;
            }

            if (sourceClip.length <= 0f)
            {
                Debug.LogError($"源动画 {sourceClip.name} 时长为 0,无法烘焙");
                return;
            }

            if (!HasValidCurve())
            {
                Debug.LogError("速度曲线至少需要 1 个关键帧");
                return;
            }

            string path = GetBakePath(out bool overwrite);
            AnimationClip baked;
            if (overwrite)
            {
                //覆盖时把内容写回原资源,保住 GUID,已引用该动画的技能/状态机不用重新绑
                baked = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                //写入前临时解锁,写完再锁回只读
                baked.hideFlags &= ~HideFlags.NotEditable;
                BakeInto(baked, sourceClip, speedCurve);
                EditorUtility.SetDirty(baked);
            }
            else
            {
                baked = Bake(sourceClip, speedCurve);
                AssetDatabase.CreateAsset(baked, path);
            }

            //烘焙产物标记为只读,避免 Flux 的曲线编辑/帧率同步/删事件等流程改动它
            baked.hideFlags |= HideFlags.NotEditable;
            EditorUtility.SetDirty(baked);
            AssetDatabase.SaveAssets();

            string action = overwrite ? "已覆盖" : "已生成";
            _lastReport = $"{action} {path}\n源时长 {sourceClip.length:F3}s → 烘焙后 {baked.length:F3}s";
            _bakedClip = baked;
            Selection.activeObject = baked;
            EditorGUIUtility.PingObject(baked);
            Debug.Log($"---- {_lastReport}");
        }

        /// <summary>
        /// 按速度曲线烘焙出新的动画对象:曲线值越大,对应进度播放越快
        /// </summary>
        public static AnimationClip Bake(AnimationClip source, AnimationCurve speedCurve)
        {
            AnimationClip result = new AnimationClip();
            result.name = source.name + "_Speed";
            BakeInto(result, source, speedCurve);
            return result;
        }

        /// <summary>
        /// 把烘焙结果写进目标动画:会先清空目标原有的曲线与事件,再写入重映射后的数据
        /// </summary>
        public static void BakeInto(AnimationClip target, AnimationClip source, AnimationCurve speedCurve)
        {
            SpeedTimeMap map = new SpeedTimeMap(source.length, speedCurve);

            ClearClip(target);

            target.frameRate = source.frameRate;
            target.wrapMode = source.wrapMode;
            target.legacy = source.legacy;

            BakeCurves(source, target, map);
            BakeObjectCurves(source, target, map);
            BakeEvents(source, target, map);
            BakeSettings(source, target, map);

            target.EnsureQuaternionContinuity();
        }

        /// <summary>
        /// 清空动画里的曲线、对象引用曲线与事件
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
        /// 重映射所有浮点曲线:关键帧时间查表换算,切线按该点速度缩放
        /// </summary>
        private static void BakeCurves(AnimationClip source, AnimationClip result, SpeedTimeMap map)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            for (int i = 0; i < bindings.Length; i++)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, bindings[i]);
                if (curve == null || curve.keys.Length == 0)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(result, bindings[i], RemapCurve(curve, map));
            }
        }

        /// <summary>
        /// 重映射对象引用曲线(如贴图切换、节点显隐)
        /// </summary>
        private static void BakeObjectCurves(AnimationClip source, AnimationClip result, SpeedTimeMap map)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int i = 0; i < bindings.Length; i++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, bindings[i]);
                if (keys == null || keys.Length == 0)
                {
                    continue;
                }

                List<ObjectReferenceKeyframe> bakedKeys = new List<ObjectReferenceKeyframe>(keys.Length);
                for (int k = 0; k < keys.Length; k++)
                {
                    ObjectReferenceKeyframe key = keys[k];
                    key.time = map.MapTime(key.time);
                    RemoveTooCloseKey(bakedKeys, key.time);
                    bakedKeys.Add(key);
                }

                AnimationUtility.SetObjectReferenceCurve(result, bindings[i], bakedKeys.ToArray());
            }
        }

        /// <summary>
        /// 重映射动画事件时间(新建 AnimationEvent,避免改到源动画)
        /// </summary>
        private static void BakeEvents(AnimationClip source, AnimationClip result, SpeedTimeMap map)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(source);
            if (events == null || events.Length == 0)
            {
                return;
            }

            List<AnimationEvent> bakedEvents = new List<AnimationEvent>(events.Length);
            for (int i = 0; i < events.Length; i++)
            {
                AnimationEvent src = events[i];
                bakedEvents.Add(new AnimationEvent
                {
                    functionName = src.functionName,
                    time = map.MapTime(src.time),
                    intParameter = src.intParameter,
                    floatParameter = src.floatParameter,
                    stringParameter = src.stringParameter,
                    objectReferenceParameter = src.objectReferenceParameter,
                    messageOptions = src.messageOptions,
                });
            }

            bakedEvents.Sort((a, b) => a.time.CompareTo(b.time));
            AnimationUtility.SetAnimationEvents(result, bakedEvents.ToArray());
        }

        /// <summary>
        /// 复制动画设置,并把播放区间映射到新的时间轴
        /// </summary>
        private static void BakeSettings(AnimationClip source, AnimationClip result, SpeedTimeMap map)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = map.MapTime(settings.startTime);
            settings.stopTime = map.MapTime(settings.stopTime);
            AnimationUtility.SetAnimationClipSettings(result, settings);
        }

        /// <summary>
        /// 把源曲线按时间重映射成新曲线
        /// </summary>
        private static AnimationCurve RemapCurve(AnimationCurve curve, SpeedTimeMap map)
        {
            Keyframe[] keys = curve.keys;
            List<Keyframe> bakedKeys = new List<Keyframe>(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                float speed = map.GetSpeed(key.time);
                key.time = map.MapTime(key.time);
                //时间被拉伸/压缩后,切线要按速度换算才能保持原曲线形状
                key.inTangent *= speed;
                key.outTangent *= speed;

                RemoveTooCloseKey(bakedKeys, key.time);
                bakedKeys.Add(key);
            }

            return new AnimationCurve(bakedKeys.ToArray());
        }

        /// <summary>
        /// 变速过大时相邻关键帧会被压到同一时刻,这里丢掉前一个
        /// </summary>
        private static void RemoveTooCloseKey(List<Keyframe> bakedKeys, float newTime)
        {
            if (bakedKeys.Count > 0 && newTime - bakedKeys[bakedKeys.Count - 1].time < MinKeyGap)
            {
                bakedKeys.RemoveAt(bakedKeys.Count - 1);
            }
        }

        /// <summary>
        /// 变速过大时相邻关键帧会被压到同一时刻,这里丢掉前一个
        /// </summary>
        private static void RemoveTooCloseKey(List<ObjectReferenceKeyframe> bakedKeys, float newTime)
        {
            if (bakedKeys.Count > 0 && newTime - bakedKeys[bakedKeys.Count - 1].time < MinKeyGap)
            {
                bakedKeys.RemoveAt(bakedKeys.Count - 1);
            }
        }

        /// <summary>
        /// 计算输出路径:固定生成在源动画所在目录。文件名留空时按"源动画名_Speed"自动追加序号,
        /// 填写文件名时直接用该名字并通过 overwrite 告知是否覆盖已有资源
        /// </summary>
        private string GetBakePath(out bool overwrite)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceClip)).Replace('\\', '/');

            if (!string.IsNullOrEmpty(outputName))
            {
                string namedPath = $"{folder}/{outputName}.anim";
                overwrite = AssetDatabase.LoadAssetAtPath<AnimationClip>(namedPath) != null;
                return namedPath;
            }

            overwrite = false;
            string baseName = sourceClip.name + "_Speed";
            string path = $"{folder}/{baseName}.anim";
            for (int index = 1; AssetDatabase.LoadMainAssetAtPath(path) != null; index++)
            {
                path = $"{folder}/{baseName}_{index}.anim";
            }

            return path;
        }

        private bool HasValidCurve()
        {
            return speedCurve != null && speedCurve.keys.Length > 0;
        }

        /// <summary>
        /// 源动画时间轴与烘焙时间轴的换算:输出时间 = 源时长 * ∫(1/速度),
        /// 速度为 1 时结果与原动画完全一致
        /// </summary>
        private class SpeedTimeMap
        {
            private readonly float _sourceLength;
            private readonly AnimationCurve _speedCurve;
            private readonly float _curveStart;
            private readonly float _curveEnd;
            private readonly float[] _timeTable = new float[CurveSampleCount + 1];

            /// <summary>烘焙后动画的总时长</summary>
            public float Length => _timeTable[CurveSampleCount];

            public SpeedTimeMap(float sourceLength, AnimationCurve speedCurve)
            {
                _sourceLength = sourceLength;
                _speedCurve = speedCurve;

                Keyframe[] keys = speedCurve.keys;
                _curveStart = keys[0].time;
                _curveEnd = keys[keys.Length - 1].time;

                float step = 1f / CurveSampleCount;
                float prevSpeed = GetSpeed(0f);
                for (int i = 1; i <= CurveSampleCount; i++)
                {
                    float speed = GetSpeed(i * step);
                    //梯形积分累加输出时长,速度越大累计越慢
                    _timeTable[i] = _timeTable[i - 1] + _sourceLength * step * (1f / prevSpeed + 1f / speed) * 0.5f;
                    prevSpeed = speed;
                }
            }

            /// <summary>
            /// 把源动画时间换算成烘焙后的时间
            /// </summary>
            public float MapTime(float sourceTime)
            {
                float scaled = Mathf.Clamp01(sourceTime / _sourceLength) * CurveSampleCount;
                int index = Mathf.Min((int)scaled, CurveSampleCount - 1);
                return Mathf.Lerp(_timeTable[index], _timeTable[index + 1], scaled - index);
            }

            /// <summary>
            /// 取源动画某个时间点的速度倍率
            /// </summary>
            public float GetSpeed(float sourceTime)
            {
                float progress = Mathf.Clamp01(sourceTime / _sourceLength);
                float value = _speedCurve.Evaluate(Mathf.Lerp(_curveStart, _curveEnd, progress));
                return Mathf.Clamp(value, MinSpeed, MaxSpeed);
            }
        }
    }
}
