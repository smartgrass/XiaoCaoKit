using System;
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

        /// <summary>胯部骨骼名:曲线路径的最后一段叫这个就算胯</summary>
        private const string HipsBoneName = "Hips";

        /// <summary>
        /// 可参与混合的身体部位。位序按"从躯干中心往外"排,便于在下拉里顺着看;
        /// 因为细分出了胯/胸/上胸/脖子,位序已经和 UnityEngine.AvatarMaskBodyPart 不一致,
        /// 和 AvatarMask 互转走 MaskPartMap 显式映射 + ApplyAvatarMask 展开,不能按位平移。
        /// 注意:Humanoid 肌肉系统里没有"胯"这条肌肉(Spine 的上一个骨骼节点就是 Hips,但它的运动被
        /// Unity 拆进了 Root Motion 的 RootT / RootQ)。所以"胯"与"根节点"在 Humanoid 数据上是同一组曲线,
        /// 勾任意一个都会混合根运动;只有 Generic 动画可能留下真正的 Hips 骨骼曲线,那种走路径匹配。
        /// </summary>
        [System.Flags]
        public enum EBodyPart
        {
            None = 0,
            Root = 1 << 0,
            Hips = 1 << 1,
            Body = 1 << 2,
            Chest = 1 << 3,
            UpperChest = 1 << 4,
            Head = 1 << 5,
            Neck = 1 << 6,
            LeftArm = 1 << 7,
            RightArm = 1 << 8,
            LeftHand = 1 << 9,
            RightHand = 1 << 10,
            LeftFingers = 1 << 11,
            RightFingers = 1 << 12,
            LeftLeg = 1 << 13,
            RightLeg = 1 << 14,
            LeftFoot = 1 << 15,
            RightFoot = 1 << 16,
        }

        /// <summary>常用部位组合:躯干(脊柱 + 胸 + 上胸 + 双肩),不含胯</summary>
        private static readonly EBodyPart Torso = EBodyPart.Body | EBodyPart.Chest | EBodyPart.UpperChest;

        /// <summary>常用部位组合:上半身(躯干 + 头颈 + 双臂双手)</summary>
        private static readonly EBodyPart UpperBody = Torso | EBodyPart.Head | EBodyPart.Neck |
                                                      EBodyPart.LeftArm | EBodyPart.RightArm |
                                                      EBodyPart.LeftHand | EBodyPart.RightHand |
                                                      EBodyPart.LeftFingers | EBodyPart.RightFingers;

        /// <summary>常用部位组合:下半身(双腿 + 双脚),不含根节点位移(胯不在这里,要单独勾)</summary>
        private static readonly EBodyPart LowerBody = EBodyPart.LeftLeg | EBodyPart.RightLeg |
                                                      EBodyPart.LeftFoot | EBodyPart.RightFoot;

        /// <summary>常用部位组合:全身(上下半身 + 胯,不含根节点位移)</summary>
        private static readonly EBodyPart FullBody = UpperBody | LowerBody | EBodyPart.Hips;

        /// <summary>
        /// 部位在下拉里的中文名,顺序必须和 EBodyPart 的位一一对应(第 i 项 = 第 i 位)。
        /// EnumFlagsField 只能显示英文成员名,所以改用 MaskField 配这张表来显示中文。
        /// </summary>
        private static readonly string[] PartNames =
        {
            "根节点",   // 1 << 0  Root
            "胯",       // 1 << 1  Hips(骨骼曲线,肌肉系统里没有胯)
            "脊柱",     // 1 << 2  Body(腰/脊柱 + 双肩)
            "胸",       // 1 << 3  Chest
            "上胸",     // 1 << 4  UpperChest
            "头",       // 1 << 5  Head
            "脖子",     // 1 << 6  Neck
            "左臂",     // 1 << 7  LeftArm
            "右臂",     // 1 << 8  RightArm
            "左手",     // 1 << 9  LeftHand
            "右手",     // 1 << 10 RightHand
            "左手指",   // 1 << 11 LeftFingers
            "右手指",   // 1 << 12 RightFingers
            "左腿",     // 1 << 13 LeftLeg
            "右腿",     // 1 << 14 RightLeg
            "左脚",     // 1 << 15 LeftFoot
            "右脚",     // 1 << 16 RightFoot
        };

        /// <summary>
        /// AvatarMask 的 Humanoid 部位与本工具部位的对应关系。
        /// AvatarMask 没有脖子和胸/上胸这些细分项,它的 Body 覆盖脊柱 + 胸 + 上胸、Head 覆盖头 + 颈,
        /// 转换后再由 ApplyAvatarMask 展开成细分部位。
        /// </summary>
        private static readonly (AvatarMaskBodyPart Mask, EBodyPart Part)[] MaskPartMap =
        {
            (AvatarMaskBodyPart.Root, EBodyPart.Root),
            (AvatarMaskBodyPart.Body, EBodyPart.Body),
            (AvatarMaskBodyPart.Head, EBodyPart.Head),
            (AvatarMaskBodyPart.LeftArm, EBodyPart.LeftArm),
            (AvatarMaskBodyPart.RightArm, EBodyPart.RightArm),
            (AvatarMaskBodyPart.LeftLeg, EBodyPart.LeftLeg),
            (AvatarMaskBodyPart.RightLeg, EBodyPart.RightLeg),
            (AvatarMaskBodyPart.LeftFingers, EBodyPart.LeftFingers),
            (AvatarMaskBodyPart.RightFingers, EBodyPart.RightFingers),
            (AvatarMaskBodyPart.LeftFootIK, EBodyPart.LeftFoot),
            (AvatarMaskBodyPart.RightFootIK, EBodyPart.RightFoot),
            (AvatarMaskBodyPart.LeftHandIK, EBodyPart.LeftHand),
            (AvatarMaskBodyPart.RightHandIK, EBodyPart.RightHand),
        };

        /// <summary>
        /// 部位与 humanoid 肌肉名前缀的对应关系(肌肉名形如 "Chest Front-Back")
        /// 前缀从头匹配,所以 "Chest " 不会命中 "UpperChest ","Left Arm " 不会命中 "Left ForeArm ";
        /// 匹配时忽略大小写(Unity 前臂肌肉名的 ForeArm/Forearm 拼写不统一)
        /// </summary>
        private static readonly (EBodyPart Part, string Prefix)[] PartPrefixes =
        {
            (EBodyPart.Root, "RootT"),
            (EBodyPart.Root, "RootQ"),
            //Humanoid 里胯(Hips)的运动就是 Root Motion:导入时胯骨的位置/旋转被拆到 RootT / RootQ 上,
            //既没有独立的 "Hips" 肌肉,也不会留下 Hips 骨骼曲线,所以胯与根节点共用这两条曲线。
            //"Spine" 肌肉表达的是脊柱相对胯的旋转,所以胯就是 Spine 的上一个节点。
            (EBodyPart.Hips, "RootT"),
            (EBodyPart.Hips, "RootQ"),
            (EBodyPart.Body, "Spine "),
            (EBodyPart.Body, "Left Shoulder "),
            (EBodyPart.Body, "Right Shoulder "),
            (EBodyPart.Chest, "Chest "),
            (EBodyPart.UpperChest, "UpperChest "),
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

        /// <summary>一条叠加配置:作用部位 + 三轴角度(度)</summary>
        [System.Serializable]
        public class OffsetItem
        {
            [Tooltip("这条叠加作用到的身体部位")]
            public EBodyPart parts = EBodyPart.None;

            /// <summary>
            /// 叠加角度(度)。肌肉命名的正向就是名字里后一个词,实测:
            /// X = 点头(Nod Down-Up),正 = 抬头后仰;Y = 左右转(Turn / Twist Left-Right),正 = 向右转;
            /// Z = 侧倾(Tilt / Left-Right),正 = 向右倾。
            /// </summary>
            [Tooltip("X = 点头(正=抬头)　Y = 左右转(正=向右)　Z = 侧倾(正=向右)")]
            public Vector3 angles;
        }

        [Tooltip("基础动画 A:结果时长、事件、对象曲线与未混合曲线都以它为准")]
        public AnimationClip clipA;

        [Tooltip("目标动画 B:只贡献勾选部位的姿势,权重为 1 时勾选部位完全变成 B")]
        public AnimationClip clipB;

        [Range(0f, 1f)]
        [Tooltip("过渡值:0 = 全 A,1 = 全 B,中间按线性插值")]
        public float weight = 0.5f;

        [Tooltip("参与混合的身体部位;躯干细分为胯/脊柱/胸/上胸,头颈也分开")]
        public EBodyPart parts = Torso | EBodyPart.Hips | EBodyPart.Head | EBodyPart.Neck |
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

        [Tooltip("在混合结果上再整体拧一个角度:每条一个部位 + 一组角度,可以多条同时叠加(头左转、右臂抬起等)。" +
                 "选\"胯\"或\"根节点\"时叠加的是整个人的朝向(RootQ),位移不受影响")]
        public List<OffsetItem> offsets = new List<OffsetItem>();

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
                "其余曲线与动画事件一律沿用 A,结果时长默认与 A 等长。\n" +
                "肌肉系统里没有\"胯\":Spine 的上一个骨骼节点是 Hips,但它的运动被 Unity 记进了 RootT / RootQ," +
                "所以\"胯\"和\"根节点\"共用同一组根运动曲线,勾任意一个都会混合它。",
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
            parts = DrawPartField("混合部位", parts);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("上半身", EditorStyles.miniButtonLeft))
                {
                    parts = UpperBody;
                }

                if (GUILayout.Button("下半身", EditorStyles.miniButtonMid))
                {
                    parts = LowerBody;
                }

                if (GUILayout.Button("全身", EditorStyles.miniButtonMid))
                {
                    parts = FullBody;
                }

                if (GUILayout.Button("清空", EditorStyles.miniButtonRight))
                {
                    parts = EBodyPart.None;
                }
            }
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
            DrawOffsetList();

            EditorGUILayout.Space(6f);
            if (clipA != null && clipB != null)
            {
                EditorGUILayout.LabelField(
                    $"结果 {GetResultLength():F3}s @ {GetSampleRate():0.#}fps" +
                    $"(A {clipA.length:F3}s / B {clipB.length:F3}s)",
                    EditorStyles.miniLabel);
            }

            using (new EditorGUI.DisabledScope(clipA == null))
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

        /// <summary>
        /// 画一个多选部位下拉,选项显示 PartNames 里的中文。
        /// MaskField 的第 i 项对应掩码第 i 位,和 EBodyPart 的位定义正好一一对应。
        /// label 传空串时不画标签(列表行里用,免得和控件挤在一起)。
        /// </summary>
        private static EBodyPart DrawPartField(string label, EBodyPart value, params GUILayoutOption[] options)
        {
            GUIContent content = string.IsNullOrEmpty(label) ? GUIContent.none : new GUIContent(label);
            return (EBodyPart)EditorGUILayout.MaskField(content, (int)value, PartNames, options);
        }

        /// <summary>
        /// 画叠加偏移列表:每条一个部位下拉 + 一组角度,可以多条同时生效,也可以单独删掉某条。
        /// 列表为空表示不做叠加。
        /// </summary>
        private void DrawOffsetList()
        {
            if (offsets == null)
            {
                offsets = new List<OffsetItem>();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("叠加旋转偏移", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加", EditorStyles.miniButton, GUILayout.Width(56f)))
            {
                offsets.Add(new OffsetItem());
            }

            EditorGUILayout.EndHorizontal();

            if (offsets.Count == 0)
            {
                EditorGUILayout.LabelField("(空 = 只做混合;点\"+ 添加\"可在结果上再拧一个角度)", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < offsets.Count; i++)
            {
                OffsetItem item = offsets[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(24f));
                item.parts = DrawPartField("", item.parts, GUILayout.MinWidth(100f), GUILayout.ExpandWidth(true));

                if (GUILayout.Button("×", GUILayout.Width(22f)))
                {
                    offsets.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }

                EditorGUILayout.EndHorizontal();
                item.angles = EditorGUILayout.Vector3Field("角度(度)", item.angles);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.LabelField(
                "X = 上下点头(正=抬头后仰)　Y = 左右转(正=向右转)　Z = 左右侧倾(正=向右倾)",
                EditorStyles.miniLabel);
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
            //EBodyPart 的位序因为插进了脖子已经和 AvatarMask 不一致,这里按映射表逐项转换
            for (int i = 0; i < MaskPartMap.Length; i++)
            {
                if (mask.GetHumanoidBodyPartActive(MaskPartMap[i].Mask))
                {
                    result |= MaskPartMap[i].Part;
                }
            }

            //AvatarMask 的粒度比这里粗:它的 Body 覆盖胯 + 脊柱 + 胸 + 上胸,Head 覆盖头 + 颈,套用时都要展开成细分部位
            if ((result & EBodyPart.Body) != 0)
            {
                result |= EBodyPart.Hips | EBodyPart.Chest | EBodyPart.UpperChest;
            }

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

        /// <summary>
        /// 按当前配置生成动画并另存。给了 B 就做 A→B 的混合;只给 A 时需要勾上"叠加旋转偏移",
        /// 相当于给 A 整体拧一个角度(比如让头一直保持向左转)
        /// </summary>
        private void BakeAndSave()
        {
            if (clipA == null)
            {
                Debug.LogError("请先选择动画 A");
                return;
            }

            if (clipB == null && (offsets == null || offsets.Count == 0))
            {
                Debug.LogError("请先选择动画 B;只想叠加旋转时,在\"叠加旋转偏移\"里至少添加一条配置");
                return;
            }

            if (clipB != null && clipA == clipB)
            {
                Debug.LogError("A 和 B 是同一个动画,不需要混合");
                return;
            }

            if (clipB != null && parts == EBodyPart.None && !blendOtherMuscles)
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

            //只叠加模式下 clipB 是 null,而输出路径上没有同名资源时 pathClip 也是 null,
            //不加 pathClip != null 判断的话 null == null 会被误判成"和 B 同名"
            if (pathClip != null && (pathClip == clipA || pathClip == clipB))
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

            //B 里可参与混合的曲线,按"路径|属性名"索引(没给 B 时是"只叠加"模式,这张表为空)
            //肌肉曲线按肌肉名对齐;胯这种骨骼曲线还要对上路径,所以要连绑定一起存下来
            Dictionary<string, AnimationCurve> curveB = new Dictionary<string, AnimationCurve>();
            Dictionary<string, EditorCurveBinding> bindingB = new Dictionary<string, EditorCurveBinding>();
            if (clipB != null)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clipB))
                {
                    if (!IsBlendable(binding))
                    {
                        continue;
                    }

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clipB, binding);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        string key = GetCurveKey(binding);
                        curveB[key] = curve;
                        bindingB[key] = binding;
                    }
                }
            }

            if (clipB != null && curveB.Count == 0)
            {
                if (logError)
                {
                    Debug.LogError(
                        "B 里没有找到勾选部位的可混合曲线,请检查部位勾选。" +
                        "肌肉曲线要求动画是 Humanoid;勾了\"胯\"的话,还要求动画里真有 Hips 骨骼的 transform 曲线" +
                        "(纯 Humanoid 动画的胯部动作走 Root Motion,归在\"根节点\")");
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
            HashSet<string> blendedKeys = new HashSet<string>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clipA))
            {
                AnimationCurve source = AnimationUtility.GetEditorCurve(clipA, binding);
                if (source == null || source.keys.Length == 0)
                {
                    continue;
                }

                if (IsBlendable(binding))
                {
                    //B 没有这条曲线时按静止值 0 参与插值,权重为 0 且等长时原样输出
                    curveB.TryGetValue(GetCurveKey(binding), out AnimationCurve curveFromB);
                    AnimationCurve blended = weight <= WeightEpsilon && sameLength
                        ? source
                        : BuildBlendCurve(source, clipA.length, curveFromB, clipB.length, weight, resultLength, stepCount);
                    AnimationUtility.SetEditorCurve(target, binding, blended);
                    blendedKeys.Add(GetCurveKey(binding));
                    blendedCount++;
                    continue;
                }

                AnimationUtility.SetEditorCurve(target, binding,
                    sameLength ? source : BuildBlendCurve(source, clipA.length, null, 0f, 0f, resultLength, stepCount));
                copiedCount++;
            }

            //根节点/胯的旋转是四元数(RootQ),逐分量线性插值会缩短模长、还可能因为 q 与 -q 插值出翻转,
            //所以 RootQ 单独按 slerp 重写一遍,覆盖上面按普通肌肉插出来的结果
            if (clipB != null && weight > WeightEpsilon &&
                ((parts & (EBodyPart.Root | EBodyPart.Hips)) != 0 || blendOtherMuscles))
            {
                ApplyRootRotationBlend(target, resultLength, stepCount);
            }

            //B 独有的曲线:权重 0 时是静止姿势,所以直接乘权重
            List<string> bOnlyNames = new List<string>();
            foreach (KeyValuePair<string, AnimationCurve> pair in curveB)
            {
                if (blendedKeys.Contains(pair.Key) || weight <= WeightEpsilon)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(target, bindingB[pair.Key],
                    BuildBlendCurve(null, 0f, pair.Value, clipB.length, weight, resultLength, stepCount));
                bOnlyNames.Add(GetCurveDisplayName(bindingB[pair.Key]));
            }

            //事件与对象引用曲线照抄 A
            AnimationUtility.SetAnimationEvents(target, AnimationUtility.GetAnimationEvents(clipA));
            CopyObjectCurves(clipA, target);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clipA);
            settings.startTime = 0f;
            settings.stopTime = resultLength;
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(target, settings);

            //肌肉曲线都写完后再叠加旋转偏移,这样混合与叠加互不干扰
            string offsetInfo = ApplyOffset(target, resultLength);

            target.EnsureQuaternionContinuity();

            return BuildReport(resultLength, rate, blendedCount, copiedCount, bOnlyNames, clipA.length,
                clipB != null ? clipB.length : 0f, offsetInfo);
        }

        /// <summary>根节点旋转曲线的四个分量(Humanoid 里叫 RootQ,不在 HumanTrait 的肌肉列表里)</summary>
        private static readonly string[] RootRotationProps = { "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

        /// <summary>取一条动画的 RootQ 四条分量曲线;一条都没有时返回 null(这条动画没有根旋转)</summary>
        private static AnimationCurve[] GetRootRotationCurves(AnimationClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            AnimationCurve[] curves = new AnimationCurve[4];
            bool any = false;
            for (int i = 0; i < 4; i++)
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), RootRotationProps[i]);
                curves[i] = AnimationUtility.GetEditorCurve(clip, binding);
                if (curves[i] != null && curves[i].keys.Length > 0)
                {
                    any = true;
                }
            }

            return any ? curves : null;
        }

        /// <summary>在给定时间上取根旋转;缺分量曲线时按单位四元数补齐,四个分量全 0 也当作单位四元数</summary>
        private static Quaternion SampleRootRotation(AnimationCurve[] curves, float time)
        {
            if (curves == null)
            {
                return Quaternion.identity;
            }

            Quaternion q = new Quaternion(
                curves[0] != null ? curves[0].Evaluate(time) : 0f,
                curves[1] != null ? curves[1].Evaluate(time) : 0f,
                curves[2] != null ? curves[2].Evaluate(time) : 0f,
                curves[3] != null ? curves[3].Evaluate(time) : 1f);

            if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f)
            {
                return Quaternion.identity;
            }

            q.Normalize();
            return q;
        }

        /// <summary>根节点/胯的旋转用 slerp 重新生成,替代按分量线性插值的结果</summary>
        private void ApplyRootRotationBlend(AnimationClip target, float resultLength, int stepCount)
        {
            AnimationCurve[] curveA = GetRootRotationCurves(clipA);
            AnimationCurve[] curveB = GetRootRotationCurves(clipB);
            if (curveA == null && curveB == null)
            {
                return;
            }

            Keyframe[][] keys = new Keyframe[4][];
            for (int i = 0; i < 4; i++)
            {
                keys[i] = new Keyframe[stepCount + 1];
            }

            for (int s = 0; s <= stepCount; s++)
            {
                float time = resultLength * s / stepCount;
                Quaternion qa = SampleRootRotation(curveA, ScaleTime(time, clipA.length, resultLength));
                Quaternion qb = SampleRootRotation(curveB, ScaleTime(time, clipB.length, resultLength));
                Quaternion q = Quaternion.Slerp(qa, qb, weight);
                q.Normalize();

                keys[0][s] = new Keyframe(time, q.x);
                keys[1][s] = new Keyframe(time, q.y);
                keys[2][s] = new Keyframe(time, q.z);
                keys[3][s] = new Keyframe(time, q.w);
            }

            for (int i = 0; i < 4; i++)
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), RootRotationProps[i]);
                AnimationUtility.SetEditorCurve(target, binding, MakeLinear(new AnimationCurve(keys[i])));
            }
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
            bool isMuscle = binding.type == typeof(Animator) && !string.IsNullOrEmpty(binding.propertyName);

            //没给 B 时是"只叠加"模式,不参与混合,A 的曲线全部照抄
            if (clipB == null)
            {
                return false;
            }

            if (isMuscle)
            {
                if (blendOtherMuscles)
                {
                    return true;
                }

                if (parts == EBodyPart.None)
                {
                    return false;
                }

                return IsPartMatch(binding.propertyName, parts);
            }

            //骨骼 transform 曲线:Humanoid 动画里不会留下 Hips 骨骼曲线(都转成 RootT/RootQ 了),
            //但 Generic 动画或挂在角色上的额外骨骼可能有,勾了胯且路径末段是 Hips 就混合
            return (parts & EBodyPart.Hips) != 0 && IsHipsBinding(binding);
        }

        /// <summary>判断一条骨骼曲线是不是胯的:看路径最后一段是不是 "Hips"(Armature/Hips 这类也能认)</summary>
        private static bool IsHipsBinding(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.path))
            {
                return false;
            }

            int index = binding.path.LastIndexOf('/');
            string boneName = index >= 0 ? binding.path.Substring(index + 1) : binding.path;
            return string.Equals(boneName, HipsBoneName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 曲线的唯一键。肌肉曲线的 path 是空的,直接用属性名即可;
        /// 骨骼曲线不同骨骼会有同名属性(都是 m_LocalRotation.x 之类),必须带上路径,否则会互相覆盖。
        /// </summary>
        private static string GetCurveKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.propertyName;
        }

        /// <summary>曲线在报告里的显示名:骨骼曲线带上路径,肌肉曲线只有属性名</summary>
        private static string GetCurveDisplayName(EditorCurveBinding binding)
        {
            return string.IsNullOrEmpty(binding.path) ? binding.propertyName : binding.path + "/" + binding.propertyName;
        }

        /// <summary>按前缀判断某条肌肉曲线是否属于给定部位集合</summary>
        private bool IsPartMatch(string propertyName, EBodyPart target)
        {
            if (target == EBodyPart.None)
            {
                return false;
            }

            for (int i = 0; i < PartPrefixes.Length; i++)
            {
                //忽略大小写:Unity 自己的前臂肌肉名拼写就不统一,
                //"Left ForeArm Down-Up" 是大写 A,而 "Left Forearm Stretch" / "Left Forearm Twist In-Out" 是小写 a
                if ((target & PartPrefixes[i].Part) != 0 &&
                    propertyName.StartsWith(PartPrefixes[i].Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在结果动画上整体叠加一个旋转偏移:把勾选部位对应轴的肌肉曲线整体平移一个角度(单位:度)。
        /// 动画里本来没有这条肌肉时,补一条常量曲线,所以静止姿势也能直接被"掰"过去。
        /// </summary>
        private string ApplyOffset(AnimationClip target, float resultLength)
        {
            if (offsets == null || offsets.Count == 0)
            {
                return string.Empty;
            }

            int addCount = 0;
            int clampCount = 0;
            List<string> names = new List<string>();

            //按列表顺序逐条叠加,后面的条目读到的已经是前面改过的曲线,所以同一条肌肉可以被多条累加
            for (int n = 0; n < offsets.Count; n++)
            {
                OffsetItem item = offsets[n];
                if (item == null || item.parts == EBodyPart.None || item.angles == Vector3.zero)
                {
                    continue;
                }

                //遍历 Humanoid 全部肌肉,命中"这条的部位 + 对应轴"的才处理
                for (int i = 0; i < HumanTrait.MuscleCount; i++)
                {
                    string muscle = HumanTrait.MuscleName[i];
                    if (!IsPartMatch(muscle, item.parts))
                    {
                        continue;
                    }

                    float delta = GetAxisOffset(muscle, item.angles);
                    if (Mathf.Approximately(delta, 0f))
                    {
                        continue;
                    }

                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), muscle);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(target, binding);

                    //肌肉有活动范围(大部分是 ±40°,UpperChest 是 ±20°),超出部分会被 Unity 截掉,这里提前夹住并计数
                    float min = HumanTrait.GetMuscleDefaultMin(i);
                    float max = HumanTrait.GetMuscleDefaultMax(i);

                    if (curve != null && curve.keys.Length > 0)
                    {
                        Keyframe[] keys = curve.keys;
                        for (int k = 0; k < keys.Length; k++)
                        {
                            float raw = keys[k].value + delta;
                            if (raw < min || raw > max)
                            {
                                clampCount++;
                            }

                            keys[k].value = Mathf.Clamp(raw, min, max);
                        }

                        curve.keys = keys;
                        AnimationUtility.SetEditorCurve(target, binding, curve);
                    }
                    else
                    {
                        //本来没有这条肌肉:按叠加角度补一条常量曲线(Jaw 之类的小关节范围很小,注意会被截断)
                        if (delta < min || delta > max)
                        {
                            clampCount++;
                        }

                        float value = Mathf.Clamp(delta, min, max);
                        AnimationUtility.SetEditorCurve(target, binding,
                            AnimationCurve.Constant(0f, resultLength, value));
                    }

                    addCount++;
                    names.Add($"#{n + 1} {muscle}");
                }

                //根节点与胯没有肌肉(HumanTrait 的 95 条肌肉里根本没有 RootT / RootQ),
                //上面那个循环碰不到它们,旋转叠加要单独走 RootQ 四元数曲线
                if ((item.parts & (EBodyPart.Root | EBodyPart.Hips)) != 0)
                {
                    addCount += ApplyRootRotationOffset(target, resultLength, item.angles);
                    names.Add($"#{n + 1} RootQ(根节点/胯 旋转)");
                }
            }

            if (addCount == 0)
            {
                return string.Empty;
            }

            string info = $"\n叠加 {addCount} 条曲线({offsets.Count} 条配置):{string.Join(", ", names)}";
            if (clampCount > 0)
            {
                info += $"\n有 {clampCount} 个关键帧超出肌肉活动范围被截断(大部分肌肉是 ±40°,UpperChest ±20°),可以适当减小角度";
            }

            return info;
        }

        /// <summary>
        /// 给根节点/胯整体再转一个角度:RootQ 是四元数曲线,整条按帧取出、左乘世界旋转再写回。
        /// 动画本来没有 RootQ 时按单位四元数(静止姿势)起算,补一条常量四元数曲线。
        /// 返回 1 表示这一条配置确实生效了。
        /// </summary>
        private int ApplyRootRotationOffset(AnimationClip target, float resultLength, Vector3 angles)
        {
            EditorCurveBinding[] bindings = new EditorCurveBinding[4];
            for (int i = 0; i < 4; i++)
            {
                bindings[i] = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), RootRotationProps[i]);
            }

            Quaternion delta = Quaternion.Euler(angles);
            AnimationCurve[] curves = GetRootRotationCurves(target);
            if (curves == null)
            {
                float[] values = { delta.x, delta.y, delta.z, delta.w };
                for (int i = 0; i < 4; i++)
                {
                    AnimationUtility.SetEditorCurve(target, bindings[i], AnimationCurve.Constant(0f, resultLength, values[i]));
                }

                return 1;
            }

            int stepCount = Mathf.Max(2, Mathf.CeilToInt(resultLength * Mathf.Max(1f, target.frameRate)));
            Keyframe[][] keys = new Keyframe[4][];
            for (int i = 0; i < 4; i++)
            {
                keys[i] = new Keyframe[stepCount + 1];
            }

            for (int s = 0; s <= stepCount; s++)
            {
                float time = resultLength * s / stepCount;
                //左乘 = 在世界坐标系上再转一个角度,整条动画统一生效
                Quaternion q = delta * SampleRootRotation(curves, time);
                q.Normalize();

                keys[0][s] = new Keyframe(time, q.x);
                keys[1][s] = new Keyframe(time, q.y);
                keys[2][s] = new Keyframe(time, q.z);
                keys[3][s] = new Keyframe(time, q.w);
            }

            for (int i = 0; i < 4; i++)
            {
                AnimationUtility.SetEditorCurve(target, bindings[i], MakeLinear(new AnimationCurve(keys[i])));
            }

            return 1;
        }

        /// <summary>
        /// 按肌肉名判断它属于哪个旋转轴,返回要叠加的角度。
        /// 躯干用老命名(Front-Back / Twist Left-Right / Left-Right),头颈用 Nod / Turn / Tilt,两套都要认。
        /// </summary>
        private static float GetAxisOffset(string muscle, Vector3 angles)
        {
            //左右转:Twist(躯干) 与 Turn(头颈) 都以 Left-Right 结尾,必须排在侧倾前面判断
            if (muscle.EndsWith("Twist Left-Right") || muscle.EndsWith("Turn Left-Right"))
            {
                return angles.y;
            }

            //左右侧倾:头颈的 Tilt
            if (muscle.EndsWith("Tilt Left-Right"))
            {
                return angles.z;
            }

            //上下点头 / 前后弯腰
            if (muscle.EndsWith("Front-Back") || muscle.EndsWith("Nod Down-Up"))
            {
                return angles.x;
            }

            //躯干侧倾的老命名(Chest Left-Right 之类)
            if (muscle.EndsWith("Left-Right"))
            {
                return angles.z;
            }

            return 0f;
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
            List<string> bOnlyNames, float lengthA, float lengthB, string offsetInfo)
        {
            string report = clipB != null
                ? $"插值 {blendedCount} 条曲线,A 其余 {copiedCount} 条照抄\n"
                : $"只叠加模式:A 的 {copiedCount + blendedCount} 条曲线全部照抄,不做混合\n";

            report +=
                $"结果 {resultLength:F3}s @ {rate:0.#}fps,过渡值 {weight:F2},loopTime = {(loopTime ? 1 : 0)}";

            //时长不一致时 A / B 都会被等比例拉伸到结果时长,短的变慢、长的变快
            if (clipB != null &&
                (Mathf.Abs(lengthA - resultLength) > MinLength || Mathf.Abs(lengthB - resultLength) > MinLength))
            {
                report += $"\n已等比例对齐时长:A {lengthA:F3}s / B {lengthB:F3}s → 结果 {resultLength:F3}s";
            }

            if (bOnlyNames.Count > 0)
            {
                report += $"\nB 独有、A 没有的曲线(按权重写入):{string.Join(", ", bOnlyNames)}";
            }

            if (!string.IsNullOrEmpty(offsetInfo))
            {
                report += offsetInfo;
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
            string baseName = clipB != null ? $"{clipA.name}_Blend_{clipB.name}" : $"{clipA.name}_Offset";
            string path = $"{folder}/{baseName}.anim";
            for (int index = 1; AssetDatabase.LoadMainAssetAtPath(path) != null; index++)
            {
                path = $"{folder}/{baseName}_{index}.anim";
            }

            return path;
        }
    }
}
