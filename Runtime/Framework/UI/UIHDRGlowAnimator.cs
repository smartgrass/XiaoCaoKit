using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCao.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIHDRGlowAnimator : MonoBehaviour
    {
        private const string DefaultShaderName = "UI/HDR Glow";

        private static readonly int BaseBrightnessId = Shader.PropertyToID("_BaseBrightness");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

        [Header("Target")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private Shader glowShader;

        [Header("Animation")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool previewInEditMode = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool pingPong = true;
        [SerializeField] [Min(0.01f)] private float duration = 1.2f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Glow")]
        [SerializeField] [Min(0f)] private float minGlowIntensity = 0.75f;
        [SerializeField] [Min(0f)] private float maxGlowIntensity = 3f;
        [SerializeField] [Min(0f)] private float baseBrightness = 1f;
        [SerializeField] private bool animateGlowColor;
        [SerializeField] [ColorUsage(true, true)] private Color minGlowColor = Color.white;
        [SerializeField] [ColorUsage(true, true)] private Color maxGlowColor = new Color(2f, 1.4f, 0.8f, 1f);

        [Header("State")]
        [SerializeField] [Range(0f, 1f)] private float normalizedTime;

        private Material originalMaterial;
        private Material runtimeMaterial;
        private Material runtimeSourceMaterial;
        private Shader runtimeSourceShader;
        private int direction = 1;
        private bool isPlaying;

#if UNITY_EDITOR
        private double lastEditorTime;
#endif

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
            glowShader = Shader.Find(DefaultShaderName);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureRuntimeMaterial();

            if (playOnEnable && (Application.isPlaying || previewInEditMode))
            {
                Restart();
                return;
            }

            ApplyAnimationSample(normalizedTime);
        }

        private void Update()
        {
            if (!Application.isPlaying && !previewInEditMode)
            {
                return;
            }

            EnsureRuntimeMaterial();
            if (runtimeMaterial == null)
            {
                return;
            }

            if (isPlaying)
            {
                AdvanceAnimation(GetDeltaTime());

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                }
#endif
            }

            ApplyAnimationSample(normalizedTime);
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
            maxGlowIntensity = Mathf.Max(minGlowIntensity, maxGlowIntensity);
            baseBrightness = Mathf.Max(0f, baseBrightness);

            EnsureReferences();
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsureRuntimeMaterial();
            ApplyAnimationSample(normalizedTime);
        }

        private void OnDisable()
        {
            isPlaying = false;
            direction = 1;
            ReleaseRuntimeMaterial();
            ResetEditorClock();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeMaterial();
        }

        public void Play()
        {
            isPlaying = true;
            ResetEditorClock();
        }

        public void Restart()
        {
            normalizedTime = 0f;
            direction = 1;
            isPlaying = true;
            ResetEditorClock();
            ApplyAnimationSample(normalizedTime);
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void StopAndReset()
        {
            isPlaying = false;
            direction = 1;
            normalizedTime = 0f;
            ApplyAnimationSample(normalizedTime);
        }

        public void SetProgress(float progress)
        {
            isPlaying = false;
            direction = 1;
            normalizedTime = Mathf.Clamp01(progress);
            ApplyAnimationSample(normalizedTime);
        }

        private void EnsureReferences()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (glowShader == null)
            {
                glowShader = Shader.Find(DefaultShaderName);
            }
        }

        private void EnsureRuntimeMaterial()
        {
            if (targetGraphic == null)
            {
                return;
            }

            Material sourceMaterial = materialTemplate;
            Shader sourceShader = sourceMaterial != null ? sourceMaterial.shader : glowShader;

            if (sourceMaterial == null && sourceShader == null)
            {
                return;
            }

            bool needsRebuild = runtimeMaterial == null
                || runtimeSourceMaterial != sourceMaterial
                || runtimeSourceShader != sourceShader;

            if (needsRebuild)
            {
                ReleaseRuntimeMaterial();

                runtimeMaterial = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : new Material(sourceShader);

                runtimeMaterial.name = sourceMaterial != null
                    ? sourceMaterial.name + " (Runtime)"
                    : DefaultShaderName + " (Runtime)";
                runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
                runtimeSourceMaterial = sourceMaterial;
                runtimeSourceShader = sourceShader;
            }

            if (targetGraphic.material != runtimeMaterial)
            {
                originalMaterial = targetGraphic.material;
                targetGraphic.material = runtimeMaterial;
                targetGraphic.SetMaterialDirty();
            }
        }

        private void ReleaseRuntimeMaterial()
        {
            if (targetGraphic != null && runtimeMaterial != null && targetGraphic.material == runtimeMaterial)
            {
                targetGraphic.material = originalMaterial;
                targetGraphic.SetMaterialDirty();
            }

            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeMaterial);
                }
                else
                {
                    DestroyImmediate(runtimeMaterial);
                }
            }

            runtimeMaterial = null;
            runtimeSourceMaterial = null;
            runtimeSourceShader = null;
        }

        private void AdvanceAnimation(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            normalizedTime += deltaTime / duration * direction;

            if (!pingPong)
            {
                if (normalizedTime >= 1f)
                {
                    if (loop)
                    {
                        normalizedTime = 0f;
                    }
                    else
                    {
                        normalizedTime = 1f;
                        isPlaying = false;
                    }
                }

                return;
            }

            if (normalizedTime >= 1f)
            {
                normalizedTime = 1f;
                direction = -1;
            }
            else if (normalizedTime <= 0f)
            {
                normalizedTime = 0f;

                if (loop)
                {
                    direction = 1;
                }
                else
                {
                    isPlaying = false;
                }
            }
        }

        private void ApplyAnimationSample(float t)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            float curveT = intensityCurve != null && intensityCurve.length > 0
                ? intensityCurve.Evaluate(Mathf.Clamp01(t))
                : Mathf.Clamp01(t);

            float glowIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, curveT);
            Color glowColor = animateGlowColor
                ? Color.Lerp(minGlowColor, maxGlowColor, curveT)
                : maxGlowColor;

            if (runtimeMaterial.HasProperty(BaseBrightnessId))
            {
                runtimeMaterial.SetFloat(BaseBrightnessId, baseBrightness);
            }

            if (runtimeMaterial.HasProperty(GlowIntensityId))
            {
                runtimeMaterial.SetFloat(GlowIntensityId, glowIntensity);
            }

            if (runtimeMaterial.HasProperty(GlowColorId))
            {
                runtimeMaterial.SetColor(GlowColorId, glowColor);
            }
        }

        private float GetDeltaTime()
        {
            if (Application.isPlaying)
            {
                return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            }

#if UNITY_EDITOR
            double now = EditorApplication.timeSinceStartup;
            if (lastEditorTime <= 0d)
            {
                lastEditorTime = now;
                return 0f;
            }

            float delta = (float)(now - lastEditorTime);
            lastEditorTime = now;
            return delta;
#else
            return 0f;
#endif
        }

        private void ResetEditorClock()
        {
#if UNITY_EDITOR
            lastEditorTime = 0d;
#endif
        }
    }
}
