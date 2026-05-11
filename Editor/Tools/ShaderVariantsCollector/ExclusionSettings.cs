using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShaderVariantsCollector
{
    [CreateAssetMenu(fileName = "ShaderVariantsExclusionSettings", menuName = "Shader Variants Collector/Exclusion Settings")]
    public class ExclusionSettings : ScriptableObject
    {
        [SerializeField]
        public List<string> excludedShaderNames = new List<string>();
        
        [SerializeField]
        public List<string> excludedKeywords = new List<string>();
        
        private void OnEnable()
        {
            // 首次创建时设置默认值
            if (excludedShaderNames.Count == 0 && excludedKeywords.Count == 0)
            {
                InitializeDefaults();
            }
        }
        
        private void InitializeDefaults()
        {
            // 默认剔除一些常见的不需要的shader
            excludedShaderNames.Add("Hidden/");
            excludedShaderNames.Add("Legacy Shaders/");
            
            // 默认剔除一些常见的调试关键字
            excludedKeywords.Add("_DEBUG");
            excludedKeywords.Add("EDITOR_VISUALIZATION");
        }
        
        public bool IsShaderExcluded(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName))
                return false;
                
            foreach (var excludedName in excludedShaderNames)
            {
                if (string.IsNullOrEmpty(excludedName))
                    continue;
                    
                // 支持部分匹配（包含关系）
                if (shaderName.Contains(excludedName))
                    return true;
            }
            
            return false;
        }
        
        public bool IsVariantExcluded(List<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return false;
                
            foreach (var keyword in keywords)
            {
                if (excludedKeywords.Contains(keyword))
                    return true;
            }
            
            return false;
        }
        
        public void AddExcludedShader(string shaderName)
        {
            if (!string.IsNullOrEmpty(shaderName) && !excludedShaderNames.Contains(shaderName))
            {
                excludedShaderNames.Add(shaderName);
            }
        }
        
        public void RemoveExcludedShader(string shaderName)
        {
            excludedShaderNames.Remove(shaderName);
        }
        
        public void AddExcludedKeyword(string keyword)
        {
            if (!string.IsNullOrEmpty(keyword) && !excludedKeywords.Contains(keyword))
            {
                excludedKeywords.Add(keyword);
            }
        }
        
        public void RemoveExcludedKeyword(string keyword)
        {
            excludedKeywords.Remove(keyword);
        }

#if UNITY_EDITOR
        private static ExclusionSettings _instance;
        private static readonly string DefaultSettingsPath = "Assets/ShaderVariantsCollector/ShaderVariantsExclusionSettings.asset";
        
        public static ExclusionSettings GetOrCreateSettings()
        {
            if (_instance == null)
            {
                _instance = AssetDatabase.LoadAssetAtPath<ExclusionSettings>(DefaultSettingsPath);
                
                if (_instance == null)
                {
                    _instance = CreateInstance<ExclusionSettings>();
                    
                    // 确保目录存在
                    string directory = System.IO.Path.GetDirectoryName(DefaultSettingsPath);
                    if (!AssetDatabase.IsValidFolder(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                        AssetDatabase.Refresh();
                    }
                    
                    AssetDatabase.CreateAsset(_instance, DefaultSettingsPath);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log($"Created new ShaderVariantsExclusionSettings at {DefaultSettingsPath}");
                }
            }
            
            return _instance;
        }
        
        public void SaveSettings()
        {
            if (_instance != null)
            {
                EditorUtility.SetDirty(_instance);
                AssetDatabase.SaveAssets();
            }
        }
        
        // 重置静态实例，用于重新加载
        public static void ResetInstance()
        {
            _instance = null;
        }
#endif
    }
} 