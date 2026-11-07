using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 头像拍照棚：选中角色/怪物 prefab 实时预览，调整角度后一键导出 128x128 透明背景头像 PNG。
/// 支持单张截图与整个目录批量导出。
/// </summary>
public class AvatarStudioWindow : EditorWindow
{
    private const int PreviewSize = 256;
    private const int ExportSize = 128;

    private DefaultAsset prefabFolder;      // prefab 源目录
    private DefaultAsset outputFolder;      // 头像输出目录
    private Vector3 camDir = new Vector3(0.4f, 0.25f, -1f); // 相机方向（yaw/pitch 由滑条控制）
    private float yawDeg = 30f;
    private float pitchDeg = 15f;
    private float padding = 1.1f;           // 取景留白系数
    [Range(0.1f, 1f)] public float cropHeight = 0.3f; // 取景高度占身高的比例（1=全身，0.3≈头部特写）
    private Vector2 panOffset;              // 手动平移偏移（相机局部空间的 世界单位）
    private float lastViewRadius = 1f;      // 上一次渲染的取景半径，用于把鼠标拖拽像素换算成世界单位
    private bool overwrite = false;         // 批量时是否覆盖已存在头像
    private GameObject currentPrefab;
    private PreviewRenderUtility preview;
    private RectTransform rect;
    private Vector2 scroll;

    // —— 灯光（角色偏暗时调这里）——
    [SerializeField] private float lightIntensity = 1.6f;   // 主光强度，补光/轮廓光按比例联动
    [SerializeField] private float ambientIntensity = 0.35f; // 环境光亮度（中性灰），决定暗部亮度
    [SerializeField] private bool castShadow = false;        // 是否让主光投自阴影（关闭更亮更干净）
    private Light rimLight;                                  // 自建轮廓光，用来把角色边缘从背景里勾出来

    // —— 姿势（可选）——
    private const string DefaultIdlePosePath =
        "Assets/_ResArt/AnimHuman/Gril_GreatSword_AnimSet/Animation/Humanoid/idle.fbx";
    [SerializeField] private AnimationClip idlePoseClip;   // 用来摆姿势的 Idle 动画，默认取上面这个 fbx 里的 idle
    [SerializeField] private bool useIdlePose = false;     // 打开后渲染前会把角色摆成该 Idle 姿势
    [SerializeField] private float idlePoseTime = 0f;      // 取 Idle 动画里的哪个时间点（秒）
    [SerializeField] private bool foldPose = true;         // 「姿势」分组是否展开
    [SerializeField] private bool foldLight = true;        // 「灯光」分组是否展开

    [MenuItem("Tools/头像拍照棚")]
    public static void Open()
    {
        var w = GetWindow<AvatarStudioWindow>("头像拍照棚");
        w.minSize = new Vector2(560, 620); // 预览区要并排放两张 256 的图
    }

    private void OnEnable()
    {
        if (prefabFolder == null)
            prefabFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/_Res/Role/Body");
        if (outputFolder == null)
            outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/_Res/Sprite/RoleTexture");
        if (idlePoseClip == null)
            idlePoseClip = LoadFirstClip(DefaultIdlePosePath);
        Selection.selectionChanged += Repaint;
        // 编辑器打开时自动带入当前选中的 prefab
        if (Selection.activeGameObject == null && Selection.activeObject is GameObject go)
            currentPrefab = go;
        else if (Selection.activeObject is GameObject g2)
            currentPrefab = g2;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
        CleanupPreview();
    }

    private void OnGUI()
    {
        // 顶部固定区：预览图 + 导出按钮，都不跟着下面的参数一起滚动
        DrawPreviewPanels();
        DrawExportBar();
        DrawSeparator();

        scroll = GUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("目录 / 当前 Prefab", EditorStyles.boldLabel);
        WithLabelWidth(74f, () =>
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefab 目录", prefabFolder, typeof(DefaultAsset), false);
                outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("输出目录", outputFolder, typeof(DefaultAsset), false);
                currentPrefab = (GameObject)EditorGUILayout.ObjectField("当前 Prefab", currentPrefab, typeof(GameObject), false);
            }
        });

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("取景（选中 Project 里的 prefab 即可预览）", EditorStyles.boldLabel);
        if (Selection.activeObject is GameObject sel && sel.scene.IsValid() == false)
            currentPrefab = sel;

        EditorGUI.BeginChangeCheck();
        WithLabelWidth(62f, () =>
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                yawDeg = EditorGUILayout.Slider("水平角度", yawDeg, -180f, 180f);
                pitchDeg = EditorGUILayout.Slider("俯仰角度", pitchDeg, -89f, 89f);
            }
        });
        WithLabelWidth(62f, () =>
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                padding = EditorGUILayout.Slider("留白", padding, 1.0f, 2.0f);
                cropHeight = EditorGUILayout.Slider(new GUIContent("取景高度", "1=全身，0.3≈头部特写"), cropHeight, 0.1f, 1f);
            }
        });
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            var pan = EditorGUILayout.Vector2Field("平移偏移", panOffset);
            if (EditorGUI.EndChangeCheck()) panOffset = pan;
            if (GUILayout.Button("归零", GUILayout.Width(40))) { panOffset = Vector2.zero; Repaint(); }
        }
        if (EditorGUI.EndChangeCheck()) Repaint();

        EditorGUILayout.Space(6);
        // 折叠时也能看出姿势是否生效
        string poseTitle = useIdlePose ? "姿势（已应用 Idle）" : "姿势";
        foldPose = EditorGUILayout.Foldout(foldPose, poseTitle, true, EditorStyles.foldoutHeader);
        if (foldPose)
        {
            EditorGUI.indentLevel++;
            idlePoseClip = (AnimationClip)EditorGUILayout.ObjectField(
                new GUIContent("Idle 姿势", "留空则保持 prefab 原始姿势；人形角色按 Avatar 重定向"), idlePoseClip, typeof(AnimationClip), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(useIdlePose ? "取消 Idle 姿势" : "应用 Idle 姿势", GUILayout.Height(24)))
                {
                    if (idlePoseClip == null) idlePoseClip = LoadFirstClip(DefaultIdlePosePath);
                    if (idlePoseClip == null)
                        Debug.LogWarning($"[头像拍照棚] 没找到 Idle 动画，请手动把 clip 拖到“Idle 姿势”：{DefaultIdlePosePath}");
                    else
                        useIdlePose = !useIdlePose;
                    Repaint();
                }
                if (GUILayout.Button("重置为默认 idle", GUILayout.Height(24)))
                {
                    idlePoseClip = LoadFirstClip(DefaultIdlePosePath);
                    idlePoseTime = 0f;
                    Repaint();
                }
            }
            using (new EditorGUI.DisabledScope(idlePoseClip == null))
            {
                EditorGUI.BeginChangeCheck();
                float maxTime = idlePoseClip != null ? Mathf.Max(0.05f, idlePoseClip.length) : 1f;
                idlePoseTime = EditorGUILayout.Slider(new GUIContent("取样时间", "Idle 动画里取哪一帧当姿势"), idlePoseTime, 0f, maxTime);
                if (EditorGUI.EndChangeCheck()) Repaint();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);
        foldLight = EditorGUILayout.Foldout(foldLight, "灯光（角色偏暗时调这里）", true, EditorStyles.foldoutHeader);
        if (foldLight)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            WithLabelWidth(62f, () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    lightIntensity = EditorGUILayout.Slider(new GUIContent("主光强度"), lightIntensity, 0.3f, 4f);
                    ambientIntensity = EditorGUILayout.Slider(new GUIContent("环境光", "提亮暗部：0=只剩主光照到的地方"), ambientIntensity, 0f, 1f);
                }
            });
            using (new EditorGUILayout.HorizontalScope())
            {
                castShadow = EditorGUILayout.Toggle(new GUIContent("开启自阴影"), castShadow);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重置灯光", GUILayout.Width(80)))
                {
                    lightIntensity = 1.6f;
                    ambientIntensity = 0.35f;
                    castShadow = false;
                }
            }
            if (EditorGUI.EndChangeCheck()) Repaint();
            EditorGUI.indentLevel--;
        }

        GUILayout.EndScrollView();
    }

    /// <summary>预览图下方的导出按钮行（固定区，不随参数滚动）。</summary>
    private void DrawExportBar()
    {
        GUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(currentPrefab == null || outputFolder == null))
            {
                if (GUILayout.Button("截图保存 (128x128)", GUILayout.Height(28)))
                    ExportOne(currentPrefab);
            }
            using (new EditorGUI.DisabledScope(prefabFolder == null || outputFolder == null))
            {
                if (GUILayout.Button("批量导出目录", GUILayout.Height(28)))
                    ExportBatch();
            }
            overwrite = GUILayout.Toggle(overwrite, new GUIContent("覆盖已有", "批量导出时覆盖已存在的头像"),
                GUILayout.Width(76));
        }
    }

    /// <summary>顶部固定的预览区：左边本次渲染、右边已导出头像，不随下面参数区滚动。</summary>
    private void DrawPreviewPanels()
    {
        const float gap = 14f; // 两格之间留的空隙
        float cell = Mathf.Clamp((position.width - 12f - gap) * 0.5f, 96f, PreviewSize);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(cell)))
            {
                var leftRect = GUILayoutUtility.GetRect(cell, cell);
                HandlePreviewDrag(leftRect);
                if (currentPrefab != null)
                    DrawPreview(leftRect, currentPrefab, PreviewSize);
                else
                    DrawPlaceholder(leftRect, "在 Project 中选中一个 prefab");
                GUILayout.Label("本次渲染", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.Space(gap);
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(cell)))
            {
                var rightRect = GUILayoutUtility.GetRect(cell, cell);
                DrawExistingAvatar(rightRect, currentPrefab);
                GUILayout.Label("已有头像", EditorStyles.centeredGreyMiniLabel);
            }
        }
    }

    /// <summary>画一条水平分隔线（颜色按编辑器皮肤区分，暗色主题下用更深的灰）。</summary>
    private static void DrawSeparator()
    {
        GUILayout.Space(6f);
        var line = EditorGUILayout.GetControlRect(false, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
            ? new Color(0.1f, 0.1f, 0.1f, 1f)
            : new Color(0.6f, 0.6f, 0.6f, 1f));
        GUILayout.Space(6f);
    }

    /// <summary>临时改 label 宽度画一段 GUI，画完恢复；用来把两个带标签的控件并排到一行。</summary>
    private static void WithLabelWidth(float width, System.Action draw)
    {
        float prev = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = width;
        try
        {
            draw();
        }
        finally
        {
            EditorGUIUtility.labelWidth = prev;
        }
    }

    /// <summary>在预览图上按住左键拖拽即可平移取景（内容跟随鼠标移动）。</summary>
    private void HandlePreviewDrag(Rect rect)
    {
        if (currentPrefab == null) return;
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            e.Use();
        else if (e.type == EventType.MouseDrag && e.button == 0 && rect.Contains(e.mousePosition))
        {
            // 像素增量换算成世界单位；内容跟随鼠标 = 相机反向平移
            float worldPerPixel = 2f * lastViewRadius * padding / rect.height;
            panOffset += new Vector2(-e.delta.x, e.delta.y) * worldPerPixel;
            e.Use();
            Repaint();
        }
    }

    /// <summary>渲染 prefab 到指定尺寸的透明背景贴图并返回；预览与导出共用。</summary>
    private Texture2D RenderAvatar(GameObject prefab, int size)
    {
        EnsurePreview();
        var cam = preview.camera;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 完全透明背景
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;

        // 清空临时场景后实例化 prefab（保留 PreviewRenderUtility 自带的相机与灯光）
        var camScene = preview.camera.gameObject.scene;
        foreach (var rootGo in camScene.GetRootGameObjects())
        {
            // 跳过相机和灯光，它们也是场景根对象，不能删
            if (rootGo == preview.camera.gameObject) continue;
            if (rootGo.GetComponent<Light>() != null) continue;
            Object.DestroyImmediate(rootGo);
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camScene);
        if (instance == null) instance = Object.Instantiate(prefab);
        instance.transform.position = Vector3.zero;
        instance.isStatic = false;

        // 先套 Idle 姿势再算包围盒，取景才会贴合真实姿态
        ApplyIdlePose(instance);

        // 计算整体包围盒并取景
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        Quaternion rot = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        float radius = bounds.extents.magnitude;

        // 取景：从头顶向下裁取一定比例的身高（1=全身，0.3≈头部特写）
        float viewHeight = bounds.size.y * cropHeight;
        Vector3 viewCenter = bounds.center + Vector3.up * (bounds.extents.y - viewHeight * 0.5f);
        float viewRadius = Mathf.Max(viewHeight, bounds.size.x * cropHeight, bounds.size.z * cropHeight) * 0.5f;
        lastViewRadius = viewRadius;

        // 应用手动平移偏移（相机局部右/上方向）
        viewCenter += rot * new Vector3(panOffset.x, panOffset.y, 0f);

        cam.orthographicSize = viewRadius * padding;
        cam.transform.position = viewCenter - rot * Vector3.forward * (radius * 4f);
        cam.transform.rotation = rot;

        // 灯光跟随相机摆放，换角度看角色时正面照样有主光
        ApplyLighting(rot);

        // 环境光临时换成中性灰平光：URP 下暗部亮度几乎全靠它，用场景默认值角色容易发闷
        var prevMode = RenderSettings.ambientMode;
        var prevLight = RenderSettings.ambientLight;
        var prevIntensity = RenderSettings.ambientIntensity;
        try
        {
            var ambient = new Color(ambientIntensity, ambientIntensity, ambientIntensity, 1f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientIntensity = 1f;
            preview.ambientColor = ambient;
            preview.camera.Render();
        }
        finally
        {
            RenderSettings.ambientMode = prevMode;
            RenderSettings.ambientLight = prevLight;
            RenderSettings.ambientIntensity = prevIntensity;
        }

        // 读回像素（保留 alpha 通道）
        var rt = preview.camera.targetTexture;
        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        if (rt.width == size)
        {
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        }
        else
        {
            // 预览 RT 与导出尺寸不同时，用 GPU Blit 缩放
            var tmp = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rt, tmp);
            RenderTexture.active = tmp;
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            RenderTexture.ReleaseTemporary(tmp);
        }
        tex.Apply();
        RenderTexture.active = prevActive;
        return tex;
    }

    private void DrawPreview(Rect rect, GameObject prefab, int size)
    {
        var tex = RenderAvatar(prefab, size);
        if (tex == null)
        {
            DrawPlaceholder(rect, "没有可渲染的 Renderer");
            return;
        }
        EditorGUI.DrawTextureTransparent(rect, tex, ScaleMode.ScaleToFit);
        DestroyImmediate(tex);
    }

    /// <summary>画出输出目录里同名的已导出头像，用于和本次渲染结果左右对比。</summary>
    private void DrawExistingAvatar(Rect rect, GameObject prefab)
    {
        var tex = LoadExistingAvatar(prefab);
        if (tex == null)
        {
            DrawPlaceholder(rect, prefab == null ? "在 Project 中选中一个 prefab" : "尚未导出");
            return;
        }
        EditorGUI.DrawTextureTransparent(rect, tex, ScaleMode.ScaleToFit);
    }

    /// <summary>按「输出目录 / prefab 名.png」找已存在的头像；找不到返回 null。</summary>
    private Texture2D LoadExistingAvatar(GameObject prefab)
    {
        if (prefab == null || outputFolder == null) return null;
        string dir = AssetDatabase.GetAssetPath(outputFolder);
        if (!AssetDatabase.IsValidFolder(dir)) return null;
        return AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{prefab.name}.png");
    }

    /// <summary>画空占位框。</summary>
    private static void DrawPlaceholder(Rect rect, string text)
    {
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        GUI.Label(rect, text, EditorStyles.centeredGreyMiniLabel);
    }

    private PreviewRenderUtility EnsurePreview()
    {
        if (preview != null) return preview;
        preview = new PreviewRenderUtility(true);
        preview.camera.targetTexture = new RenderTexture(PreviewSize, PreviewSize, 24, RenderTextureFormat.ARGB32);
        // URP 下裸相机会在 RenderSingleCamera 里空引用，必须补上 UniversalAdditionalCameraData 并按 Game 相机渲染
        preview.camera.cameraType = CameraType.Game;
        if (preview.camera.GetComponent<UniversalAdditionalCameraData>() == null)
            preview.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        // 内置的两盏灯由 ApplyLighting 在每次渲染前按相机朝向重新摆放，这里只补一盏自建轮廓光
        CreateRimLight();
        return preview;
    }

    /// <summary>按相机朝向摆放主光 / 补光 / 轮廓光，保证任意取景角度下角色正面都有光。</summary>
    private void ApplyLighting(Quaternion camRot)
    {
        var lights = preview.lights;
        if (lights.Length > 0)
        {
            var key = lights[0];
            key.enabled = true;
            key.type = LightType.Directional;
            key.lightmapBakeType = LightmapBakeType.Realtime; // 预览只走实时渲染，Baked 灯不发光
            key.color = new Color(1f, 0.97f, 0.92f); // 略暖的主光
            key.intensity = lightIntensity;
            key.shadows = castShadow ? LightShadows.Soft : LightShadows.None;
            // 相对相机：从右上方 45° 打过来
            key.transform.rotation = camRot * Quaternion.Euler(25f, -32f, 0f);
        }

        if (lights.Length > 1)
        {
            var fill = lights[1];
            fill.enabled = true;
            fill.type = LightType.Directional;
            fill.lightmapBakeType = LightmapBakeType.Realtime;
            fill.color = new Color(0.82f, 0.88f, 1f); // 偏冷的补光，压一下暗部
            fill.intensity = lightIntensity * 0.5f;
            fill.shadows = LightShadows.None;
            // 相对相机：从左前方低角度补
            fill.transform.rotation = camRot * Quaternion.Euler(5f, 55f, 0f);
        }

        if (rimLight != null)
        {
            rimLight.intensity = lightIntensity * 0.7f;
            rimLight.shadows = LightShadows.None;
            // 相对相机：从背后偏上，勾出轮廓
            rimLight.transform.rotation = camRot * Quaternion.Euler(-12f, 165f, 0f);
        }
    }

    /// <summary>在预览场景里补一盏轮廓光（PreviewRenderUtility 只自带两盏灯）。</summary>
    private void CreateRimLight()
    {
        if (rimLight != null || preview == null) return;
        var go = new GameObject("AvatarStudio_RimLight");
        rimLight = go.AddComponent<Light>();
        rimLight.enabled = true;
        rimLight.type = LightType.Directional;
        rimLight.lightmapBakeType = LightmapBakeType.Realtime;
        rimLight.color = new Color(1f, 0.94f, 0.86f);
        rimLight.shadows = LightShadows.None;
        // 必须挂进预览场景，否则不会被预览相机渲染到
        preview.AddSingleGO(go);
    }

    /// <summary>从 fbx / 动画文件里取第一个 AnimationClip，路径无效时返回 null。</summary>
    private static AnimationClip LoadFirstClip(string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    /// <summary>
    /// 把预览实例摆成 Idle 姿势：Humanoid 动画的曲线存在肌肉空间里，直接改 Transform 无效，
    /// 必须通过 Animator + Playable 求值一次，让结果写回骨骼。
    /// </summary>
    private void ApplyIdlePose(GameObject instance)
    {
        if (!useIdlePose || idlePoseClip == null) return;

        var animator = instance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[头像拍照棚] {instance.name} 没有 Animator，Idle 姿势已跳过");
            return;
        }
        // 人形动画只能驱动 Humanoid Avatar，否则采样出来骨骼不动
        if (idlePoseClip.humanMotion && animator.avatar != null && !animator.avatar.isHuman)
        {
            Debug.LogWarning($"[头像拍照棚] {instance.name} 的 Avatar 不是 Humanoid，Idle 姿势已跳过");
            return;
        }

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // 避免被视锥剔除跳过求值
        if (animator.avatar != null) animator.Rebind();           // 先回 bind pose，不受 prefab 保存姿势干扰

        var graph = PlayableGraph.Create("AvatarStudioIdlePose");
        try
        {
            var playable = AnimationClipPlayable.Create(graph, idlePoseClip);
            playable.SetApplyFootIK(false);
            playable.SetTime(Mathf.Clamp(idlePoseTime, 0f, idlePoseClip.length));
            var output = AnimationPlayableOutput.Create(graph, "AvatarStudioIdlePose", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            graph.Evaluate(0f); // 立即求值一帧，姿势写进骨骼
        }
        finally
        {
            graph.Destroy();
        }

        // 摆完姿势蒙皮可能超出原始包围盒，关掉剔除避免角色被裁掉
        foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;
    }

    private void CleanupPreview()
    {
        if (preview == null) return;
        if (preview.camera != null && preview.camera.targetTexture != null)
            DestroyImmediate(preview.camera.targetTexture);
        preview.Cleanup();
        preview = null;
        rimLight = null; // 跟着预览场景一起被销毁
    }

    /// <summary>导出单个 prefab 的头像。</summary>
    private void ExportOne(GameObject prefab)
    {
        string outDir = AssetDatabase.GetAssetPath(outputFolder);
        if (!AssetDatabase.IsValidFolder(outDir))
        {
            EditorUtility.DisplayDialog("错误", "输出目录无效", "确定");
            return;
        }
        string name = prefab.name + ".png";
        string path = Path.Combine(outDir, name).Replace('\\', '/');
        SavePng(prefab, path);
        Debug.Log($"[头像拍照棚] 已导出 {path}");
        Repaint(); // 刷新右侧「已有头像」
    }

    /// <summary>批量导出目录下所有 prefab 的头像。</summary>
    private void ExportBatch()
    {
        string folderPath = AssetDatabase.GetAssetPath(prefabFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("错误", "Prefab 目录无效", "确定");
            return;
        }
        string outDir = AssetDatabase.GetAssetPath(outputFolder);
        if (!AssetDatabase.IsValidFolder(outDir))
        {
            EditorUtility.DisplayDialog("错误", "输出目录无效", "确定");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int done = 0;
        try
        {
            foreach (var guid in guids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                string outPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(prefabPath) + ".png").Replace('\\', '/');
                if (!overwrite && File.Exists(outPath)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;
                SavePng(prefab, outPath);
                done++;
                if (EditorUtility.DisplayCancelableProgressBar("批量导出头像", prefabPath, (float)done / guids.Length))
                    break;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        Debug.Log($"[头像拍照棚] 批量导出完成，本次导出 {done} 张，目录 {outDir}");
        Repaint(); // 刷新右侧「已有头像」
    }

    /// <summary>渲染并保存为 PNG，同时导入为 Sprite。</summary>
    private void SavePng(GameObject prefab, string path)
    {
        var tex = RenderAvatar(prefab, ExportSize);
        if (tex == null)
        {
            Debug.LogWarning($"[头像拍照棚] {prefab.name} 没有任何 Renderer，跳过");
            return;
        }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
