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

        /// <summary>是否启用目录筛选（仅收集指定目录内引用的 Shader 变体）</summary>
        [SerializeField]
        private bool enableDirectoryFilter = false;

        /// <summary>参与收集的目录（工程相对路径，如 Assets/_Res）</summary>
        [SerializeField]
        private List<string> collectFolders = new List<string>();

        /// <summary>目录筛选未配置目录时使用的默认收集目录</summary>
        public const string DefaultCollectFolder = "Assets/_Res";
        
        private void OnEnable()
        {
            // 目录筛选：目录列表为空时补上默认目录
            if (collectFolders == null)
                collectFolders = new List<string>();
            if (collectFolders.Count == 0)
                collectFolders.Add(DefaultCollectFolder);

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
        
        /// <summary>是否启用目录筛选（仅收集指定目录内引用的 Shader 变体）</summary>
        public bool EnableDirectoryFilter
        {
            get => enableDirectoryFilter;
            set => enableDirectoryFilter = value;
        }

        /// <summary>参与收集的目录列表（工程相对路径）</summary>
        public List<string> CollectFolders
        {
            get
            {
                if (collectFolders == null)
                    collectFolders = new List<string>();
                return collectFolders;
            }
        }

        /// <summary>
        /// 获取当前生效的收集目录；未启用目录筛选时返回 null（表示扫描整个工程）。
        /// </summary>
        public string[] GetActiveCollectFolders()
        {
            if (!enableDirectoryFilter)
                return null;

            var folders = new List<string>();
            foreach (var folder in CollectFolders)
            {
                var normalized = NormalizeFolder(folder);
                if (!string.IsNullOrEmpty(normalized) && !folders.Contains(normalized))
                    folders.Add(normalized);
            }

            // 没有配置任何目录时回退到默认目录
            if (folders.Count == 0)
                folders.Add(DefaultCollectFolder);

            return folders.ToArray();
        }

        /// <summary>把目录路径规整为 Assets/xxx 形式（去除首尾空白与多余斜杠）。</summary>
        public static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return string.Empty;

            var normalized = folder.Replace('\\', '/').Trim();
            while (normalized.EndsWith("/"))
                normalized = normalized.Substring(0, normalized.Length - 1);

            return normalized;
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