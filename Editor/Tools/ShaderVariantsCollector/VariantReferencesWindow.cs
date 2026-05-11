using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using YooAsset.Editor;

namespace ShaderVariantsCollector
{
    public class VariantReferencesWindow : EditorWindow
    {
        private ShaderVariantData targetVariant;
        private MaterialReferencesTreeView treeView;
        private TreeViewState treeViewState;
        private SearchField searchField;
        private string searchText = string.Empty;
        private bool isScanning;
        private Vector2 scrollPos;

        // 运行时缓存 - 参考ResourceChecker策略
        private static Dictionary<string, CachedVariantReferences> runtimeCache = new Dictionary<string, CachedVariantReferences>();
        private static bool cacheInitialized = false;
        private static bool collectedInPlayingMode = false;
        private const int MAX_CACHE_SIZE = 20; // 最大缓存条目数
        // 不使用时间过期策略，除非手动清理或对象失效

        // 与主窗口一致的样式
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private Color headerBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);

        [System.Serializable]
        private class CachedVariantReferences
        {
            public List<Material> materials = new List<Material>();
            public Dictionary<Material, List<UnityEngine.Object>> materialToRefs = new Dictionary<Material, List<UnityEngine.Object>>();
            public double lastUpdateTime;
            public double lastAccessTime; // 添加访问时间，用于LRU
            public string variantKey;

            public bool IsValid()
            {
                // 仅清理失效引用，不以数量作为有效性条件，避免0结果反复重建
                materials.RemoveAll(m => m == null);
                var keysToRemove = new List<Material>();
                foreach (var kvp in materialToRefs)
                {
                    if (kvp.Key == null)
                    {
                        keysToRemove.Add(kvp.Key);
                        continue;
                    }
                    kvp.Value?.RemoveAll(obj => obj == null);
                }
                foreach (var key in keysToRemove)
                {
                    materialToRefs.Remove(key);
                }
                return true;
            }

            public bool IsExpired()
            {
                // 取消时间过期策略
                return false;
            }

            public void MarkAccessed()
            {
                lastAccessTime = EditorApplication.timeSinceStartup;
            }
        }

        public static void ShowWindow(ShaderVariantData variant)
        {
            var window = GetWindow<VariantReferencesWindow>(true, "Shader Variant References", true);
            window.minSize = new Vector2(520, 360);
            window.targetVariant = variant;
            window.Initialize();
            window.Show();
        }

        private void Initialize()
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += () =>
            {
                treeView?.SetFocusAndEnsureSelectedItem();
            };

            // 初始化样式
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(10, 10, 10, 10),
                alignment = TextAnchor.MiddleLeft
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(10, 10, 5, 5),
                alignment = TextAnchor.MiddleLeft
            };

            treeView = new MaterialReferencesTreeView(treeViewState, this);
            
            // 检查缓存状态，类似ResourceChecker
            RemoveDestroyedReferences();
            StartScanWithCache();
        }

        private void OnEnable()
        {
            // 监听播放模式变化，清理缓存
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            // 监听项目变化，清理缓存
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 播放模式变化时清理缓存，参考ResourceChecker
            if (collectedInPlayingMode != Application.isPlaying)
            {
                ClearRuntimeCache();
                collectedInPlayingMode = Application.isPlaying;
            }
        }

        private void OnProjectChanged()
        {
            // 项目资源变化时清理缓存
            EditorApplication.delayCall += () =>
            {
                RemoveDestroyedReferences();
            };
        }

        /// <summary>
        /// 清理运行时缓存（参考ResourceChecker策略）
        /// </summary>
        public static void ClearRuntimeCache()
        {
            runtimeCache.Clear();
            cacheInitialized = false;
        }

        /// <summary>
        /// 获取当前缓存的统计信息
        /// </summary>
        public static string GetCacheStats()
        {
            RemoveDestroyedReferences();
            int totalMaterials = 0;
            int totalReferences = 0;
            foreach (var cache in runtimeCache.Values)
            {
                totalMaterials += cache.materials?.Count ?? 0;
                totalReferences += cache.materialToRefs?.Values.Sum(refs => refs?.Count ?? 0) ?? 0;
            }
            return $"Cached Variants: {runtimeCache.Count}, Materials: {totalMaterials}, References: {totalReferences}";
        }

        /// <summary>
        /// 性能测试：强制重新扫描并测量时间
        /// </summary>
        // [MenuItem("Tools/Shader Variants Collector/Test Cache Performance")]
        public static void TestCachePerformance()
        {
            var windows = Resources.FindObjectsOfTypeAll<VariantReferencesWindow>();
            if (windows.Length == 0)
            {
                EditorUtility.DisplayDialog("Test Cache Performance", "No VariantReferencesWindow is open. Please open a variant references window first.", "OK");
                return;
            }

            var window = windows[0];
            if (window.targetVariant?.shader == null)
            {
                EditorUtility.DisplayDialog("Test Cache Performance", "No target variant set.", "OK");
                return;
            }

            string cacheKey = window.GetVariantCacheKey();
            bool hadCache = runtimeCache.ContainsKey(cacheKey);

            // 清除缓存并测量重建时间
            if (hadCache)
            {
                runtimeCache.Remove(cacheKey);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            window.StartScan();
            
            EditorApplication.delayCall += () =>
            {
                stopwatch.Stop();
                string message = $"Scan completed in {stopwatch.ElapsedMilliseconds}ms\n";
                message += $"Previous cache status: {(hadCache ? "Had cache" : "No cache")}\n";
                message += GetCacheStats();
                Debug.Log($"[VariantReferencesWindow] Performance test: {message}");
                EditorUtility.DisplayDialog("Cache Performance Test", message, "OK");
            };
        }

        private static void RemoveDestroyedReferences()
        {
            if (!cacheInitialized) return;

            // 仅做条目内清理，不删除缓存项，避免命中后仍触发重建
            foreach (var kvp in runtimeCache)
            {
                kvp.Value.IsValid();
            }
        }

        private string GetVariantCacheKey()
        {
            if (targetVariant?.shader == null) return string.Empty;
            // 使用Shader GUID而非名称，避免同名/改名导致的缓存未命中
            string shaderPath = AssetDatabase.GetAssetPath(targetVariant.shader);
            string shaderGuid = string.IsNullOrEmpty(shaderPath) ? targetVariant.shader.name : AssetDatabase.AssetPathToGUID(shaderPath);
            var keys = (targetVariant.keywords != null ? targetVariant.keywords : new List<string>())
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .Select(k => k.ToUpperInvariant())
                .Distinct()
                .OrderBy(k => k);
            return $"{shaderGuid}|{targetVariant.passType}|{string.Join(",", keys)}";
        }

        private void StartScanWithCache()
        {
            if (targetVariant?.shader == null) return;

            string cacheKey = GetVariantCacheKey();
            
            // 尝试从运行时缓存获取
            if (runtimeCache.TryGetValue(cacheKey, out var cached))
            {
                // 检查缓存有效性
                if (cached.IsValid())
                {
                    // 标记访问并使用缓存数据，立即显示
                    cached.MarkAccessed();
                    treeView?.Build(cached.materials, cached.materialToRefs);
                    Repaint();
                    Debug.Log($"[VariantReferencesWindow] Cache hit for variant: {targetVariant.shader.name}");
                    return;
                }
                else
                {
                    // 缓存过期，移除
                    runtimeCache.Remove(cacheKey);
                    Debug.Log($"[VariantReferencesWindow] Cache invalid for variant (destroyed refs): {targetVariant.shader.name}");
                }
            }
            else
            {
                Debug.Log($"[VariantReferencesWindow] Cache miss for variant: {targetVariant.shader.name}");
            }

            // 缓存未命中或已过期，执行扫描
            StartScan();
        }

        private void OnGUI()
        {
            // 绘制标题和背景
            DrawHeader();
            
            // 绘制工具栏
            DrawToolbar();

            // 绘制TreeView
            Rect treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            if (treeView != null)
            {
                treeView.OnGUI(treeRect);
            }
            else
            {
                EditorGUILayout.HelpBox("No data to display.", MessageType.Info);
            }

            // 绘制底部状态栏
            DrawStatusBar();
        }

        private void DrawStatusBar()
        {
            if (Event.current.type != EventType.Repaint) return;

            // 绘制状态栏背景
            Rect statusRect = new Rect(0, position.height - 20, position.width, 20);
            EditorGUI.DrawRect(statusRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));

            // 左侧显示材质和引用数量
            string statsInfo = "";
            if (treeView != null)
            {
                int matCount = treeView.GetTotalMaterialCount();
                int refCount = treeView.GetTotalReferenceCount();
                statsInfo = $"Materials: {matCount} | References: {refCount}";
            }
            
            var leftStyle = new GUIStyle(EditorStyles.miniLabel) 
            { 
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(10, position.height - 20, 200, 20), statsInfo, leftStyle);

            // 右侧显示缓存状态
            string cacheInfo = $"Cache: {runtimeCache.Count}/{MAX_CACHE_SIZE}";
            var rightStyle = new GUIStyle(EditorStyles.miniLabel) 
            { 
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(position.width - 120, position.height - 20, 115, 20), cacheInfo, rightStyle);
        }

        private void DrawHeader()
        {
            // 绘制标题背景
            Rect headerRect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.DrawRect(headerRect, headerBackgroundColor);
            
            // 绘制标题文本
            GUI.Label(headerRect, "Shader Variant References", headerStyle);
            
            // 绘制变体信息
            if (targetVariant != null)
            {
                Rect infoRect = EditorGUILayout.GetControlRect(false, 25);
                EditorGUI.DrawRect(infoRect, new Color(0.1f, 0.1f, 0.1f, 0.1f));
                
                // 显示shader图标
                Rect iconRect = infoRect;
                iconRect.width = 20;
                iconRect.x += 10;
                if (targetVariant.shader != null)
                {
                    GUI.DrawTexture(iconRect, AssetPreview.GetMiniThumbnail(targetVariant.shader), ScaleMode.ScaleToFit);
                }
                
                // 显示变体信息
                Rect labelRect = infoRect;
                labelRect.x += 35;
                labelRect.width -= 35;
                GUI.Label(labelRect, $"{targetVariant.shader.name} | Pass: {targetVariant.passType} | Keywords: {targetVariant.GetKeywordsString()}", subHeaderStyle);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool isCached = !string.IsNullOrEmpty(GetVariantCacheKey()) && runtimeCache.ContainsKey(GetVariantCacheKey());
                if (isCached)
                {
                    GUILayout.Label("⚡", EditorStyles.boldLabel, GUILayout.Width(20)); // 闪电符号表示使用缓存
                }
                
                GUILayout.FlexibleSpace();
                
                GUILayout.Label("Search:", EditorStyles.miniLabel);
                searchText = searchField.OnToolbarGUI(searchText, GUILayout.Width(200));
                
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    // 强制重建持久化缓存（Prefab索引）
                    EditorUtility.DisplayProgressBar("Rebuilding Cache", "Refreshing prefab indexes...", 0.1f);
                    try
                    {
                        SVCCache.Clear();
                        SVCCache.EnsurePrefabIndex();
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }

                    // 清除此变体的运行时缓存
                    string cacheKey = GetVariantCacheKey();
                    if (runtimeCache.ContainsKey(cacheKey))
                    {
                        runtimeCache.Remove(cacheKey);
                    }

                    // 重新扫描
                    StartScan();
                }
            }
        }

        private void StartScan()
        {
            if (targetVariant?.shader == null) return;

            isScanning = true;
            var scanStartTime = System.Diagnostics.Stopwatch.StartNew();
            
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // 自动确保Prefab引用缓存就绪
                    EditorUtility.DisplayProgressBar("Building Cache", "Indexing prefab references...", 0.1f);
                    SVCCache.EnsurePrefabIndex();

                    EditorUtility.DisplayProgressBar("Scanning", "Collecting YooAsset package materials...", 0.4f);
                    var materials = GetMaterialsForVariant();

                    EditorUtility.DisplayProgressBar("Scanning", "Building references (cached + fallback)...", 0.7f);
                    var materialToRefs = new Dictionary<Material, List<UnityEngine.Object>>();
                    foreach (var mat in materials)
                    {
                        var refs = SVCCache.GetPrefabsForMaterial(mat);

                        // 回退：若缓存未命中Prefab引用，降级到路径匹配扫描
                        if (refs == null || refs.Count == 0)
                        {
                            var fallback = FallbackFindPrefabsByMaterialPath(mat);
                            refs = fallback;
                        }

                        // 合并代码文件引用（启发式）
                        var codeRefs = FindCodeReferencesForMaterial(mat);
                        if (codeRefs.Count > 0)
                        {
                            if (refs == null) refs = new List<UnityEngine.Object>();
                            refs.AddRange(codeRefs);
                        }

                        materialToRefs[mat] = refs?.Distinct().ToList() ?? new List<UnityEngine.Object>();
                    }

                    // 保存到运行时缓存
                    string cacheKey = GetVariantCacheKey();
                    var cached = new CachedVariantReferences
                    {
                        materials = materials,
                        materialToRefs = materialToRefs,
                        lastUpdateTime = EditorApplication.timeSinceStartup,
                        lastAccessTime = EditorApplication.timeSinceStartup,
                        variantKey = cacheKey
                    };
                    runtimeCache[cacheKey] = cached;
                    cacheInitialized = true;

                    // 检查并清理缓存大小
                    RemoveDestroyedReferences();

                    treeView?.Build(materials, materialToRefs);
                    
                    scanStartTime.Stop();
                    Debug.Log($"[VariantReferencesWindow] Scan completed in {scanStartTime.ElapsedMilliseconds}ms for variant: {targetVariant.shader.name} (Materials: {materials.Count}, Total References: {materialToRefs.Values.Sum(refs => refs.Count)})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Scan failed: {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    isScanning = false;
                    Repaint();
                }
            };
        }

        /// <summary>
        /// 获取当前变体对应的材质列表。
        /// </summary>
        private List<Material> GetMaterialsForVariant()
        {
            return GetYooAssetPackageMaterialsForVariant(targetVariant);
        }

        /// <summary>
        /// 从YooAsset打包资源的依赖中获取匹配当前变体的材质。
        /// </summary>
        private List<Material> GetYooAssetPackageMaterialsForVariant(ShaderVariantData variant)
        {
            var materials = new List<Material>();
            if (variant == null || variant.shader == null) return materials;

            var materialPaths = GetYooAssetPackageMaterialPaths();
            foreach (var materialPath in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (IsMaterialMatchVariant(material, variant))
                {
                    materials.Add(material);
                }
            }

            return materials.Distinct().ToList();
        }

        /// <summary>
        /// 获取YooAsset所有包裹资源依赖到的材质路径。
        /// </summary>
        private List<string> GetYooAssetPackageMaterialPaths()
        {
            var allAssets = new HashSet<string>();
            var materialPaths = new List<string>();
            var packages = AssetBundleCollectorSettingData.Setting.Packages;

            foreach (var package in packages)
            {
                CollectResult collectResult = AssetBundleCollectorSettingData.Setting.GetPackageAssets(EBuildMode.DryRunBuild, package.PackageName);
                foreach (var assetInfo in collectResult.CollectAssets)
                {
                    string[] depends = AssetDatabase.GetDependencies(assetInfo.AssetInfo.AssetPath, true);
                    foreach (var dependAsset in depends)
                    {
                        allAssets.Add(dependAsset);
                    }
                }
            }

            foreach (var assetPath in allAssets)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType == typeof(Material))
                {
                    materialPaths.Add(assetPath);
                }
            }

            return materialPaths;
        }

        /// <summary>
        /// 保留旧逻辑：从项目内所有材质中获取匹配当前变体的材质。
        /// </summary>
        private List<Material> GetAllProjectMaterialsForVariant(ShaderVariantData variant)
        {
            return SVCCache.GetMaterialsForVariant(variant);
        }

        /// <summary>
        /// 判断材质是否匹配指定Shader变体。
        /// </summary>
        private bool IsMaterialMatchVariant(Material material, ShaderVariantData variant)
        {
            if (material == null || material.shader == null || variant == null || variant.shader == null)
            {
                return false;
            }

            if (material.shader != variant.shader)
            {
                return false;
            }

            var targetKeywords = new HashSet<string>(
                (variant.keywords ?? new List<string>())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
            );
            var materialKeywords = new HashSet<string>(
                (material.shaderKeywords ?? new string[0])
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
            );

            return targetKeywords.IsSubsetOf(materialKeywords);
        }

        private List<UnityEngine.Object> FallbackFindPrefabsByMaterialPath(Material material)
        {
            var results = new List<UnityEngine.Object>();
            if (material == null) return results;
            string targetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(targetPath)) return results;

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                try
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        var mats = r.sharedMaterials;
                        if (mats == null) continue;
                        foreach (var m in mats)
                        {
                            if (m == null) continue;
                            string mp = AssetDatabase.GetAssetPath(m);
                            if (!string.IsNullOrEmpty(mp) && mp == targetPath)
                            {
                                results.Add(prefab);
                                goto NEXT_PREFAB;
                            }
                        }
                    }
                }
                catch { }
                NEXT_PREFAB: ;
            }
            return results.Distinct().ToList();
        }

        private List<UnityEngine.Object> FindCodeReferencesForMaterial(Material material)
        {
            var results = new List<UnityEngine.Object>();
            try
            {
                string matPath = AssetDatabase.GetAssetPath(material);
                string fileName = string.IsNullOrEmpty(matPath) ? null : System.IO.Path.GetFileName(matPath);
                var textGuids = AssetDatabase.FindAssets("t:TextAsset");
                foreach (var guid in textGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        string content = System.IO.File.ReadAllText(path);
                        if ((!string.IsNullOrEmpty(matPath) && content.Contains(matPath)) ||
                            (!string.IsNullOrEmpty(fileName) && content.Contains(fileName)))
                        {
                            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                            if (ta != null) results.Add(ta);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return results.Distinct().ToList();
        }

        private List<UnityEngine.Object> FindReferencesForMaterial(Material material)
        {
            var results = new List<UnityEngine.Object>();
            if (material == null) return results;

            // 1) Prefabs 引用（Renderer.sharedMaterials 包含该材质）
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                try
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        var mats = renderer.sharedMaterials;
                        if (mats == null) continue;
                        if (mats.Any(m => m == material))
                        {
                            results.Add(prefab);
                            break;
                        }
                    }
                }
                catch { }
            }

            // 2) 代码文件中是否存在对该材质资源路径的直接字符串引用（启发式）
            string matPath = AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(matPath))
            {
                var textGuids = AssetDatabase.FindAssets("t:TextAsset");
                foreach (var guid in textGuids)
                {
                    try
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                        string content = File.ReadAllText(path);
                        if (content.Contains(matPath) || content.Contains(Path.GetFileName(matPath)))
                        {
                            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                            if (ta != null) results.Add(ta);
                        }
                    }
                    catch { }
                }
            }

            return results.Distinct().ToList();
        }

        internal void PingObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        internal string GetSearchText()
        {
            return searchText ?? string.Empty;
        }

        internal ShaderVariantData GetTargetVariant()
        {
            return targetVariant;
        }
    }

    internal class MaterialReferencesTreeView : TreeView
    {
        private class Node : TreeViewItem
        {
            public UnityEngine.Object payload;
            public Node(int id, int depth, string displayName, UnityEngine.Object payload) : base(id, depth, displayName)
            {
                this.payload = payload;
            }
        }

        private readonly VariantReferencesWindow window;
        private readonly List<Material> materials = new List<Material>();
        private readonly Dictionary<Material, List<UnityEngine.Object>> materialToRefs = new Dictionary<Material, List<UnityEngine.Object>>();
        private int idCounter = 1;

        // 与主界面保持一致的颜色设置
        private static readonly Color materialColor = new Color(0.7f, 0.9f, 1f, 0.3f);     // 浅蓝色
        private static readonly Color prefabColor = new Color(0.9f, 1f, 0.7f, 0.3f);       // 浅绿色
        private static readonly Color codeColor = new Color(1f, 0.9f, 0.7f, 0.3f);         // 浅橙色

        public MaterialReferencesTreeView(TreeViewState state, VariantReferencesWindow window) : base(state)
        {
            this.window = window;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20f; // 与主界面保持一致的行高
            useScrollView = true; // 与主界面一致
            Reload();
        }

        public void Build(List<Material> mats, Dictionary<Material, List<UnityEngine.Object>> refsMap)
        {
            materials.Clear();
            materials.AddRange(mats.Where(m => m != null));
            materialToRefs.Clear();
            foreach (var kv in refsMap)
            {
                materialToRefs[kv.Key] = kv.Value?.Where(o => o != null).Distinct().ToList() ?? new List<UnityEngine.Object>();
            }
            Reload();
        }

        public int GetTotalMaterialCount()
        {
            return materials.Count;
        }

        public int GetTotalReferenceCount()
        {
            return materialToRefs.Values.Sum(refs => refs?.Count ?? 0);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            idCounter = 1;

            string search = window.GetSearchText();

            foreach (var mat in materials)
            {
                if (!string.IsNullOrEmpty(search) && !mat.name.ToLower().Contains(search.ToLower()))
                    continue;

                var matNode = new Node(idCounter++, 0, mat.name, mat);
                matNode.children = new List<TreeViewItem>();

                if (materialToRefs.TryGetValue(mat, out var refs) && refs != null && refs.Count > 0)
                {
                    foreach (var r in refs)
                    {
                        string childName = r is GameObject ? $"[Prefab] {r.name}" : $"[Code] {r.name}";
                        var child = new Node(idCounter++, 1, childName, r);
                        matNode.AddChild(child);
                    }
                }

                if (root.children == null) root.children = new List<TreeViewItem>();
                root.AddChild(matNode);
            }

            if (root.children == null)
            {
                root.children = new List<TreeViewItem>();
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as Node;
            if (item == null)
            {
                base.RowGUI(args);
                return;
            }

            // 不再绘制任何背景色，保持与主界面一致的清爽风格

            // 先绘制自定义折叠箭头（与主界面一致的字符▶ / ▼），用于有子节点的行
            float foldoutWidth = 16f;
            float indent = GetContentIndent(item);
            if (item.hasChildren)
            {
                Rect foldRect = new Rect(args.rowRect.x + Mathf.Max(0, indent - foldoutWidth), args.rowRect.y, foldoutWidth, args.rowRect.height);
                bool expanded = IsExpanded(item.id);
                if (GUI.Button(foldRect, expanded ? "▼" : "▶", EditorStyles.label))
                {
                    SetExpanded(item.id, !expanded);
                }
            }

            // 使用与主界面一致的行绘制方式（图标 + 文本 + 操作按钮）
            DrawRow(args);
        }

        private void DrawRow(RowGUIArgs args)
        {
            var item = args.item as Node;
            if (item == null) return;

            Rect rect = args.rowRect;
            float indent = GetContentIndent(item);
            rect.x += indent;
            rect.width -= indent;

            // 绘制图标
            Rect iconRect = rect;
            iconRect.width = 20;

            if (item.depth == 0) // 材质行
            {
                if (item.payload is Material mat)
                {
                    var icon = AssetPreview.GetMiniThumbnail(mat) ?? EditorGUIUtility.ObjectContent(null, typeof(Material)).image;
                    if (icon != null)
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }
            }
            else if (item.payload is GameObject)
            {
                var icon = EditorGUIUtility.FindTexture("PrefabModel Icon");
                if (icon != null)
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
            else
            {
                var icon = EditorGUIUtility.FindTexture("cs Script Icon");
                if (icon != null)
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            // 绘制文本
            Rect labelRect = rect;
            labelRect.x += 22;
            labelRect.width -= 22;
            GUI.Label(labelRect, item.displayName);

            // 绘制操作按钮（仅材质行）
            if (item.depth == 0)
            {
                Rect buttonRect = rect;
                buttonRect.x = rect.xMax - 60;
                buttonRect.width = 55;
                if (GUI.Button(buttonRect, "Locate", EditorStyles.miniButton))
                {
                    window.PingObject(item.payload);
                }
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var node = FindItem(id, rootItem) as Node;
            if (node?.payload != null)
            {
                window.PingObject(node.payload);
            }
        }

        protected override void SingleClickedItem(int id)
        {
            var node = FindItem(id, rootItem) as Node;
            if (node?.payload is Material mat)
            {
                window.PingObject(mat);
            }
        }
    }
} 
