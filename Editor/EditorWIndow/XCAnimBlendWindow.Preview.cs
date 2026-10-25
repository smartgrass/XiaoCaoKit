using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AssetEditor.Editor
{
    /// <summary>
    /// 动画混合窗口的实时预览部分:独立的 PreviewScene + RenderTexture,不改动当前场景。
    /// 参数一改就重建临时混合动画,并按当前时间点采样到预览模型上。
    /// </summary>
    public partial class XCAnimBlendWindow
    {
        /// <summary>预览画布最小边长</summary>
        private const float PreviewMinSize = 96f;

        [Tooltip("实时预览用的角色模型,需要有 Animator 与 Humanoid Avatar")]
        public GameObject previewModel;

        [SerializeField] private float previewYaw = 160f;
        [SerializeField] private float previewPitch = 10f;
        [SerializeField] private float previewDistance = 1f;
        [SerializeField] private float previewSpeed = 1f;
        [SerializeField] private bool previewLoop = true;

        private PreviewRenderUtility _previewUtility;
        private RenderTexture _previewTexture;
        private GameObject _previewInstance;
        private int _previewModelId;
        private AnimationClip _previewClip;
        private int _previewSignature = int.MinValue;
        private float _previewTime;
        private bool _previewPlaying;
        private double _previewLastTick;
        private Vector3 _previewCenter;
        private float _previewRadius = 1f;

        /// <summary>整个预览面板:模型选择 + 画布 + 时间轴</summary>
        private void DrawPreviewPane()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            previewModel = (GameObject)EditorGUILayout.ObjectField("预览模型", previewModel, typeof(GameObject), true);
            if (GUILayout.Button("重置视角", GUILayout.Width(64f)))
            {
                previewYaw = 160f;
                previewPitch = 10f;
                previewDistance = 1f;
            }

            EditorGUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetRect(PreviewMinSize, PreviewMinSize, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            DrawPreviewCanvas(rect);

            float length = _previewClip != null ? _previewClip.length : 0f;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_previewPlaying ? "暂停" : "播放", GUILayout.Width(56f)))
            {
                _previewPlaying = !_previewPlaying;
                _previewLastTick = 0.0;
            }

            if (GUILayout.Button("回到起点", GUILayout.Width(72f)))
            {
                _previewTime = 0f;
                _previewPlaying = false;
            }

            using (new EditorGUI.DisabledScope(length <= 0f))
            {
                float time = EditorGUILayout.Slider(_previewTime, 0f, Mathf.Max(length, 0.001f));
                if (!Mathf.Approximately(time, _previewTime))
                {
                    _previewTime = time;
                    _previewPlaying = false;
                }
            }

            previewLoop = GUILayout.Toggle(previewLoop, "循环", GUILayout.Width(44f));
            previewSpeed = EditorGUILayout.Slider(previewSpeed, 0.1f, 2f, GUILayout.Width(110f));
            EditorGUILayout.EndHorizontal();

            if (_previewClip == null && clipA != null && clipB != null)
            {
                EditorGUILayout.LabelField("当前参数下没有可混合的肌肉曲线", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>画预览画面,并把鼠标操作分发给视角控制</summary>
        private void DrawPreviewCanvas(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f, 1f));

            if (previewModel == null)
            {
                GUI.Label(rect, "指定一个 Humanoid 角色模型后即可实时预览", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (rect.width < 32f || rect.height < 32f)
            {
                return;
            }

            EnsurePreviewInstance();
            UpdatePreviewClip();
            HandlePreviewInput(rect);
            AdvancePreviewTime();
            RenderPreview(rect);
        }

        /// <summary>预览模型的实例化:统一放进 PreviewRenderUtility 自己的场景里,不影响当前场景</summary>
        private void EnsurePreviewInstance()
        {
            EnsurePreviewUtility();

            if (_previewInstance != null && _previewModelId == previewModel.GetInstanceID())
            {
                return;
            }

            DestroyPreviewInstance();
            _previewModelId = previewModel.GetInstanceID();

            Scene scene = _previewUtility.camera.gameObject.scene;
            if (PrefabUtility.IsPartOfPrefabAsset(previewModel))
            {
                _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(previewModel, scene);
            }

            if (_previewInstance == null)
            {
                _previewInstance = Object.Instantiate(previewModel);
                SceneManager.MoveGameObjectToScene(_previewInstance, scene);
            }

            _previewInstance.name = previewModel.name + "(Preview)";
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewInstance.transform.position = Vector3.zero;
            _previewInstance.transform.rotation = Quaternion.identity;
            _previewInstance.SetActive(true);

            //用渲染包围盒定初始取景
            Renderer[] renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                _previewCenter = bounds.center;
                _previewRadius = Mathf.Max(0.1f, bounds.extents.magnitude);
            }
            else
            {
                _previewCenter = Vector3.up;
                _previewRadius = 1f;
            }
        }

        /// <summary>创建预览相机/灯光与渲染贴图(URP 下必须补 UniversalAdditionalCameraData,否则渲染会空引用)</summary>
        private void EnsurePreviewUtility()
        {
            if (_previewUtility != null)
            {
                return;
            }

            _previewUtility = new PreviewRenderUtility(true);
            Camera camera = _previewUtility.camera;
            camera.cameraType = CameraType.Game;
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 30f;

            _previewUtility.lights[0].intensity = 1f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            if (_previewUtility.lights.Length > 1)
            {
                _previewUtility.lights[1].intensity = 0.6f;
                _previewUtility.lights[1].transform.rotation = Quaternion.Euler(30f, 200f, 0f);
            }

            _previewTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = _previewTexture;
        }

        /// <summary>参数变化时重建临时混合动画(不落盘)</summary>
        private void UpdatePreviewClip()
        {
            int signature = BuildPreviewSignature();
            if (_previewClip != null && signature == _previewSignature)
            {
                return;
            }

            _previewSignature = signature;

            if (_previewClip != null)
            {
                Object.DestroyImmediate(_previewClip);
                _previewClip = null;
            }

            if (clipA == null || clipB == null || clipA == clipB)
            {
                return;
            }

            float resultLength = GetResultLength();
            if (resultLength < MinLength)
            {
                return;
            }

            AnimationClip clip = new AnimationClip();
            if (string.IsNullOrEmpty(FillClip(clip, resultLength, false)))
            {
                Object.DestroyImmediate(clip);
                return;
            }

            clip.name = "PreviewBlend";
            _previewClip = clip;
            _previewTime = Mathf.Clamp(_previewTime, 0f, clip.length);
        }

        /// <summary>参数指纹,只有它变了才重建预览动画</summary>
        private int BuildPreviewSignature()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (clipA != null ? clipA.GetInstanceID() : 0);
                hash = hash * 31 + (clipB != null ? clipB.GetInstanceID() : 0);
                hash = hash * 31 + weight.GetHashCode();
                hash = hash * 31 + (int)parts;
                hash = hash * 31 + (blendOtherMuscles ? 1 : 0);
                hash = hash * 31 + targetLength.GetHashCode();
                hash = hash * 31 + sampleRate.GetHashCode();
                hash = hash * 31 + (loopTime ? 1 : 0);
                return hash;
            }
        }

        /// <summary>拖左键转视角,滚轮拉远近</summary>
        private void HandlePreviewInput(Rect rect)
        {
            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0 && rect.Contains(current.mousePosition))
            {
                previewYaw += current.delta.x * 0.6f;
                previewPitch = Mathf.Clamp(previewPitch - current.delta.y * 0.4f, -80f, 80f);
                current.Use();
                Repaint();
            }
            else if (current.type == EventType.ScrollWheel && rect.Contains(current.mousePosition))
            {
                previewDistance = Mathf.Clamp(previewDistance + current.delta.y * 0.03f, 0.3f, 3f);
                current.Use();
                Repaint();
            }
        }

        /// <summary>推进播放时间</summary>
        private void AdvancePreviewTime()
        {
            if (!_previewPlaying || _previewClip == null)
            {
                _previewLastTick = 0.0;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float delta = _previewLastTick > 0.0 ? (float)(now - _previewLastTick) : 0f;
            _previewLastTick = now;

            float length = Mathf.Max(0.001f, _previewClip.length);
            _previewTime += delta * previewSpeed;
            if (_previewTime >= length)
            {
                _previewTime = previewLoop ? Mathf.Repeat(_previewTime, length) : length;
            }
        }

        /// <summary>渲染一帧到渲染贴图并画到窗口上</summary>
        private void RenderPreview(Rect rect)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 64, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 64, 1024);
            if (_previewTexture == null || _previewTexture.width != width || _previewTexture.height != height)
            {
                if (_previewTexture != null)
                {
                    _previewUtility.camera.targetTexture = null;
                    Object.DestroyImmediate(_previewTexture);
                }

                _previewTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _previewUtility.camera.targetTexture = _previewTexture;
            }

            ApplyPreviewPose();
            UpdatePreviewCamera();
            _previewUtility.camera.Render();
            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.StretchToFill);
        }

        /// <summary>把当前时间点的姿势采样到预览模型上</summary>
        private void ApplyPreviewPose()
        {
            if (_previewClip == null || _previewInstance == null)
            {
                return;
            }

            _previewClip.SampleAnimation(_previewInstance, Mathf.Clamp(_previewTime, 0f, _previewClip.length));
            //采样会带出根位移,预览时把模型钉回原点,避免越走越远
            _previewInstance.transform.position = Vector3.zero;
        }

        /// <summary>按当前角度/距离摆相机</summary>
        private void UpdatePreviewCamera()
        {
            Quaternion rotation = Quaternion.Euler(previewPitch, previewYaw, 0f);
            float distance = Mathf.Max(0.2f, _previewRadius * 2.4f * previewDistance);
            Transform cameraTransform = _previewUtility.camera.transform;
            cameraTransform.position = _previewCenter - rotation * Vector3.forward * distance;
            cameraTransform.rotation = rotation;
        }

        private void OnDisable()
        {
            ReleasePreview();
        }

        /// <summary>关窗时把临时相机、贴图、模型、动画全部清理掉</summary>
        private void ReleasePreview()
        {
            DestroyPreviewInstance();

            if (_previewClip != null)
            {
                Object.DestroyImmediate(_previewClip);
                _previewClip = null;
            }

            if (_previewUtility != null)
            {
                if (_previewTexture != null)
                {
                    _previewUtility.camera.targetTexture = null;
                    Object.DestroyImmediate(_previewTexture);
                    _previewTexture = null;
                }

                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            _previewSignature = int.MinValue;
            _previewPlaying = false;
        }

        private void DestroyPreviewInstance()
        {
            if (_previewInstance != null)
            {
                Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _previewModelId = 0;
        }
    }
}
