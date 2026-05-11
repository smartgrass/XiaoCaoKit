using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderVariantsCollector
{
    public class ShaderVariantsCollectorWindow : EditorWindow
    {
        private ShaderVariantCollection selectedCollection;
        private ShaderVariantsTreeView treeView;
        private TreeViewState treeViewState;
        private Vector2 scrollPosition;
        private bool isCollecting = false;
        private float collectionProgress = 0f;
        private List<ShaderVariantData> collectedVariants = new List<ShaderVariantData>();
        private List<ShaderVariantData> newVariants = new List<ShaderVariantData>();
        
        // 剔除配置相关
        private ExclusionSettings exclusionSettings;
        private bool showExclusionSettings = false;
        private Vector2 exclusionScrollPosition;
        private string newShaderToExclude = "";
        private string newKeywordToExclude = "";

        [MenuItem("Tools/Shader Variants Collector/Open Editor Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantsCollectorWindow>("Shader Variants Collector");
            window.minSize = new Vector2(800, 600);
            window.LoadExclusionSettings();
        }

        [MenuItem("Tools/Shader Variants Collector/Clear Runtime Cache")]
        public static void ClearRuntimeCache()
        {
            VariantReferencesWindow.ClearRuntimeCache();
            SVCCache.Clear();
            Debug.Log("Runtime cache cleared.");
        }

        [MenuItem("Tools/Shader Variants Collector/Show Cache Stats")]
        public static void ShowCacheStats()
        {
            string stats = VariantReferencesWindow.GetCacheStats();
            string svcStats = SVCCache.HasIndexes() ? "SVCCache: Active" : "SVCCache: Empty";
            EditorUtility.DisplayDialog("Cache Statistics", $"{stats}\n{svcStats}", "OK");
        }
        
        private void LoadExclusionSettings()
        {
            exclusionSettings = ExclusionSettings.GetOrCreateSettings();
        }
        
        private void SaveExclusionSettings()
        {
            if (exclusionSettings != null)
            {
                exclusionSettings.SaveSettings();
            }
        }

        public ExclusionSettings GetExclusionSettings()
        {
            if (exclusionSettings == null)
            {
                LoadExclusionSettings();
            }
            return exclusionSettings;
        }
        
        private void DrawExclusionSettings()
        {
            if (exclusionSettings == null)
            {
                LoadExclusionSettings();
            }
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("剔除配置", EditorStyles.boldLabel);
            
            exclusionScrollPosition = EditorGUILayout.BeginScrollView(exclusionScrollPosition, GUILayout.MaxHeight(200));
            
            // 剔除的Shader列表
            EditorGUILayout.LabelField("剔除的Shader:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newShaderToExclude = EditorGUILayout.TextField("添加Shader名称:", newShaderToExclude);
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(newShaderToExclude))
                {
                    exclusionSettings.AddExcludedShader(newShaderToExclude);
                    newShaderToExclude = "";
                    SaveExclusionSettings();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < exclusionSettings.excludedShaderNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {exclusionSettings.excludedShaderNames[i]}");
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    exclusionSettings.excludedShaderNames.RemoveAt(i);
                    SaveExclusionSettings();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // 剔除的关键字列表
            EditorGUILayout.LabelField("剔除的关键字:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newKeywordToExclude = EditorGUILayout.TextField("添加关键字:", newKeywordToExclude);
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(newKeywordToExclude))
                {
                    exclusionSettings.AddExcludedKeyword(newKeywordToExclude);
                    newKeywordToExclude = "";
                    SaveExclusionSettings();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < exclusionSettings.excludedKeywords.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {exclusionSettings.excludedKeywords[i]}");
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    exclusionSettings.excludedKeywords.RemoveAt(i);
                    SaveExclusionSettings();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置为默认设置"))
            {
                exclusionSettings.excludedShaderNames.Clear();
                exclusionSettings.excludedKeywords.Clear();
                
                // 重新初始化默认值
                exclusionSettings.excludedShaderNames.Add("Hidden/");
                exclusionSettings.excludedShaderNames.Add("Legacy Shaders/");
                exclusionSettings.excludedKeywords.Add("_DEBUG");
                exclusionSettings.excludedKeywords.Add("EDITOR_VISUALIZATION");
                
                SaveExclusionSettings();
            }
            if (GUILayout.Button("清空所有设置"))
            {
                exclusionSettings.excludedShaderNames.Clear();
                exclusionSettings.excludedKeywords.Clear();
                SaveExclusionSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void OnEnable()
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();
            
            if (treeView == null)
                treeView = new ShaderVariantsTreeView(treeViewState, this);
            
            RefreshTreeView();
            LoadExclusionSettings();
        }
        
        private void OnDestroy()
        {
            SaveExclusionSettings();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawMainContent();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var prevCollection = selectedCollection;
                selectedCollection = (ShaderVariantCollection)EditorGUILayout.ObjectField(selectedCollection, typeof(ShaderVariantCollection), false, GUILayout.ExpandWidth(true));
                if (selectedCollection != prevCollection)
                {
                    RefreshTreeView();
                }

                GUILayout.FlexibleSpace();

                // 配置按钮
                if (GUILayout.Button(showExclusionSettings ? "Hide Settings" : "Show Settings", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    showExclusionSettings = !showExclusionSettings;
                }

                // 新建变体集合按钮
                if (GUILayout.Button("New Collection", EditorStyles.toolbarButton))
                {
                    CreateNewCollection();
                }

                // 收集变体按钮（检查是否有选中的集合并且没有在收集中）
                GUI.enabled = selectedCollection != null && !isCollecting;
                if (GUILayout.Button("Collect Variants", EditorStyles.toolbarButton))
                {
                    CollectVariants();
                }
                
                GUI.enabled = true;
            }
        }

        private void DrawMainContent()
        {
            EditorGUILayout.BeginVertical();
            
            // 绘制配置界面
            if (showExclusionSettings)
            {
                DrawExclusionSettings();
                EditorGUILayout.Space();
            }
            
            if (selectedCollection == null)
            {
                EditorGUILayout.HelpBox("Please select a ShaderVariantCollection asset.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (isCollecting)
            {
                DrawProgressBar();
            }

            DrawTreeView();
            
            if (newVariants.Count > 0)
            {
                DrawNewVariantsActions();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Collecting variants...", GUILayout.Width(120));
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)), collectionProgress, $"{collectionProgress:P0}");
            EditorGUILayout.EndHorizontal();
            Repaint();
        }

        private void DrawTreeView()
        {
            if (treeView != null)
            {
                var treeViewRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                treeView.OnGUI(treeViewRect);
            }
        }

        private void DrawNewVariantsActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("New Variants Found:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Confirm All", GUILayout.Width(100)))
                {
                    // 延迟执行，避免在GUI绘制过程中修改集合导致layout错误
                    EditorApplication.delayCall += ConfirmAllNewVariants;
                }
                if (GUILayout.Button("Ignore All", GUILayout.Width(100)))
                {
                    // 延迟执行，避免在GUI绘制过程中修改集合导致layout错误
                    EditorApplication.delayCall += IgnoreAllNewVariants;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in DrawNewVariantsActions: {e.Message}");
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RefreshTreeView()
        {
            if (treeView != null)
            {
                treeView.SetCollection(selectedCollection);
                treeView.Reload();
            }
        }

        private void CollectVariants()
        {
            if (selectedCollection == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a ShaderVariantCollection first.", "OK");
                return;
            }

            // 收集前重建缓存
            try { SVCCache.RefreshAll(); } catch (Exception e) { Debug.LogWarning($"Refresh cache failed: {e.Message}"); }

            isCollecting = true;
            collectionProgress = 0f;
            newVariants.Clear();
            
            // 开始收集变体
            EditorApplication.update += CollectVariantsUpdate;
        }

        private void CollectVariantsUpdate()
        {
            if (!isCollecting)
            {
                EditorApplication.update -= CollectVariantsUpdate;
                return;
            }

            // 模拟收集进度
            collectionProgress += 0.01f;
            
            if (collectionProgress >= 1f)
            {
                isCollecting = false;
                collectionProgress = 1f;
                
                // 完成收集，查找新变体
                FindNewVariants();
                
                EditorApplication.update -= CollectVariantsUpdate;
            }
        }

        private void FindNewVariants()
        {
            // 这里应该实现实际的变体收集逻辑
            // 暂时添加一些示例数据
            var existingVariants = GetExistingVariants();
            var allVariants = GetAllVariantsInProject();
            
            newVariants = allVariants
                .Where(v => !existingVariants.Contains(v))
                .OrderBy(v => v?.shader != null ? v.shader.name : string.Empty, StringComparer.Ordinal)
                .ThenBy(v => v != null ? v.passType.ToString() : string.Empty, StringComparer.Ordinal)
                .ThenBy(v => v != null ? v.GetKeywordsString() : string.Empty, StringComparer.Ordinal)
                .ToList();
            
            if (treeView != null)
            {
                treeView.AddNewVariants(newVariants);
                treeView.Reload();
            }
        }

        private List<ShaderVariantData> GetExistingVariants()
        {
            var variants = new List<ShaderVariantData>();
            if (selectedCollection != null)
            {
                variants = ShaderVariantCollector.CollectVariantsFromCollection(selectedCollection);
            }
            return variants;
        }

        private List<ShaderVariantData> GetAllVariantsInProject()
        {
            return ShaderVariantCollector.CollectVariantsFromProject(exclusionSettings);
        }

        private void ConfirmAllNewVariants()
        {
            if (selectedCollection == null || newVariants.Count == 0)
                return;

            try
            {
                var variantsToProcess = new List<ShaderVariantData>(newVariants);
                int successCount = 0;
                int failCount = 0;

                // 分别处理收集变体和手动变体
                var collectedVariants = variantsToProcess.Where(v => v.isCollected).ToList();
                var manualVariants = variantsToProcess.Where(v => !v.isCollected).ToList();

                // 批量处理收集到的变体（使用对象初始化语法）
                foreach (var variant in collectedVariants)
                {
                    bool added = AddCollectedVariantWithObjectInitializer(variant);
                    if (added)
                    {
                        successCount++;
                        newVariants.Remove(variant);
                        if (treeView != null)
                        {
                            treeView.RemoveNewVariant(variant);
                        }
                    }
                    else
                    {
                        failCount++;
                        Debug.LogWarning($"Failed to add collected variant: {variant.shader.name} - {variant.GetKeywordsString()}");
                    }
                }

                // 逐个处理手动变体（使用API方法）
                foreach (var variant in manualVariants)
                {
                    bool added = false;
                    var keywordsArray = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                    
                    try
                    {
                        selectedCollection.Add(new ShaderVariantCollection.ShaderVariant(
                            variant.shader, variant.passType, keywordsArray));
                        added = true;
                    }
                    catch
                    {
                        // 如果失败，则尝试任意 PassType
                        added = TryAddVariantWithAnyPass(variant);
                    }

                    if (added)
                    {
                        successCount++;
                        newVariants.Remove(variant);
                        if (treeView != null)
                        {
                            treeView.RemoveNewVariant(variant);
                        }
                    }
                    else
                    {
                        failCount++;
                        Debug.LogWarning($"Failed to add variant: {variant.shader.name} - {variant.GetKeywordsString()}");
                    }
                }

                // 显示结果统计
                if (successCount > 0)
                {
                    Debug.Log($"Successfully added {successCount} variants to collection.");
                }
                if (failCount > 0)
                {
                    Debug.LogWarning($"Failed to add {failCount} variants. Check console for details.");
                }

                // 刷新UI
                if (treeView != null)
                {
                    treeView.Reload();
                }
                RefreshTreeView();
                EditorUtility.SetDirty(selectedCollection);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in ConfirmAllNewVariants: {e.Message}");
            }
        }

        private void IgnoreAllNewVariants()
        {
            try
            {
                newVariants.Clear();
                if (treeView != null)
                {
                    treeView.ClearNewVariants();
                    treeView.Reload();
                }
                RefreshTreeView();
                Debug.Log("All new variants have been ignored.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in IgnoreAllNewVariants: {e.Message}");
            }
        }

        public void OnVariantConfirmed(ShaderVariantData variant)
        {
            if (selectedCollection != null)
            {
                var keywordsArray = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                bool added = false;
                
                if (variant.isCollected)
                {
                    // 对于收集到的变体，使用对象初始化语法跳过Unity构造函数的验证
                    added = AddCollectedVariantWithObjectInitializer(variant);
                    if (!added)
                    {
                        Debug.LogWarning($"Could not add collected variant to collection. " +
                                       $"Shader: {variant.shader.name}, Keywords: {variant.GetKeywordsString()}");
                        added = true; // 标记为已添加，避免错误对话框
                    }
                }
                else
                {
                    // 对于手动添加的变体，使用原有的严格校验逻辑
                    try
                    {
                        selectedCollection.Add(new ShaderVariantCollection.ShaderVariant(
                            variant.shader, variant.passType, keywordsArray));
                        added = true;
                    }
                    catch
                    {
                        // 如果失败，则尝试任意 PassType
                        added = TryAddVariantWithAnyPass(variant);
                    }
                    
                    if (!added)
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Keywords",
                            "The selected keyword combination is not valid for any pass in this shader.\n\n" +
                            $"Shader: {variant.shader.name}\nPass: {variant.passType}\nKeywords: {variant.GetKeywordsString()}",
                            "OK");
                    }
                }
                
                EditorUtility.SetDirty(selectedCollection);
            }
            
            newVariants.Remove(variant);
            if (treeView != null)
            {
                treeView.RemoveNewVariant(variant);
                treeView.Reload();
            }
            
            // 刷新TreeView以显示新添加的变体
            RefreshTreeView();
        }

        public void OnVariantIgnored(ShaderVariantData variant)
        {
            newVariants.Remove(variant);
            if (treeView != null)
            {
                treeView.RemoveNewVariant(variant);
                treeView.Reload();
            }
        }

        public bool CollectionHasVariant(Shader shader, PassType passType, IEnumerable<string> keywords)
        {
            if (shader == null || selectedCollection == null)
            {
                return false;
            }
            var target = new HashSet<string>(keywords?.Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>());
            var existing = ShaderVariantCollector.CollectVariantsFromCollection(selectedCollection);
            foreach (var v in existing)
            {
                if (v.shader != shader) continue;
                if (v.passType != passType) continue;
                var set = new HashSet<string>(v.keywords.Where(k => !string.IsNullOrEmpty(k)));
                if (set.SetEquals(target)) return true;
            }
            return false;
        }

        private bool AddCollectedVariantWithObjectInitializer(ShaderVariantData variant)
        {
            try
            {
                // 使用对象初始化语法创建变体，跳过构造函数的验证
                var shaderVariant = new ShaderVariantCollection.ShaderVariant
                {
                    shader = variant.shader,
                    keywords = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray(),
                    passType = variant.passType
                };

                selectedCollection.Add(shaderVariant);
                EditorUtility.SetDirty(selectedCollection);
                Debug.Log($"Added collected variant: {variant.shader.name} - PassType: {variant.passType} - Keywords: {variant.GetKeywordsString()}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to add collected variant {variant.shader.name}: {e.Message}");
                return false;
            }
        }

        private bool TryAddVariantWithAnyPass(ShaderVariantData variant)
        {
            // 遍历常见的 PassType，尝试添加
            var keywordsArray = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
            var passTypes = (PassType[])System.Enum.GetValues(typeof(PassType));
            foreach (var pass in passTypes)
            {
                try
                {
                    selectedCollection.Add(new ShaderVariantCollection.ShaderVariant(
                        variant.shader, pass, keywordsArray));
                    Debug.Log($"Added variant with pass {pass} for shader {variant.shader.name}: {variant.GetKeywordsString()}");
                    return true;
                }
                catch
                {
                    // ignore and try next pass
                }
            }
            return false;
        }



        // YAML methods removed - now using object initializer syntax for collected variants


        
        private void CreateNewCollection()
        {
            // 让用户选择保存位置和文件名
            string defaultName = "NewShaderVariantCollection";
            string defaultPath = "Assets";
            
            // 如果当前有选中的集合，使用其所在目录作为默认目录
            if (selectedCollection != null)
            {
                string currentPath = AssetDatabase.GetAssetPath(selectedCollection);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    defaultPath = System.IO.Path.GetDirectoryName(currentPath);
                }
            }
            
            // 显示保存文件对话框
            string savePath = EditorUtility.SaveFilePanelInProject(
                "Create New Shader Variant Collection",
                defaultName,
                "asset",
                "Please specify where to save the new ShaderVariantCollection",
                defaultPath);
            
            // 如果用户取消了对话框，退出
            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }
            
            // 创建新的ShaderVariantCollection
            var newCollection = new ShaderVariantCollection();
            
            // 保存资源
            AssetDatabase.CreateAsset(newCollection, savePath);
            AssetDatabase.SaveAssets();
            
            // 选中新创建的集合
            selectedCollection = newCollection;
            Selection.activeObject = newCollection;
            EditorGUIUtility.PingObject(newCollection);
            
            // 刷新TreeView
            RefreshTreeView();
            
            Debug.Log($"Created new ShaderVariantCollection: {savePath}");
        }
    }
} 
