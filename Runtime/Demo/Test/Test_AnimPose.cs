using System.Collections.Generic;
using UniVRM10;
using UnityEngine;
using VRM;

namespace XiaoCao
{
    /// <summary>
    /// 模型使用的 VRM 版本。
    /// </summary>
    public enum EVrmVersion
    {
        /// <summary>没找到 VRM 组件，判断不出版本</summary>
        Unknown,

        /// <summary>VRM0，模型上带 VRMBlendShapeProxy / VRMMeta 等 VRM0 组件</summary>
        Vrm0,

        /// <summary>VRM1，模型文件是 .vrm 结尾，模型上带 Vrm10Instance</summary>
        Vrm1,
    }

    public class Test_AnimPose : MonoBehaviour
    {
        [Range(0, 1)]
        [NaughtyAttributes.OnValueChanged("SetValue")]
        public float time;

        [NaughtyAttributes.InfoBox("VRM1 的模型文件是 .vrm 结尾，导入后模型上会带 Vrm10Instance；VRM0 的模型带 VRMBlendShapeProxy / VRMMeta。")]
        public GameObject model;

        public AnimationClip clip;

        [NaughtyAttributes.ReadOnly]
        [Tooltip("最近一次判断出的模型版本：VRM1 的模型文件是 .vrm 结尾")]
        public EVrmVersion modelVersion;

        [NaughtyAttributes.Button("ResPlayer")]
        public void ResetPlayer()
        {
            if (model != null && clip != null)
            {
                clip.SampleAnimation(model, 0);
                model.transform.localPosition = Vector3.zero;
            }
        }

        public void SetValue()
        {

            if (model != null && clip != null)
            {
                clip.SampleAnimation(model, time * clip.length);
            }

        }

        /// <summary>
        /// 判断模型是 VRM0 还是 VRM1：VRM1 的模型文件是 .vrm 结尾，模型上会带 Vrm10Instance；
        /// VRM0 的模型则带 VRMBlendShapeProxy / VRMMeta。结果写进 modelVersion 并打印提示。
        /// </summary>
        [NaughtyAttributes.Button("判断模型版本")]
        public EVrmVersion CheckModelVersion()
        {
            if (model == null)
            {
                modelVersion = EVrmVersion.Unknown;
                Debug.LogWarning("未绑定 model，无法判断模型版本。", this);
                return modelVersion;
            }

            //VRM1 的模型文件是 .vrm 结尾，导入后模型上一定带 Vrm10Instance
            if (model.GetComponentInChildren<Vrm10Instance>(true) != null)
            {
                modelVersion = EVrmVersion.Vrm1;
                Debug.Log($"模型 [{model.name}] 是 VRM1，模型文件是 .vrm 结尾。", this);
            }
            else if (model.GetComponentInChildren<VRMBlendShapeProxy>(true) != null
                || model.GetComponentInChildren<VRMMeta>(true) != null)
            {
                modelVersion = EVrmVersion.Vrm0;
                Debug.Log($"模型 [{model.name}] 是 VRM0。", this);
            }
            else
            {
                modelVersion = EVrmVersion.Unknown;
                Debug.LogWarning($"模型 [{model.name}] 上没找到 VRM 组件；VRM1 的模型文件是 .vrm 结尾。", this);
            }

            return modelVersion;
        }
    }
}
