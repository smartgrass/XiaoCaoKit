using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShaderVariantsCollector
{
    public static class ShaderVariantCollector
    {
        // 将SerializedProperty转化为ShaderVariant
        private static ShaderVariantCollection.ShaderVariant PropToVariantObject(Shader shader, SerializedProperty variantInfo)
        {
            PassType passType = (PassType)variantInfo.FindPropertyRelative("passType").intValue;
            string keywords = variantInfo.FindPropertyRelative("keywords").stringValue;
            string[] keywordSet = keywords.Split(' ');
            keywordSet = (keywordSet.Length == 1 && keywordSet[0] == "") ? new string[0] : keywordSet;

            ShaderVariantCollection.ShaderVariant newVariant = new ShaderVariantCollection.ShaderVariant()
            {
                shader = shader,
                keywords = keywordSet,
                passType = passType
            };

            return newVariant;
        }
        
        public static List<ShaderVariantData> CollectVariantsFromCollection(ShaderVariantCollection collection)
        {
            var variants = new List<ShaderVariantData>();
            if (collection == null) return variants;

            var so = new SerializedObject(collection);
            var shadersProp = so.FindProperty("m_Shaders");
            if (shadersProp != null && shadersProp.isArray)
            {
                for (int i = 0; i < shadersProp.arraySize; i++)
                {
                    var shaderEntry = shadersProp.GetArrayElementAtIndex(i);
                    var shaderProp = shaderEntry.FindPropertyRelative("first");
                    
                    Shader shader = shaderProp != null ? shaderProp.objectReferenceValue as Shader : null;
                    if (shader == null) continue;
                    
                    var valueProp = shaderEntry.FindPropertyRelative("second");
                    SerializedProperty variantsProp = valueProp.FindPropertyRelative("variants");

                    for (int j = 0; j < variantsProp.arraySize; j++)
                    {
                        var variantProp = variantsProp.GetArrayElementAtIndex(j);
                        ShaderVariantCollection.ShaderVariant variant = PropToVariantObject(shader, variantProp);
                        variants.Add(new ShaderVariantData(shader, variant.passType, variant.keywords.ToList()));
                    }
                }
            }
            // 使用稳定的Equals/GetHashCode去重
            return SortVariantsByName(variants.Distinct());
        }

        private static List<ShaderVariantData> GetShaderVariants(ShaderVariantCollection collection, Shader shader, int shaderIndex)
        {
            var variants = new List<ShaderVariantData>();
            
            try
            {
                var type = typeof(ShaderVariantCollection);
                var keywordsField = type.GetField("m_Keywords", BindingFlags.NonPublic | BindingFlags.Instance);
                var passTypesField = type.GetField("m_PassTypes", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (keywordsField != null && passTypesField != null)
                {
                    var keywords = keywordsField.GetValue(collection) as string[][];
                    var passTypes = passTypesField.GetValue(collection) as PassType[];
                    
                    if (keywords != null && passTypes != null)
                    {
                        for (int i = 0; i < keywords.Length; i++)
                        {
                            if (i < passTypes.Length)
                            {
                                var variant = new ShaderVariantData
                                {
                                    shader = shader,
                                    keywords = new List<string>(keywords[i] ?? new string[0])
                                };
                                variants.Add(variant);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting variants for shader {shader.name}: {e.Message}");
            }
            
            return variants;
        }

        public static List<ShaderVariantData> CollectVariantsFromProject(ExclusionSettings exclusionSettings = null)
        {
            var variants = new List<ShaderVariantData>();
            
            // 收集所有Material中的变体
            var materialVariants = CollectVariantsFromMaterials(exclusionSettings);
            variants.AddRange(materialVariants);
            
            // 收集所有Prefab中的变体
            var prefabVariants = CollectVariantsFromPrefabs(exclusionSettings);
            variants.AddRange(prefabVariants);
            
            // 去重
            return SortVariantsByName(variants.Distinct());
        }

        public static List<ShaderVariantData> CollectVariantsForShader(Shader targetShader, ExclusionSettings exclusionSettings = null)
        {
            var variants = new List<ShaderVariantData>();
            if (targetShader == null) return variants;

            // 扫描材质
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null) continue;
                if (material.shader != targetShader) continue;

                // 排除规则：Shader层
                if (exclusionSettings != null && exclusionSettings.IsShaderExcluded(material.shader.name))
                    continue;

                var passType = GetRealPassTypeFromMaterial(material);
                var variant = new ShaderVariantData
                {
                    shader = material.shader,
                    keywords = new List<string>(material.shaderKeywords ?? new string[0]),
                    sourceMaterial = material,
                    isCollected = true,
                    isBuiltinShader = IsBuiltinShader(material.shader),
                    passType = passType
                };

                // 排除规则：关键字层
                if (exclusionSettings != null && exclusionSettings.IsVariantExcluded(variant.keywords))
                    continue;

                variants.Add(variant);
            }

            // 扫描Prefab（引用此材质）
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var mat = renderer.sharedMaterial;
                    if (mat == null || mat.shader == null) continue;
                    if (mat.shader != targetShader) continue;

                    // 排除规则：Shader层
                    if (exclusionSettings != null && exclusionSettings.IsShaderExcluded(mat.shader.name))
                        continue;

                    var passType = GetRealPassTypeFromMaterial(mat);
                    var variant = new ShaderVariantData
                    {
                        shader = mat.shader,
                        keywords = new List<string>(mat.shaderKeywords ?? new string[0]),
                        sourceMaterial = mat,
                        referencedPrefabs = new List<GameObject> { prefab },
                        isCollected = true,
                        isBuiltinShader = IsBuiltinShader(mat.shader),
                        passType = passType
                    };

                    // 排除规则：关键字层
                    if (exclusionSettings != null && exclusionSettings.IsVariantExcluded(variant.keywords))
                        continue;

                    variants.Add(variant);
                }
            }

            return SortVariantsByName(variants.Distinct());
        }

        /// <summary>
        /// 按Shader名称、PassType和关键字对变体进行稳定排序。
        /// </summary>
        private static List<ShaderVariantData> SortVariantsByName(IEnumerable<ShaderVariantData> variants)
        {
            if (variants == null)
            {
                return new List<ShaderVariantData>();
            }

            return variants
                .OrderBy(v => v?.shader != null ? v.shader.name : string.Empty, StringComparer.Ordinal)
                .ThenBy(v => v != null ? v.passType.ToString() : string.Empty, StringComparer.Ordinal)
                .ThenBy(v => v != null ? v.GetKeywordsString() : string.Empty, StringComparer.Ordinal)
                .ToList();
        }

        private static List<ShaderVariantData> CollectVariantsFromMaterials(ExclusionSettings exclusionSettings = null)
        {
            var variants = new List<ShaderVariantData>();
            
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (material != null && material.shader != null)
                {
                    // 检查Shader是否被剔除
                    if (exclusionSettings != null && exclusionSettings.IsShaderExcluded(material.shader.name))
                    {
                        continue;
                    }
                    
                    // 从Material获取真实的PassType
                    var passType = GetRealPassTypeFromMaterial(material);
                    
                    var variant = new ShaderVariantData
                    {
                        shader = material.shader,
                        keywords = new List<string>(material.shaderKeywords ?? new string[0]),
                        sourceMaterial = material,
                        isCollected = true, // 标识为收集到的变体
                        isBuiltinShader = IsBuiltinShader(material.shader),
                        passType = passType
                    };
                    
                    // 检查变体关键字是否被剔除
                    if (exclusionSettings != null && exclusionSettings.IsVariantExcluded(variant.keywords))
                    {
                        continue;
                    }
                    
                    variants.Add(variant);
                }
            }
            
            return variants;
        }

        private static List<ShaderVariantData> CollectVariantsFromPrefabs(ExclusionSettings exclusionSettings = null)
        {
            var variants = new List<ShaderVariantData>();
            
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        var mat = renderer.sharedMaterial;
                        if (mat != null && mat.shader != null)
                        {
                            // 检查Shader是否被剔除
                            if (exclusionSettings != null && exclusionSettings.IsShaderExcluded(mat.shader.name))
                            {
                                continue;
                            }

                            // 从Material获取真实的PassType
                            var passType = GetRealPassTypeFromMaterial(mat);
                            
                            var variant = new ShaderVariantData
                            {
                                shader = mat.shader,
                                keywords = new List<string>(mat.shaderKeywords ?? new string[0]),
                                sourceMaterial = mat,
                                referencedPrefabs = new List<GameObject> { prefab },
                                isCollected = true, // 标识为收集到的变体
                                isBuiltinShader = IsBuiltinShader(mat.shader),
                                passType = passType
                            };
                            
                            // 检查变体关键字是否被剔除
                            if (exclusionSettings != null && exclusionSettings.IsVariantExcluded(variant.keywords))
                            {
                                continue;
                            }
                            
                            variants.Add(variant);
                        }
                    }
                }
            }
            
            return variants;
        }

        private static PassType GetRealPassTypeFromMaterial(Material material)
        {
            if (material == null || material.shader == null)
                return PassType.Normal;

            try
            {
                // 对于内置Shader，使用特殊处理
                if (IsBuiltinShader(material.shader))
                {
                    return GetBuiltinShaderPassType(material.shader);
                }

                // 直接从Material获取LightMode标签，这样可以获取当前使用的SubShader的Pass信息
                string lightMode = material.GetTag("LightMode", false, "");
                
                // 如果没有找到LightMode标签，尝试搜索fallbacks
                if (string.IsNullOrEmpty(lightMode))
                {
                    lightMode = material.GetTag("LightMode", true, "");
                }
                
                // 首先检查shader名称是否明显是SRP的
                if (IsPotentialSRPShader(material.shader))
                {
                    PassType heuristicPassType = GetPassTypeByHeuristics(material.shader);
                    Debug.Log($"PassType inference for shader '{material.shader.name}': Detected SRP shader -> LightMode='{lightMode}' -> Using heuristics -> {heuristicPassType}");
                    return heuristicPassType;
                }
                
                // 对于非SRP shader，使用LightMode映射
                PassType passType = MapLightModeToPassType(lightMode);
                
                // 如果LightMode为空且不是SRP shader，使用启发式方法
                if (string.IsNullOrEmpty(lightMode))
                {
                    PassType heuristicPassType = GetPassTypeByHeuristics(material.shader);
                    Debug.Log($"PassType inference for shader '{material.shader.name}': No LightMode -> Using heuristics -> {heuristicPassType}");
                    passType = heuristicPassType;
                }
                else
                {
                    Debug.Log($"PassType inference for shader '{material.shader.name}': LightMode='{lightMode}' -> {passType}");
                }
                
                return passType;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to get PassType from material {material.name}: {e.Message}");
                PassType fallbackPassType = GetPassTypeByHeuristics(material.shader);
                Debug.Log($"PassType fallback for shader '{material.shader.name}': Using heuristics -> {fallbackPassType}");
                return fallbackPassType;
            }
        }

        private static bool IsBuiltinShader(Shader shader)
        {
            if (shader == null) return false;
            
            // 内置Shader的AssetPath通常为空或者特殊路径
            string assetPath = AssetDatabase.GetAssetPath(shader);
            return string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("Resources/");
        }

        private static PassType GetBuiltinShaderPassType(Shader shader)
        {
            // 根据内置Shader的名称返回合适的PassType
            string shaderName = shader.name.ToLower();
            
            if (shaderName.Contains("ui/") || shaderName.Contains("sprites/"))
            {
                return PassType.Normal; // UI和Sprites通常使用Normal pass
            }
            
            if (shaderName.Contains("skybox"))
            {
                return PassType.Normal;
            }
            
            // 其他内置Shader默认使用Normal
            return PassType.Normal;
        }

        private static PassType MapLightModeToPassType(string lightMode)
        {
            if (string.IsNullOrEmpty(lightMode))
                return PassType.Normal;
            
            switch (lightMode.ToLower())
            {
                case "forwardbase":
                case "forward":
                    return PassType.ForwardBase;
                case "forwardadd":
                    return PassType.ForwardAdd;
                case "deferred":
                    return PassType.Deferred;
                case "shadowcaster":
                    return PassType.ShadowCaster;
                case "meta":
                    return PassType.Meta;
                case "motionvectors":
                    return PassType.MotionVectors;
                // SRP/URP 相关的 LightMode
                case "srpdefaultunlit":
                case "unlit":
                case "universalforward":
                case "universalforwardonly":
                case "urpforward":
                case "urpforwardonly":
                case "lightweightforward":
                case "lwrpforward":
                case "scriptablerenderpassforward":
                case "srplightweightforward":
                case "universalforwardbase":
                case "universaldeferred":
                case "universal2d":
                case "hdrpforward":
                case "hdrpdeferred":
                case "hdrpforwardonly":
                    return PassType.ScriptableRenderPipeline;
                default:
                    return PassType.Normal;
            }
        }

        private static bool IsPotentialSRPShader(Shader shader)
        {
            if (shader == null) return false;
            string name = shader.name.ToLower();
            
            return name.Contains("urp/") || name.Contains("universal") || 
                   name.Contains("lwrp/") || name.Contains("lightweight") ||
                   name.Contains("srp/") || name.Contains("scriptable") ||
                   name.Contains("render pipeline") || name.Contains("hdrp/");
        }

        private static PassType GetPassTypeByHeuristics(Shader shader)
        {
            if (shader == null) return PassType.Normal;
            string name = shader.name.ToLower();
            
            // 检查SRP/URP相关的shader名称
            if (IsPotentialSRPShader(shader))
            {
                return PassType.ScriptableRenderPipeline;
            }
            
            // 传统渲染管线的判断
            if (name.Contains("particles")) return PassType.ForwardBase;
            if (name.Contains("unlit")) return PassType.ForwardBase;
            if (name.Contains("lit")) return PassType.ForwardBase;
            
            return PassType.Normal;
        }

        public static List<GameObject> FindVariantReferences(ShaderVariantData variant)
        {
            var references = new List<GameObject>();
            try
            {
                if (variant == null || variant.shader == null)
                {
                    return references;
                }

                // 规范化目标关键字集合
                var targetKeywords = new HashSet<string>(
                    (variant.keywords ?? new List<string>())
                        .Where(k => !string.IsNullOrEmpty(k))
                        .Distinct()
                );

                // 扫描所有Prefab，匹配其Renderer使用的材质
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in prefabGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    try
                    {
                        // 包含全部Renderer类型
                        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                        bool matched = false;
                        foreach (var renderer in renderers)
                        {
                            var mats = renderer.sharedMaterials;
                            if (mats == null) continue;

                            foreach (var mat in mats)
                            {
                                if (mat == null || mat.shader == null) continue;
                                if (mat.shader != variant.shader) continue;

                                var matKeywords = new HashSet<string>((mat.shaderKeywords ?? new string[0])
                                    .Where(k => !string.IsNullOrEmpty(k))
                                    .Distinct());

                                // 规则：variant关键字应为材质关键字的子集（允许材质包含更多关键字）
                                if (targetKeywords.IsSubsetOf(matKeywords))
                                {
                                    references.Add(prefab);
                                    matched = true;
                                    break;
                                }
                            }

                            if (matched) break;
                        }
                    }
                    catch
                    {
                        // 忽略单个Prefab处理中的异常，继续扫描
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FindVariantReferences failed: {e.Message}");
            }
            
            return references.Distinct().ToList();
        }

        public static List<Material> FindMaterialsForVariant(ShaderVariantData variant)
        {
            var materials = new List<Material>();
            try
            {
                if (variant == null || variant.shader == null)
                {
                    return materials;
                }

                var targetKeywords = new HashSet<string>(
                    (variant.keywords ?? new List<string>())
                        .Where(k => !string.IsNullOrEmpty(k))
                        .Distinct()
                );

                // 扫描所有材质资产
                var matGuids = AssetDatabase.FindAssets("t:Material");
                foreach (var guid in matGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null || mat.shader == null) continue;
                    if (mat.shader != variant.shader) continue;

                    var matKeywords = new HashSet<string>((mat.shaderKeywords ?? new string[0])
                        .Where(k => !string.IsNullOrEmpty(k))
                        .Distinct());

                    if (targetKeywords.IsSubsetOf(matKeywords))
                    {
                        materials.Add(mat);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FindMaterialsForVariant failed: {e.Message}");
            }

            return materials.Distinct().ToList();
        }

        public static void WarmupCollection(ShaderVariantCollection collection)
        {
            if (collection != null)
            {
                collection.WarmUp();
            }
        }

        public static bool IsVariantInCollection(ShaderVariantCollection collection, ShaderVariantData variant)
        {
            if (collection == null || variant.shader == null)
                return false;
            
            try
            {
                // 使用对象初始化语法检查变体是否在集合中
                var shaderVariant = new ShaderVariantCollection.ShaderVariant
                {
                    shader = variant.shader,
                    keywords = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray(),
                    passType = variant.passType
                };
                
                return collection.Contains(shaderVariant);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error checking variant in collection: {e.Message}");
                return false;
            }
        }

        // 添加调试方法
        // [MenuItem("Tools/Shader Variants Collector/Debug/Analyze Collection Structure")]
        public static void AnalyzeCollectionStructure()
        {
            var collection = Selection.activeObject as ShaderVariantCollection;
            if (collection == null)
            {
                Debug.LogWarning("Please select a ShaderVariantCollection first");
                return;
            }
            
            Debug.Log("=== ShaderVariantCollection Structure Analysis ===");
            
            var type = typeof(ShaderVariantCollection);
            
            // 获取所有公共属性
            var properties = type.GetProperties();
            Debug.Log("Public Properties:");
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(collection);
                    Debug.Log($"  {prop.Name}: {value} (Type: {prop.PropertyType})");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  {prop.Name}: Error - {e.Message}");
                }
            }
            
            // 获取所有公共方法
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Debug.Log("Public Methods:");
            foreach (var method in methods)
            {
                if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                {
                    Debug.Log($"  {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                }
            }
            
            // 获取所有非公共字段
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Log("Non-Public Fields:");
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(collection);
                    Debug.Log($"  {field.Name}: {value} (Type: {field.FieldType})");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  {field.Name}: Error - {e.Message}");
                }
            }
            
            Debug.Log("=== End Analysis ===");
        }
    }
} 
