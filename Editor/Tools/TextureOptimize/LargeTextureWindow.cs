using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorAssetsTools
{
    /// <summary>
    /// 扫描指定目录下资源所引用的大纹理，按尺寸分档列出（可折叠、可逐项勾选），
    /// 支持把选中纹理按百分比降低分辨率写回源文件，写回前可先备份原图。
    /// </summary>
    public class LargeTextureWindow : EditorWindow
    {
        private const string MenuPath = "XiaoCao/资源工具/大纹理扫描与压缩";
        private const string WindowTitle = "大纹理扫描与压缩";
        private const string DefaultScanRoot = "Assets/_Res";
        private const string DefaultBackupFolder = "TextureBackup";
        private const string PrefPercent = "XCTools.LargeTexture.Percent";
        private const string PrefBackupRoot = "XCTools.LargeTexture.BackupRoot";
        private const string PrefImportMaxSize = "XCTools.LargeTexture.ImportMaxSize";
        private const string PrefDisableMipmap = "XCTools.LargeTexture.DisableMipmap";

        /// <summary>可选的导入上限，0 表示不修改；顺序与 ImportMaxSizeLabels 对应</summary>
        private static readonly int[] ImportMaxSizeOptions = { 0, 8192, 4096, 2048, 1024, 512, 256 };
        private static readonly string[] ImportMaxSizeLabels = { "不修改", "8192", "4096", "2048", "1024", "512", "256" };

        /// <summary>这些扩展名的资源才可能引用到纹理，避免对音频/脚本做无用的依赖查询</summary>
        private static readonly HashSet<string> ReferenceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab", ".mat", ".asset", ".controller", ".overridecontroller", ".anim", ".unity",
            ".fbx", ".obj", ".blend", ".playable", ".spriteatlas", ".mask", ".rendertexture", ".shadergraph"
        };

        private string scanRoot = DefaultScanRoot;
        private int sizeThreshold = 512;
        private Vector2 scrollPosition;
        private List<TextureTier> tiers = new List<TextureTier>();
        private bool scanned;
        private int percent = 50;
        private bool backupBeforeResize = true;

        /// <summary>要写入 TextureImporter.maxTextureSize 的值，0 表示不动</summary>
        private int importMaxSize;
        private bool disableMipmap;
        private bool overwriteBackup;
        private string backupRoot;
        private string statusMessage = "选择目录后点击「扫描」，列表按尺寸分档显示，组内按文件体积从大到小排列。";
        private MessageType statusType = MessageType.Info;
        private GUIStyle nameStyle;

        /// <summary>一条纹理记录（路径 + 源尺寸 + 被引用情况）</summary>
        private sealed class TextureEntry
        {
            public string AssetPath;
            public string FileName;
            public int SourceWidth;
            public int SourceHeight;
            public long FileSize;
            public int ImportMaxSize;
            public bool MipmapEnabled;
            public bool MultipleSprite;
            public List<string> Referencers = new List<string>();
            public bool Selected;
        }

        /// <summary>一个尺寸档位，是一组可勾选纹理的容器</summary>
        private sealed class TextureTier
        {
            public int Min;
            public int Max;
            public string Label;
            public bool Foldout = true;
            public List<TextureEntry> Entries = new List<TextureEntry>();

            /// <summary>档位内已勾选的条目数</summary>
            public int SelectedCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        if (Entries[i].Selected)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            /// <summary>档位内全部文件的体积合计</summary>
            public long TotalSize
            {
                get
                {
                    long total = 0;
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        total += Entries[i].FileSize;
                    }

                    return total;
                }
            }
        }

        [MenuItem(MenuPath, priority = 133)]
        public static void Open()
        {
            LargeTextureWindow window = GetWindow<LargeTextureWindow>(WindowTitle);
            window.minSize = new Vector2(820f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(820f, 560f);
            percent = Mathf.Clamp(EditorPrefs.GetInt(PrefPercent, 50), 5, 100);

            importMaxSize = EditorPrefs.GetInt(PrefImportMaxSize, 0);
            if (Array.IndexOf(ImportMaxSizeOptions, importMaxSize) < 0)
            {
                importMaxSize = 0;
            }

            disableMipmap = EditorPrefs.GetBool(PrefDisableMipmap, false);

            backupRoot = EditorPrefs.GetString(PrefBackupRoot, string.Empty);
            if (string.IsNullOrEmpty(backupRoot))
            {
                backupRoot = Path.GetFullPath(DefaultBackupFolder);
            }

            nameStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 2, 0, 0),
                clipping = TextClipping.Clip
            };
        }

        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                return;
            }

            DrawToolbar();
            DrawSummary();

            // 列表占满剩余空间，底部压缩区保持在窗口下方不随滚动
            GUILayout.Space(2f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            try
            {
                DrawTiers();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            DrawBottomBar();
        }

        /// <summary>顶部扫描设置行</summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("扫描目录", EditorStyles.miniLabel, GUILayout.Width(54f));
                scanRoot = EditorGUILayout.TextField(scanRoot, EditorStyles.toolbarTextField, GUILayout.MinWidth(180f));

                GUILayout.Label("最小边长", EditorStyles.miniLabel, GUILayout.Width(52f));
                sizeThreshold = EditorGUILayout.IntField(sizeThreshold, EditorStyles.toolbarTextField, GUILayout.Width(48f));

                if (GUILayout.Button("扫描", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Scan();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    SetAllFoldout(true);
                }

                if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    SetAllFoldout(false);
                }
            }
        }

        /// <summary>统计行：数量、总体积、已勾选情况</summary>
        private void DrawSummary()
        {
            int totalCount = 0;
            long totalSize = 0;
            int selectedCount = 0;
            long selectedSize = 0;

            for (int i = 0; i < tiers.Count; i++)
            {
                TextureTier tier = tiers[i];
                totalCount += tier.Entries.Count;
                totalSize += tier.TotalSize;
                for (int j = 0; j < tier.Entries.Count; j++)
                {
                    if (tier.Entries[j].Selected)
                    {
                        selectedCount++;
                        selectedSize += tier.Entries[j].FileSize;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(
                    string.Format("共 {0} 张（≥{1}px），合计 {2}", totalCount, sizeThreshold, FormatBytes(totalSize)),
                    EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    string.Format("已勾选 {0} 张，{1}", selectedCount, FormatBytes(selectedSize)),
                    EditorStyles.miniLabel);
            }
        }

        /// <summary>绘制所有档位</summary>
        private void DrawTiers()
        {
            if (!scanned)
            {
                EditorGUILayout.HelpBox(
                    string.Format("扫描 {0} 下所有资源引用到的纹理（含模型、材质、ScriptableObject 引用到的外部贴图），\n" +
                                  "只列出边长 ≥ {1} 的文件，按尺寸分档、组内按体积降序。", scanRoot, sizeThreshold),
                    MessageType.Info);
                return;
            }

            if (tiers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.Format("{0} 下没有引用到边长 ≥ {1} 的纹理。", scanRoot, sizeThreshold),
                    MessageType.Warning);
                return;
            }

            for (int i = 0; i < tiers.Count; i++)
            {
                DrawTier(tiers[i]);
            }
        }

        /// <summary>绘制一个尺寸档位（折叠头 + 组内全选 + 条目列表）</summary>
        private void DrawTier(TextureTier tier)
        {
            GUILayout.Space(4f);

            // Foldout 会撑满剩余宽度，这里按矩形手工排版，把「全选」按钮固定在右侧
            string title = string.Format("{0}  （{1} 张，{2}）", tier.Label, tier.Entries.Count,
                FormatBytes(tier.TotalSize));
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect foldoutRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 60f, rowRect.height);
            Rect buttonRect = new Rect(rowRect.xMax - 54f, rowRect.y, 54f, rowRect.height);

            tier.Foldout = EditorGUI.Foldout(foldoutRect, tier.Foldout, title, true, EditorStyles.foldout);

            bool allSelected = tier.SelectedCount == tier.Entries.Count;
            if (GUI.Button(buttonRect, allSelected ? "全不选" : "全选", EditorStyles.miniButton))
            {
                for (int i = 0; i < tier.Entries.Count; i++)
                {
                    tier.Entries[i].Selected = !allSelected;
                }
            }

            if (!tier.Foldout)
            {
                return;
            }

            for (int i = 0; i < tier.Entries.Count; i++)
            {
                DrawEntry(tier.Entries[i]);
            }
        }

        /// <summary>绘制单条纹理：勾选框、名称（点击定位）、源尺寸、体积、导入上限、引用数</summary>
        private void DrawEntry(TextureEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16f);
                entry.Selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(16f));

                if (GUILayout.Button(new GUIContent(entry.FileName, "点击在 Project 窗口定位"), nameStyle,
                    GUILayout.Width(190f)))
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(entry.AssetPath);
                    if (asset != null)
                    {
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }

                GUILayout.Label(string.Format("{0}x{1}", entry.SourceWidth, entry.SourceHeight), EditorStyles.miniLabel,
                    GUILayout.Width(80f));

                GUILayout.Label(FormatBytes(entry.FileSize), EditorStyles.miniLabel, GUILayout.Width(64f));

                // 导入上限不小于源尺寸时不会截断，显示横线避免干扰
                int maxSide = Mathf.Max(entry.SourceWidth, entry.SourceHeight);
                string importText = entry.ImportMaxSize > 0 && entry.ImportMaxSize < maxSide
                    ? string.Format("导入≤{0}", entry.ImportMaxSize)
                    : "—";
                GUILayout.Label(new GUIContent(importText, "TextureImporter 的 maxTextureSize，导入后会按它截断"),
                    EditorStyles.miniLabel, GUILayout.Width(72f));

                if (!entry.MipmapEnabled)
                {
                    GUILayout.Label(new GUIContent("无Mip", "导入设置里已关闭 mipmap"), EditorStyles.miniLabel,
                        GUILayout.Width(44f));
                }

                if (entry.MultipleSprite)
                {
                    GUILayout.Label(new GUIContent("多Sprite", "切图型纹理，缩放后切图区域会失配"), EditorStyles.miniLabel,
                        GUILayout.Width(52f));
                }

                string referenceText = entry.Referencers.Count > 0
                    ? string.Format("引用 {0}", entry.Referencers.Count)
                    : "无引用";
                string tooltip = entry.Referencers.Count > 0
                    ? string.Join("\n", entry.Referencers.ToArray())
                    : "没有在扫描目录中找到引用它的资源";
                GUILayout.Label(new GUIContent(referenceText, tooltip), EditorStyles.miniLabel, GUILayout.Width(52f));

                if (entry.Referencers.Count > 0)
                {
                    GUILayout.Label(Path.GetFileName(entry.Referencers[0]), EditorStyles.miniLabel,
                        GUILayout.Width(160f));
                }
            }
        }

        /// <summary>底部压缩设置与执行区</summary>
        private void DrawBottomBar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("分辨率百分比", GUILayout.Width(78f));
                    percent = EditorGUILayout.IntSlider(percent, 5, 100);

                    if (GUILayout.Button("25%", EditorStyles.miniButton, GUILayout.Width(40f)))
                    {
                        percent = 25;
                    }

                    if (GUILayout.Button("50%", EditorStyles.miniButton, GUILayout.Width(40f)))
                    {
                        percent = 50;
                    }

                    if (GUILayout.Button("75%", EditorStyles.miniButton, GUILayout.Width(40f)))
                    {
                        percent = 75;
                    }
                }

                GUILayout.Label(BuildSizePreview(), EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Import MaxSize",
                            "写入 TextureImporter.maxTextureSize。只改导入设置，不动源图文件，不需要备份"),
                        GUILayout.Width(96f));
                    int maxSizeIndex = IndexOfImportMaxSize(importMaxSize);
                    maxSizeIndex = EditorGUILayout.Popup(maxSizeIndex, ImportMaxSizeLabels, GUILayout.Width(88f));
                    importMaxSize = ImportMaxSizeOptions[Mathf.Clamp(maxSizeIndex, 0, ImportMaxSizeOptions.Length - 1)];

                    disableMipmap = EditorGUILayout.ToggleLeft(
                        new GUIContent("关闭 Mipmap", "勾选后把选中纹理的 mipmap 关掉"), disableMipmap,
                        GUILayout.Width(96f));

                    if (GUILayout.Button("应用导入设置到选中项", GUILayout.Width(160f)))
                    {
                        ApplyImportSettings();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Label(BuildImportPreview(), EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    backupBeforeResize = EditorGUILayout.ToggleLeft("压缩前备份原图", backupBeforeResize,
                        GUILayout.Width(120f));

                    using (new EditorGUI.DisabledScope(!backupBeforeResize))
                    {
                        backupRoot = EditorGUILayout.TextField(backupRoot);
                        if (GUILayout.Button("浏览", EditorStyles.miniButton, GUILayout.Width(48f)))
                        {
                            string selected = EditorUtility.OpenFolderPanel("选择备份目录", backupRoot, string.Empty);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                backupRoot = selected;
                                EditorPrefs.SetString(PrefBackupRoot, backupRoot);
                            }
                        }

                        overwriteBackup = EditorGUILayout.ToggleLeft(
                            new GUIContent("覆盖已有备份", "默认不覆盖，避免二次压缩把已缩小的图当成原图"),
                            overwriteBackup, GUILayout.Width(96f));
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("压缩选中项", GUILayout.Height(28f)))
                    {
                        ResizeSelected();
                    }

                    if (GUILayout.Button("从备份恢复选中项", GUILayout.Height(28f)))
                    {
                        RestoreSelected();
                    }

                    if (GUILayout.Button("打开备份目录", GUILayout.Height(28f), GUILayout.Width(110f)))
                    {
                        OpenBackupFolder();
                    }
                }

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, statusType);
                }
            }
        }

        /// <summary>用第一条勾选的纹理给出缩放前后的尺寸示例</summary>
        private string BuildSizePreview()
        {
            int count = 0;
            long limitedCount = 0;
            string sample = "勾选纹理后可预览目标尺寸";
            for (int i = 0; i < tiers.Count; i++)
            {
                for (int j = 0; j < tiers[i].Entries.Count; j++)
                {
                    TextureEntry entry = tiers[i].Entries[j];
                    if (!entry.Selected)
                    {
                        continue;
                    }

                    count++;
                    int width;
                    int height;
                    GetTargetSize(entry, out width, out height);
                    if (IsImportLimited(entry, width, height))
                    {
                        limitedCount++;
                    }

                    if (count == 1)
                    {
                        sample = string.Format("示例：{0}  {1}x{2} → {3}x{4}", entry.FileName, entry.SourceWidth,
                            entry.SourceHeight, width, height);
                    }
                }
            }

            if (limitedCount > 0)
            {
                sample += string.Format("；其中 {0} 张受导入上限限制，实际纹理尺寸可能不变", limitedCount);
            }

            return sample;
        }

        /// <summary>扫描：收集目录内资源引用的纹理，按尺寸分档并排序</summary>
        private void Scan()
        {
            if (!AssetDatabase.IsValidFolder(scanRoot))
            {
                SetStatus(string.Format("目录不存在：{0}", scanRoot), MessageType.Error);
                return;
            }

            sizeThreshold = Mathf.Max(16, sizeThreshold);
            ClearResults();
            statusMessage = string.Empty;

            Dictionary<string, bool> typeCache = new Dictionary<string, bool>();
            Dictionary<string, List<string>> referenceMap = new Dictionary<string, List<string>>();
            List<string> candidates = CollectReferenceCandidates();

            bool canceled = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("扫描纹理引用", candidates[i],
                        (i + 1f) / candidates.Count))
                {
                    canceled = true;
                    break;
                }

                string[] dependencies = AssetDatabase.GetDependencies(candidates[i], true);
                for (int d = 0; d < dependencies.Length; d++)
                {
                    string dependency = dependencies[d];
                    if (!IsTextureAsset(dependency, typeCache))
                    {
                        continue;
                    }

                    List<string> referencers;
                    if (!referenceMap.TryGetValue(dependency, out referencers))
                    {
                        referencers = new List<string>();
                        referenceMap[dependency] = referencers;
                    }

                    if (referencers.Count < 12 && !referencers.Contains(candidates[i]))
                    {
                        referencers.Add(candidates[i]);
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // 目录内自带的纹理即使没被任何资源引用（走 Addressable 名字加载）也要列出
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { scanRoot });
            for (int i = 0; i < textureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                if (!string.IsNullOrEmpty(path) && !referenceMap.ContainsKey(path))
                {
                    referenceMap[path] = new List<string>();
                }
            }

            List<int> bounds = BuildTierBounds(sizeThreshold);
            for (int i = 0; i < bounds.Count; i++)
            {
                tiers.Add(new TextureTier
                {
                    Min = bounds[i],
                    Max = i == 0 ? int.MaxValue : bounds[i - 1] - 1,
                    Label = i == 0
                        ? string.Format("≥ {0}", bounds[i])
                        : string.Format("{0} ~ {1}", bounds[i], bounds[i - 1] - 1)
                });
            }

            foreach (KeyValuePair<string, List<string>> pair in referenceMap)
            {
                TextureEntry entry = BuildEntry(pair.Key, pair.Value);
                if (entry == null)
                {
                    continue;
                }

                int maxSide = Mathf.Max(entry.SourceWidth, entry.SourceHeight);
                int tierIndex = FindTierIndex(maxSide);
                if (tierIndex >= 0)
                {
                    tiers[tierIndex].Entries.Add(entry);
                }
            }

            for (int i = 0; i < tiers.Count; i++)
            {
                tiers[i].Entries.Sort(CompareBySize);
            }

            tiers.RemoveAll(tier => tier.Entries.Count == 0);
            scanned = true;

            int total = 0;
            for (int i = 0; i < tiers.Count; i++)
            {
                total += tiers[i].Entries.Count;
            }

            SetStatus(canceled
                    ? string.Format("扫描被中断，已找到 {0} 张大纹理。", total)
                    : string.Format("扫描完成：{0} 张 ≥{1}px 的纹理，来自 {2} 个资源。", total, sizeThreshold,
                        candidates.Count),
                canceled ? MessageType.Warning : MessageType.Info);
            Repaint();
        }

        /// <summary>收集扫描目录内可能引用纹理的资源路径</summary>
        private List<string> CollectReferenceCandidates()
        {
            List<string> candidates = new List<string>();
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { scanRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                if (ReferenceExtensions.Contains(Path.GetExtension(path)))
                {
                    candidates.Add(path);
                }
            }

            return candidates;
        }

        /// <summary>把一条纹理路径转成列表条目，不满足阈值或格式不支持时返回 null</summary>
        private TextureEntry BuildEntry(string assetPath, List<string> referencers)
        {
            if (!TextureFileCodec.IsSupported(assetPath))
            {
                return null;
            }

            int width;
            int height;
            bool hasAlpha;
            if (!TextureFileCodec.TryReadInfo(assetPath, out width, out height, out hasAlpha))
            {
                return null;
            }

            if (Mathf.Max(width, height) < sizeThreshold)
            {
                return null;
            }

            FileInfo info = new FileInfo(Path.GetFullPath(assetPath));
            if (!info.Exists)
            {
                return null;
            }

            TextureEntry entry = new TextureEntry
            {
                AssetPath = assetPath,
                FileName = Path.GetFileName(assetPath),
                SourceWidth = width,
                SourceHeight = height,
                FileSize = info.Length,
                Referencers = referencers
            };

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                entry.ImportMaxSize = importer.maxTextureSize;
                entry.MipmapEnabled = importer.mipmapEnabled;
                entry.MultipleSprite = importer.spriteImportMode == SpriteImportMode.Multiple;
            }

            return entry;
        }

        /// <summary>档位下限：从 8192 逐级折半到阈值，倒数第二档之下再补一个阈值档</summary>
        private static List<int> BuildTierBounds(int threshold)
        {
            List<int> bounds = new List<int>();
            for (int value = 8192; value > threshold; value /= 2)
            {
                bounds.Add(value);
            }

            bounds.Add(threshold);
            return bounds;
        }

        private int FindTierIndex(int maxSide)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (maxSide >= tiers[i].Min && maxSide <= tiers[i].Max)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>按文件体积降序，体积相同按路径排序保证结果稳定</summary>
        private static int CompareBySize(TextureEntry a, TextureEntry b)
        {
            int result = b.FileSize.CompareTo(a.FileSize);
            return result != 0 ? result : string.CompareOrdinal(a.AssetPath, b.AssetPath);
        }

        /// <summary>当前导入上限在选项数组里的下标，非法值退回「不修改」</summary>
        private static int IndexOfImportMaxSize(int value)
        {
            int index = Array.IndexOf(ImportMaxSizeOptions, value);
            return index >= 0 ? index : 0;
        }

        /// <summary>导入设置那一行的说明文字：会改什么、影响多少张</summary>
        private string BuildImportPreview()
        {
            if (importMaxSize <= 0 && !disableMipmap)
            {
                return "导入设置：未选择修改项（Import MaxSize 为「不修改」且未勾选「关闭 Mipmap」）。";
            }

            List<string> changes = new List<string>();
            if (importMaxSize > 0)
            {
                changes.Add(string.Format("Import MaxSize = {0}", importMaxSize));
            }

            if (disableMipmap)
            {
                changes.Add("关闭 Mipmap");
            }

            return string.Format("导入设置：{0}；只改 .meta 与导入结果，不动源图。当前已勾选 {1} 张。",
                string.Join("、", changes.ToArray()), CollectSelected().Count);
        }

        /// <summary>
        /// 把导入上限与 mipmap 设置写到勾选的纹理上：只改 TextureImporter 并重新导入，
        /// 不覆盖源文件，所以不受备份开关影响，也不需要"从备份恢复"。
        /// </summary>
        private void ApplyImportSettings()
        {
            EditorPrefs.SetInt(PrefImportMaxSize, importMaxSize);
            EditorPrefs.SetBool(PrefDisableMipmap, disableMipmap);

            if (importMaxSize <= 0 && !disableMipmap)
            {
                SetStatus("Import MaxSize 为「不修改」且未勾选「关闭 Mipmap」，没有需要应用的设置。",
                    MessageType.Warning);
                return;
            }

            List<TextureEntry> targets = CollectSelected();
            if (targets.Count == 0)
            {
                SetStatus("没有勾选任何纹理。", MessageType.Warning);
                return;
            }

            List<string> changes = new List<string>();
            if (importMaxSize > 0)
            {
                changes.Add(string.Format("Import MaxSize 设为 {0}", importMaxSize));
            }

            if (disableMipmap)
            {
                changes.Add("关闭 Mipmap");
            }

            string message = string.Format("将对 {0} 张纹理执行：{1}。\n\n只修改导入设置并重新导入，不动源图文件，不需要备份。",
                targets.Count, string.Join("、", changes.ToArray()));
            if (!EditorUtility.DisplayDialog("应用导入设置", message, "应用", "取消"))
            {
                return;
            }

            int success = 0;
            List<string> failures = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    TextureEntry entry = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("应用导入设置", entry.AssetPath,
                            (i + 1f) / targets.Count))
                    {
                        break;
                    }

                    TextureImporter importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
                    if (importer == null)
                    {
                        failures.Add(string.Format("{0}：不是 TextureImporter", entry.FileName));
                        continue;
                    }

                    if (importMaxSize > 0)
                    {
                        importer.maxTextureSize = importMaxSize;
                    }

                    if (disableMipmap)
                    {
                        importer.mipmapEnabled = false;
                    }

                    //立刻重导入, 保证回读的导入上限/mipmap 状态与列表一致
                    importer.SaveAndReimport();
                    entry.ImportMaxSize = importer.maxTextureSize;
                    entry.MipmapEnabled = importer.mipmapEnabled;
                    success++;
                }
            }
            catch (Exception e)
            {
                failures.Add("处理中断：" + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            ReportResult("应用导入设置", success, failures);
        }

        /// <summary>对勾选的纹理执行压缩</summary>
        private void ResizeSelected()
        {
            EditorPrefs.SetInt(PrefPercent, percent);

            List<TextureEntry> targets = CollectSelected();
            if (targets.Count == 0)
            {
                SetStatus("没有勾选任何纹理。", MessageType.Warning);
                return;
            }

            if (backupBeforeResize && string.IsNullOrEmpty(backupRoot))
            {
                SetStatus("请先指定备份目录，或关闭「压缩前备份原图」。", MessageType.Warning);
                return;
            }

            if (backupBeforeResize && IsBackupRootInsideAssets())
            {
                SetStatus("备份目录不能放在 Assets 下，否则备份图会被当成资源一起打包。请换到工程外的目录。",
                    MessageType.Error);
                return;
            }

            int spriteCount = 0;
            int limitedCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].MultipleSprite)
                {
                    spriteCount++;
                }

                int width;
                int height;
                GetTargetSize(targets[i], out width, out height);
                if (IsImportLimited(targets[i], width, height))
                {
                    limitedCount++;
                }
            }

            string message = string.Format("将把 {0} 张纹理缩放到 {1}%，并覆盖写回源文件。", targets.Count, percent);
            message += backupBeforeResize
                ? "\n\n压缩前备份到：\n" + backupRoot
                : "\n\n未开启备份：原图会被直接覆盖，无法还原。";
            message += "\n\n提示：TGA 写回无压缩真彩（体积随分辨率下降），PNG 重新编码较慢，大图请耐心等待进度条。";
            if (spriteCount > 0)
            {
                message += string.Format("\n\n注意：{0} 张是多 Sprite 切图，缩放后切图区域可能错位。", spriteCount);
            }

            if (limitedCount > 0)
            {
                message += string.Format("\n注意：{0} 张的导入上限低于目标尺寸，压缩后实际纹理尺寸可能不变。",
                    limitedCount);
            }

            if (!EditorUtility.DisplayDialog("压缩纹理", message, "开始压缩", "取消"))
            {
                return;
            }

            List<TextureEntry> changed = new List<TextureEntry>();
            List<string> failures = new List<string>();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    TextureEntry entry = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("压缩纹理", entry.AssetPath,
                            (i + 1f) / targets.Count))
                    {
                        break;
                    }

                    string error;
                    if (backupBeforeResize &&
                        !TextureFileCodec.Backup(entry.AssetPath, backupRoot, overwriteBackup, out error))
                    {
                        failures.Add(string.Format("{0}：备份失败（{1}）", entry.FileName, error));
                        continue;
                    }

                    int newWidth;
                    int newHeight;
                    if (!TextureFileCodec.TryResize(entry.AssetPath, percent, out newWidth, out newHeight, out error))
                    {
                        failures.Add(string.Format("{0}：{1}", entry.FileName, error));
                        continue;
                    }

                    changed.Add(entry);
                }
            }
            catch (Exception e)
            {
                failures.Add("处理中断：" + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            for (int i = 0; i < changed.Count; i++)
            {
                AssetDatabase.ImportAsset(changed[i].AssetPath, ImportAssetOptions.ForceUpdate);
                RefreshEntryInfo(changed[i]);
            }

            AssetDatabase.Refresh();
            ReportResult(string.Format("压缩 {0}%", percent), changed.Count, failures);
        }

        /// <summary>用备份覆盖回勾选的纹理</summary>
        private void RestoreSelected()
        {
            List<TextureEntry> targets = CollectSelected();
            if (targets.Count == 0)
            {
                SetStatus("没有勾选任何纹理。", MessageType.Warning);
                return;
            }

            if (!EditorUtility.DisplayDialog("从备份恢复",
                    string.Format("将用备份覆盖 {0} 张纹理的源文件。\n备份目录：\n{1}", targets.Count, backupRoot),
                    "开始恢复", "取消"))
            {
                return;
            }

            List<TextureEntry> changed = new List<TextureEntry>();
            List<string> failures = new List<string>();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    TextureEntry entry = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("恢复纹理", entry.AssetPath,
                            (i + 1f) / targets.Count))
                    {
                        break;
                    }

                    string error;
                    if (!TextureFileCodec.Restore(entry.AssetPath, backupRoot, out error))
                    {
                        failures.Add(string.Format("{0}：{1}", entry.FileName, error));
                        continue;
                    }

                    changed.Add(entry);
                }
            }
            catch (Exception e)
            {
                failures.Add("处理中断：" + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            for (int i = 0; i < changed.Count; i++)
            {
                AssetDatabase.ImportAsset(changed[i].AssetPath, ImportAssetOptions.ForceUpdate);
                RefreshEntryInfo(changed[i]);
            }

            AssetDatabase.Refresh();
            ReportResult("从备份恢复", changed.Count, failures);
        }

        /// <summary>统一输出处理结果</summary>
        private void ReportResult(string action, int successCount, List<string> failures)
        {
            string message = string.Format("{0}完成：成功 {1} 张", action, successCount);
            if (failures.Count > 0)
            {
                message += string.Format("，失败 {0} 张：\n{1}", failures.Count,
                    string.Join("\n", failures.GetRange(0, Mathf.Min(failures.Count, 8)).ToArray()));
                if (failures.Count > 8)
                {
                    message += "\n……完整列表见 Console";
                }

                Debug.LogWarning("[大纹理工具] " + action + "\n" + string.Join("\n", failures.ToArray()));
                SetStatus(message, MessageType.Warning);
            }
            else
            {
                SetStatus(message, MessageType.Info);
            }

            Repaint();
        }

        /// <summary>备份目录是否落在 Assets 内（会被导入并打进包体）</summary>
        private bool IsBackupRootInsideAssets()
        {
            string root = Path.GetFullPath(backupRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            return root.StartsWith(assets, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>压缩后重新读取源尺寸与文件体积</summary>
        private static void RefreshEntryInfo(TextureEntry entry)
        {
            int width;
            int height;
            bool hasAlpha;
            if (TextureFileCodec.TryReadInfo(entry.AssetPath, out width, out height, out hasAlpha))
            {
                entry.SourceWidth = width;
                entry.SourceHeight = height;
            }

            FileInfo info = new FileInfo(Path.GetFullPath(entry.AssetPath));
            if (info.Exists)
            {
                entry.FileSize = info.Length;
            }
        }

        private void OpenBackupFolder()
        {
            if (string.IsNullOrEmpty(backupRoot))
            {
                SetStatus("还没有指定备份目录。", MessageType.Warning);
                return;
            }

            Directory.CreateDirectory(backupRoot);
            EditorUtility.RevealInFinder(backupRoot);
        }

        private List<TextureEntry> CollectSelected()
        {
            List<TextureEntry> selected = new List<TextureEntry>();
            for (int i = 0; i < tiers.Count; i++)
            {
                for (int j = 0; j < tiers[i].Entries.Count; j++)
                {
                    if (tiers[i].Entries[j].Selected)
                    {
                        selected.Add(tiers[i].Entries[j]);
                    }
                }
            }

            return selected;
        }

        /// <summary>按百分比算出目标尺寸，与服务端的实际缩放逻辑保持一致</summary>
        private void GetTargetSize(TextureEntry entry, out int width, out int height)
        {
            float scale = Mathf.Clamp(percent, 1, 100) / 100f;
            width = Mathf.Max(TextureFileCodec.MinSide, Mathf.RoundToInt(entry.SourceWidth * scale));
            height = Mathf.Max(TextureFileCodec.MinSide, Mathf.RoundToInt(entry.SourceHeight * scale));
        }

        /// <summary>目标尺寸超过导入上限时，导入后的纹理不会真的变小</summary>
        private static bool IsImportLimited(TextureEntry entry, int targetWidth, int targetHeight)
        {
            return entry.ImportMaxSize > 0 && Mathf.Max(targetWidth, targetHeight) > entry.ImportMaxSize;
        }

        private static bool IsTextureAsset(string path, Dictionary<string, bool> cache)
        {
            bool result;
            if (cache.TryGetValue(path, out result))
            {
                return result;
            }

            result = AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D);
            cache[path] = result;
            return result;
        }

        private void SetAllFoldout(bool foldout)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                tiers[i].Foldout = foldout;
            }
        }

        private void ClearResults()
        {
            tiers.Clear();
            scanned = false;
            Repaint();
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L)
            {
                return string.Format("{0:F1} MB", bytes / (1024f * 1024f));
            }

            if (bytes >= 1024L)
            {
                return string.Format("{0:F0} KB", bytes / 1024f);
            }

            return string.Format("{0} B", bytes);
        }
    }
}
