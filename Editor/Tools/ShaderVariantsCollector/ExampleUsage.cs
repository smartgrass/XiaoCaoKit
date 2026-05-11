using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using ShaderVariantsCollector;

namespace ShaderVariantsCollector.Examples
{
    /// <summary>
    /// 示例脚本，展示如何使用ShaderVariantsCollector插件的API
    /// </summary>
    public static class ExampleUsage
    {
        // [MenuItem("Tools/Shader Variants Collector/Examples/Open Collector Window")]
        public static void OpenCollectorWindow()
        {
            ShaderVariantsCollectorWindow.ShowWindow();
        }

        // [MenuItem("Tools/Shader Variants Collector/Examples/Collect Variants from Project")]
        public static void CollectVariantsFromProject()
        {
            var variants = ShaderVariantCollector.CollectVariantsFromProject();
            Debug.Log($"Found {variants.Count} variants in project");
            
            foreach (var variant in variants)
            {
                Debug.Log($"Shader: {variant.GetShaderName()}, Keywords: {variant.GetKeywordsString()}");
            }
        }

        // [MenuItem("Tools/Shader Variants Collector/Examples/Create Sample Collection")]
        public static void CreateSampleCollection()
        {
            // 创建示例ShaderVariantCollection
            var collection = new ShaderVariantCollection();
            
            // 添加一些示例变体
            var standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                collection.Add(new ShaderVariantCollection.ShaderVariant(standardShader, PassType.Normal));
                collection.Add(new ShaderVariantCollection.ShaderVariant(standardShader, PassType.ShadowCaster));
            }
            
            // 保存到Assets文件夹
            var path = "Assets/SampleShaderVariantCollection.asset";
            AssetDatabase.CreateAsset(collection, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Created sample collection at: {path}");
            
            // 选中新创建的资源
            Selection.activeObject = collection;
            EditorGUIUtility.PingObject(collection);
        }

        // [MenuItem("Tools/Shader Variants Collector/Examples/Find Variant References")]
        public static void FindVariantReferences()
        {
            // 示例：查找特定变体的引用
            var standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                var variant = new ShaderVariantData(standardShader, new System.Collections.Generic.List<string>());
                var references = ShaderVariantCollector.FindVariantReferences(variant);
                
                Debug.Log($"Found {references.Count} references for Standard shader");
                foreach (var reference in references)
                {
                    Debug.Log($"Reference: {reference.name}");
                }
            }
        }

        // [MenuItem("Tools/Shader Variants Collector/Examples/Warmup Collection")]
        public static void WarmupCollection()
        {
            // 示例：预热ShaderVariantCollection
            var collection = Selection.activeObject as ShaderVariantCollection;
            if (collection != null)
            {
                ShaderVariantCollector.WarmupCollection(collection);
                Debug.Log("Collection warmed up successfully");
            }
            else
            {
                Debug.LogWarning("Please select a ShaderVariantCollection first");
            }
        }

        // [MenuItem("Tools/Shader Variants Collector/Examples/Check Variant in Collection")]
        public static void CheckVariantInCollection()
        {
            // 示例：检查变体是否在集合中
            var collection = Selection.activeObject as ShaderVariantCollection;
            if (collection != null)
            {
                var standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    var variant = new ShaderVariantData(standardShader, new System.Collections.Generic.List<string>());
                    bool isInCollection = ShaderVariantCollector.IsVariantInCollection(collection, variant);
                    
                    Debug.Log($"Variant is in collection: {isInCollection}");
                }
            }
            else
            {
                Debug.LogWarning("Please select a ShaderVariantCollection first");
            }
        }
    }
} 