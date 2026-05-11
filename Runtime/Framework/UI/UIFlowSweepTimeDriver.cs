using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 为 Flow Sweep UI 材质写入不受 Time.timeScale 影响的时间值。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIFlowSweepTimeDriver : MonoBehaviour
    {
        private const string DefaultShaderName = "UI/Flow Sweep"; 

        private static readonly int ExternalTimeId = Shader.PropertyToID("_ExternalTime");
        private static readonly int IgnoreTimeScaleId = Shader.PropertyToID("_IgnoreTimeScale");

        [Header("Target")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private Shader flowSweepShader;

        private Material originalMaterial;
        private Material runtimeMaterial;
        private Material runtimeSourceMaterial;
        private Shader runtimeSourceShader;

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
            flowSweepShader = Shader.Find(DefaultShaderName);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureReferences();
            CaptureOriginalMaterial();
            EnsureRuntimeMaterial();
            SyncShaderTime();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureRuntimeMaterial();
            SyncShaderTime();
        }

        private void OnDisable()
        {
            ReleaseRuntimeMaterial();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeMaterial();
        }

        /// <summary>
        /// 确保组件引用和默认 Shader 已就绪。
        /// </summary>
        private void EnsureReferences()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (flowSweepShader == null)
            {
                flowSweepShader = Shader.Find(DefaultShaderName);
            }
        }

        /// <summary>
        /// 为目标 Graphic 准备独立的运行时材质，避免改到共享材质资源。
        /// </summary>
        private void EnsureRuntimeMaterial()
        {
            if (targetGraphic == null)
            {
                return;
            }

            CaptureOriginalMaterial();
            Material sourceMaterial = ResolveSourceMaterial();
            Shader sourceShader = sourceMaterial != null ? sourceMaterial.shader : flowSweepShader;
            if (sourceMaterial != null && !sourceMaterial.HasProperty(ExternalTimeId))
            {
                sourceMaterial = null;
                sourceShader = flowSweepShader;
            }

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
                if (runtimeMaterial.HasProperty(IgnoreTimeScaleId))
                {
                    runtimeMaterial.SetFloat(IgnoreTimeScaleId, 1f);
                }

                runtimeSourceMaterial = sourceMaterial;
                runtimeSourceShader = sourceShader;
            }

            if (targetGraphic.material != runtimeMaterial)
            {
                targetGraphic.material = runtimeMaterial;
                targetGraphic.SetMaterialDirty();
            }
        }

        /// <summary>
        /// 记录启动时的原始材质，作为运行时克隆源和恢复目标。
        /// </summary>
        private void CaptureOriginalMaterial()
        {
            if (targetGraphic == null)
            {
                return;
            }

            if (targetGraphic.material == runtimeMaterial)
            {
                return;
            }

            originalMaterial = targetGraphic.material;
        }

        /// <summary>
        /// 释放运行时材质，并恢复 Graphic 原始材质。
        /// </summary>
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

        /// <summary>
        /// 把当前的非缩放时间写入 Flow Sweep 材质。
        /// </summary>
        private void SyncShaderTime()
        {
            if (runtimeMaterial == null || !runtimeMaterial.HasProperty(ExternalTimeId))
            {
                return;
            }

            runtimeMaterial.SetFloat(ExternalTimeId, GetCurrentTimeValue());
        }

        /// <summary>
        /// 优先使用显式模板，否则复制目标当前材质。
        /// </summary>
        private Material ResolveSourceMaterial()
        {
            if (materialTemplate != null)
            {
                return materialTemplate;
            }

            return originalMaterial != null && originalMaterial.HasProperty(ExternalTimeId)
                ? originalMaterial
                : null;
        }

        /// <summary>
        /// 运行时使用 Time.unscaledTime，编辑器预览使用编辑器时钟。
        /// </summary>
        private float GetCurrentTimeValue()
        {
            return Time.unscaledTime;
        }
    }
}
