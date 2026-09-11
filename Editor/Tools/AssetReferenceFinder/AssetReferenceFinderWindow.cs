#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XiaoCaoEditor
{
    /// <summary>
    /// 查找项目中引用指定资源的其他资源。
    /// </summary>
    public class AssetReferenceFinderWindow : EditorWindow
    {
        private enum ReferenceMode
        {
            Direct,
            IncludeIndirect
        }

        private enum ResultTypeFilter
        {
            All,
            Prefab,
            Scene,
            Material,
            ScriptableObject,
            Other
        }

        private sealed class ReferenceItem
        {
            public string Path;
            public Object Asset;
            public Type AssetType;
        }

        private const int ScanBatchSize = 80;
        private const string WindowTitle = "资源引用查找";

        private Object targetAsset;
        private ReferenceMode referenceMode;
        private ResultTypeFilter resultTypeFilter;
        private string searchText = string.Empty;
        private string targetPath = string.Empty;
        private string scanStatus = "请选择一个资源后开始查找。";
        private string lastScanSummary = string.Empty;
        private readonly List<string> candidatePaths = new List<string>();
        private readonly List<ReferenceItem> results = new List<ReferenceItem>();
        private int scanIndex;
        private int failedAssetCount;
        private bool isScanning;
        private bool scanIncludesIndirect;
        private Vector2 resultScrollPosition;
        private Stopwatch scanStopwatch;

        /// <summary>
        /// 打开资源引用查找窗口。
        /// </summary>
        [MenuItem("XiaoCao/资源工具/资源引用查找", priority = 120)]
        public static void Open()
        {
            OpenForAsset(Selection.activeObject);
        }

        /// <summary>
        /// 使用当前 Project 选中的资源打开引用查找窗口。
        /// </summary>
        [MenuItem("Assets/XiaoCao/查找资源引用", false, 2000)]
        private static void OpenFromSelection()
        {
            OpenForAsset(Selection.activeObject);
        }

        [MenuItem("Assets/XiaoCao/查找资源引用", true)]
        private static bool ValidateOpenFromSelection()
        {
            Object selectedObject = Selection.activeObject;
            string path = selectedObject == null ? string.Empty : AssetDatabase.GetAssetPath(selectedObject);
            return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
        }

        /// <summary>
        /// 打开窗口并设置待查找资源。
        /// </summary>
        public static void OpenForAsset(Object asset)
        {
            AssetReferenceFinderWindow window = GetWindow<AssetReferenceFinderWindow>(WindowTitle);
            window.minSize = new Vector2(620f, 420f);
            window.targetAsset = asset;
            window.ResetResults();
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(620f, 420f);
        }

        private void OnDisable()
        {
            StopScan();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTargetSelector();
            DrawOptions();
            DrawScanControls();
            DrawScanStatus();
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("资源引用查找", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "选择一个资源，扫描哪些 prefab、材质、场景或其他资源引用了它。",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
        }

        private void DrawTargetSelector()
        {
            bool targetChangedByButton = false;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                targetAsset = EditorGUILayout.ObjectField(
                    "目标资源",
                    targetAsset,
                    typeof(Object),
                    false);

                if (GUILayout.Button("当前选择", GUILayout.Width(76f)))
                {
                    targetAsset = Selection.activeObject;
                    targetChangedByButton = true;
                    GUI.FocusControl(null);
                }
            }

            bool targetChangedByField = EditorGUI.EndChangeCheck();
            if (targetChangedByButton || targetChangedByField)
            {
                ResetResults();
            }

            string currentPath = GetAssetPath(targetAsset);
            if (!string.IsNullOrEmpty(currentPath))
            {
                EditorGUILayout.LabelField("资源路径", currentPath, EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("目标必须是 Project 中的资源文件，不能是场景中的对象或文件夹。", MessageType.Info);
            }
        }

        private void DrawOptions()
        {
            ReferenceMode previousReferenceMode = referenceMode;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(isScanning))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    referenceMode = (ReferenceMode)EditorGUILayout.Popup(
                        "搜索范围",
                        (int)referenceMode,
                        new[] { "仅直接引用", "包含间接依赖" });
                    resultTypeFilter = (ResultTypeFilter)EditorGUILayout.Popup(
                        "结果类型",
                        (int)resultTypeFilter,
                        new[] { "全部", "Prefab", "场景", "材质", "ScriptableObject", "其他" });
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (previousReferenceMode != referenceMode && results.Count > 0)
                {
                    ResetResults();
                }
                Repaint();
            }
        }

        private void DrawScanControls()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
                searchText = GUILayout.TextField(
                    searchText,
                    searchStyle,
                    GUILayout.MinWidth(180f));

                GUILayout.FlexibleSpace();

                GUI.enabled = !isScanning && targetAsset != null;
                if (GUILayout.Button("查找引用", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                {
                    StartScan();
                }

                GUI.enabled = isScanning;
                if (GUILayout.Button("取消", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                {
                    StopScan();
                    scanStatus = "已取消扫描。";
                }

                GUI.enabled = true;
            }
        }

        private void DrawScanStatus()
        {
            if (isScanning)
            {
                float progress = candidatePaths.Count == 0
                    ? 0f
                    : (float)scanIndex / candidatePaths.Count;
                Rect progressRect = EditorGUILayout.GetControlRect(false, 18f);
                EditorGUI.ProgressBar(
                    progressRect,
                    progress,
                    string.Format(
                        "扫描中 {0}/{1}，已找到 {2} 个引用",
                        scanIndex,
                        candidatePaths.Count,
                        results.Count));
            }

            if (!string.IsNullOrEmpty(scanStatus))
            {
                EditorGUILayout.LabelField(scanStatus, EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(lastScanSummary))
            {
                EditorGUILayout.LabelField(lastScanSummary, EditorStyles.miniLabel);
            }
        }

        private void DrawResults()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(
                    string.Format("引用结果：{0} / {1}", GetFilteredResultCount(), results.Count),
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                {
                    ResetResults();
                }
            }

            resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition);
            for (int i = 0; i < results.Count; i++)
            {
                ReferenceItem item = results[i];
                if (!MatchesFilter(item))
                {
                    continue;
                }

                DrawResultItem(item, i);
            }

            if (!isScanning && results.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无结果。选择资源后点击“查找引用”。", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResultItem(ReferenceItem item, int index)
        {
            using (new EditorGUILayout.HorizontalScope(index % 2 == 0 ? GUI.skin.box : EditorStyles.helpBox))
            {
                Object displayAsset = item.Asset;
                GUIContent assetContent = displayAsset == null
                    ? new GUIContent("(资源无法加载)", EditorGUIUtility.FindTexture("DefaultAsset Icon"))
                    : EditorGUIUtility.ObjectContent(displayAsset, item.AssetType ?? typeof(Object));

                GUILayout.Label(assetContent.image, GUILayout.Width(22f), GUILayout.Height(22f));

                using (new EditorGUILayout.VerticalScope())
                {
                    string assetName = displayAsset == null
                        ? System.IO.Path.GetFileNameWithoutExtension(item.Path)
                        : displayAsset.name;

                    if (GUILayout.Button(assetName, EditorStyles.label))
                    {
                        PingAsset(item);
                    }

                    EditorGUILayout.LabelField(item.Path, EditorStyles.miniLabel);
                }

                if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(44f)))
                {
                    PingAsset(item);
                }
            }
        }

        /// <summary>
        /// 开始扫描所有项目资源的依赖关系。
        /// </summary>
        private void StartScan()
        {
            if (isScanning)
            {
                return;
            }

            targetPath = GetAssetPath(targetAsset);
            if (string.IsNullOrEmpty(targetPath) || AssetDatabase.IsValidFolder(targetPath))
            {
                scanStatus = "请选择一个有效的 Project 资源文件。";
                return;
            }

            candidatePaths.Clear();
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                string path = allAssetPaths[i];
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase) ||
                    AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                candidatePaths.Add(path);
            }

            candidatePaths.Sort(StringComparer.OrdinalIgnoreCase);
            results.Clear();
            scanIndex = 0;
            failedAssetCount = 0;
            lastScanSummary = string.Empty;
            scanStatus = "正在准备扫描...";
            scanIncludesIndirect = referenceMode == ReferenceMode.IncludeIndirect;
            isScanning = true;
            scanStopwatch = Stopwatch.StartNew();

            EditorApplication.update -= ScanNextBatch;
            EditorApplication.update += ScanNextBatch;
            Repaint();
        }

        /// <summary>
        /// 每帧处理一批资源，避免大型项目扫描时阻塞编辑器界面。
        /// </summary>
        private void ScanNextBatch()
        {
            if (!isScanning)
            {
                return;
            }

            int endIndex = Math.Min(scanIndex + ScanBatchSize, candidatePaths.Count);
            bool includeIndirect = scanIncludesIndirect;
            for (; scanIndex < endIndex; scanIndex++)
            {
                string candidatePath = candidatePaths[scanIndex];
                try
                {
                    string[] dependencies = AssetDatabase.GetDependencies(candidatePath, includeIndirect);
                    if (!ContainsPath(dependencies, targetPath))
                    {
                        continue;
                    }

                    results.Add(new ReferenceItem
                    {
                        Path = candidatePath,
                        Asset = AssetDatabase.LoadMainAssetAtPath(candidatePath),
                        AssetType = AssetDatabase.GetMainAssetTypeAtPath(candidatePath)
                    });
                }
                catch (Exception)
                {
                    failedAssetCount++;
                }
            }

            scanStatus = string.Format(
                "扫描中：{0}/{1}，搜索范围：{2}",
                scanIndex,
                candidatePaths.Count,
                includeIndirect ? "包含间接依赖" : "仅直接引用");
            Repaint();

            if (scanIndex >= candidatePaths.Count)
            {
                FinishScan();
            }
        }

        /// <summary>
        /// 完成扫描并更新结果摘要。
        /// </summary>
        private void FinishScan()
        {
            StopScan();
            results.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path));

            long elapsedMilliseconds = scanStopwatch == null ? 0L : scanStopwatch.ElapsedMilliseconds;
            lastScanSummary = string.Format(
                "扫描完成：找到 {0} 个引用，耗时 {1} ms{2}",
                results.Count,
                elapsedMilliseconds,
                failedAssetCount > 0 ? string.Format("，{0} 个资源读取失败", failedAssetCount) : string.Empty);
            scanStatus = string.IsNullOrEmpty(targetPath)
                ? string.Empty
                : "目标资源：" + targetPath;
            Repaint();
        }

        private void StopScan()
        {
            EditorApplication.update -= ScanNextBatch;
            isScanning = false;
        }

        private void ResetResults()
        {
            StopScan();
            candidatePaths.Clear();
            results.Clear();
            scanIndex = 0;
            failedAssetCount = 0;
            targetPath = GetAssetPath(targetAsset);
            lastScanSummary = string.Empty;
            scanStatus = targetAsset == null ? "请选择一个资源后开始查找。" : "点击“查找引用”开始扫描。";
            Repaint();
        }

        private int GetFilteredResultCount()
        {
            int count = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (MatchesFilter(results[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private bool MatchesFilter(ReferenceItem item)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                bool matchesName = item.Asset != null &&
                                   item.Asset.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesPath = item.Path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesName && !matchesPath)
                {
                    return false;
                }
            }

            switch (resultTypeFilter)
            {
                case ResultTypeFilter.Prefab:
                    return item.Path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
                case ResultTypeFilter.Scene:
                    return item.Path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
                case ResultTypeFilter.Material:
                    return item.AssetType == typeof(Material) ||
                           item.Path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
                case ResultTypeFilter.ScriptableObject:
                    return item.AssetType != null &&
                           typeof(ScriptableObject).IsAssignableFrom(item.AssetType);
                case ResultTypeFilter.Other:
                    return !IsPrefab(item.Path) &&
                           !IsScene(item.Path) &&
                           item.AssetType != typeof(Material) &&
                           !item.Path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                           !(item.AssetType != null &&
                             typeof(ScriptableObject).IsAssignableFrom(item.AssetType));
                default:
                    return true;
            }
        }

        private static bool IsPrefab(string path)
        {
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsScene(string path)
        {
            return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsPath(string[] paths, string expectedPath)
        {
            if (paths == null)
            {
                return false;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                if (string.Equals(paths[i], expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetAssetPath(Object asset)
        {
            return asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
        }

        private static void PingAsset(ReferenceItem item)
        {
            if (item.Asset == null)
            {
                return;
            }

            Selection.activeObject = item.Asset;
            EditorGUIUtility.PingObject(item.Asset);
        }
    }
}
#endif
