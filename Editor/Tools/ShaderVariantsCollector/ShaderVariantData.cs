using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ShaderVariantsCollector
{
    [System.Serializable]
    public class ShaderVariantData
    {
        public Shader shader;
        public UnityEngine.Rendering.PassType passType = UnityEngine.Rendering.PassType.Normal;
        public List<string> keywords = new List<string>();
        public Material sourceMaterial;
        public List<GameObject> referencedPrefabs = new List<GameObject>();
        public bool isNew = false;
        public bool isConfirmed = false;
        public bool isIgnored = false;
        public bool isCollected = false; // 标识是否为从项目中收集到的变体
        public bool isBuiltinShader = false; // 标识是否为内置Shader

        public ShaderVariantData()
        {
        }

        public ShaderVariantData(Shader shader, List<string> keywords, Material sourceMaterial = null)
        {
            this.shader = shader;
            this.keywords = new List<string>(keywords);
            this.sourceMaterial = sourceMaterial;
        }

        public ShaderVariantData(Shader shader, UnityEngine.Rendering.PassType passType, List<string> keywords, Material sourceMaterial = null)
        {
            this.shader = shader;
            this.passType = passType;
            this.keywords = new List<string>(keywords);
            this.sourceMaterial = sourceMaterial;
        }

        private static IEnumerable<string> NormalizeKeywords(IEnumerable<string> input)
        {
            if (input == null) yield break;
            foreach (var k in input.Where(k => !string.IsNullOrEmpty(k)).Distinct().OrderBy(k => k))
            {
                yield return k;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ShaderVariantData other)
            {
                if (shader != other.shader)
                    return false;

                if (passType != other.passType)
                    return false;

                var a = NormalizeKeywords(keywords).ToList();
                var b = NormalizeKeywords(other.keywords).ToList();
                if (a.Count != b.Count)
                    return false;

                for (int i = 0; i < a.Count; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }

                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = shader != null ? shader.GetHashCode() : 0;
                hash = (hash * 397) ^ (int)passType;
                foreach (var keyword in NormalizeKeywords(keywords))
                {
                    hash = (hash * 397) ^ keyword.GetHashCode();
                }
                return hash;
            }
        }

        public string GetKeywordsString()
        {
            var normalized = NormalizeKeywords(keywords).ToList();
            if (normalized.Count == 0)
                return "No Keywords";
            return string.Join(", ", normalized);
        }

        public string GetShaderName()
        {
            return shader != null ? shader.name : "Unknown Shader";
        }

        public string GetMaterialName()
        {
            return sourceMaterial != null ? sourceMaterial.name : "Unknown Material";
        }
    }
} 