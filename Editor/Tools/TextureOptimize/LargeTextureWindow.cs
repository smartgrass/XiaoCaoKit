using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private const string PrefMipmapMode = "XCTools.LargeTexture.MipmapMode";

        /// <summary>「记录到配置」勾选状态的 EditorPrefs 键</summary>
        private const string PrefRecordToConfig = "XCTools.LargeTexture.RecordToConfig";

        /// <summary>「处理后自动取消勾选」的 EditorPrefs 键</summary>
        private const string PrefClearSelectionAfterProcess = "XCTools.LargeTexture.ClearSelectionAfterProcess";

        /// <summary>列表显示筛选的 EditorPrefs 键</summary>
        private const string PrefDisplayFilter = "XCTools.LargeTexture.DisplayFilter";

        /// <summary>MaxSize 筛选的 EditorPrefs 键</summary>
        private const string PrefMaxSizeFilter = "XCTools.LargeTexture.MaxSizeFilter";

        /// <summary>每条纹理最多记录多少个引用者（tooltip 与右键选中都受这个上限约束）</summary>
        private const int MaxReferencers = 12;

        /// <summary>TextureImporter.maxTextureSize 的 Unity 默认值，值不等于它说明被改过</summary>
        private const int DefaultImportMaxSize = 2048;

        /// <summary>筛选下拉的显示文案，顺序与 DisplayFilter 的取值对应</summary>
        private static readonly string[] DisplayFilterLabels = { "显示所有", "显示未处理", "显示已处理" };

        /// <summary>MaxSize 筛选下拉的显示文案，顺序与 MaxSizeFilter 的取值对应</summary>
        private static readonly string[] MaxSizeFilterLabels = { "不限", "未配置MaxSize", "已配置MaxSize" };

        /// <summary>列表显示筛选：按目录处理状态过滤条目</summary>
        private enum DisplayFilter
        {
            All = 0,

            /// <summary>只显示还没登记过的纹理（含部分处理的目录）</summary>
            Unprocessed = 1,

            /// <summary>只显示配置里已登记的纹理</summary>
            Processed = 2
        }

        /// <summary>按 MaxSize 是否配置过筛选条目</summary>
        private enum MaxSizeFilter
        {
            All = 0,

            /// <summary>只显示还没配置 MaxSize 的纹理</summary>
            NotConfigured = 1,

            /// <summary>只显示已配置过 MaxSize 的纹理</summary>
            Configured = 2
        }

        /// <summary>目录处理记录文件：放在工具所在的 Editor 目录，随工程进版本库，不会被打进包体</summary>
        private const string ConfigPath = "Assets/XiaoCaoKit/Editor/Tools/TextureOptimize/TextureOptimizeConfig.json";

        /// <summary>旧版本只有「关闭 Mipmap」勾选框时用的键，仅用于一次性迁移</summary>
        private const string PrefDisableMipmapLegacy = "XCTools.LargeTexture.DisableMipmap";
        private const string PrefDirectoryLevel = "XCTools.LargeTexture.DirectoryLevel";

        /// <summary>可选的导入上限，0 表示不修改；顺序与 ImportMaxSizeLabels 对应</summary>
        private static readonly int[] ImportMaxSizeOptions = { 0, 8192, 4096, 2048, 1024, 512, 256, 128, 64, 32 };
        private static readonly string[] ImportMaxSizeLabels =
        {
            "不修改", "8192", "4096", "2048", "1024", "512", "256", "128", "64", "32"
        };

        /// <summary>mipmap 处理方式的显示文案，顺序与 MipmapMode 的取值对应</summary>
        private static readonly string[] MipmapModeLabels = { "不变", "关闭", "开启" };

        /// <summary>应用导入设置时对 mipmap 的处理方式</summary>
        private enum MipmapMode
        {
            /// <summary>保持每张图原本的设置</summary>
            Keep = 0,

            /// <summary>全部关掉 mipmap</summary>
            Disable = 1,

            /// <summary>全部打开 mipmap</summary>
            Enable = 2
        }

        /// <summary>这些扩展名的资源才可能引用到纹理，避免对音频/脚本做无用的依赖查询</summary>
        private static readonly HashSet<string> ReferenceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab", ".mat", ".asset", ".controller", ".overridecontroller", ".anim", ".unity",
            ".fbx", ".obj", ".blend", ".playable", ".spriteatlas", ".mask", ".rendertexture", ".shadergraph"
        };

        private string scanRoot = DefaultScanRoot;
        private int sizeThreshold = 512;

        /// <summary>目录分级：0 不按目录分组；1 以扫描目录下第 1 层目录为分组，2 取前两层，依此类推</summary>
        private int directoryLevel;
        private Vector2 scrollPosition;

        /// <summary>扫描结果全集，改「目录分级」时据此重排，无需重新扫描</summary>
        private List<TextureEntry> allEntries = new List<TextureEntry>();

        /// <summary>列表结构：目录组（一级）→ 尺寸档（二级）→ 条目</summary>
        private List<DirectoryGroup> groups = new List<DirectoryGroup>();

        /// <summary>当前阈值算出的档位下限，重排分组时据此给每个目录重建档位</summary>
        private List<int> tierBounds = new List<int>();
        private bool scanned;
        private int percent = 50;
        private bool backupBeforeResize = true;

        /// <summary>要写入 TextureImporter.maxTextureSize 的值，0 表示不动</summary>
        private int importMaxSize;
        /// <summary>要写入 TextureImporter.mipmapEnabled 的方式，Keep 表示不动</summary>
        private MipmapMode mipmapMode = MipmapMode.Keep;

        /// <summary>列表显示筛选（只影响展示，不影响勾选状态与实际处理范围）</summary>
        private DisplayFilter displayFilter = DisplayFilter.All;

        /// <summary>MaxSize 筛选，与处理状态筛选叠加生效</summary>
        private MaxSizeFilter maxSizeFilter = MaxSizeFilter.All;

        /// <summary>
        /// 需要在下一帧 Layout 时重排列表。勾选变化会改变「已勾选项不参与筛选」的显示范围，
        /// 但不能在绘制过程中重建 groups（控件数量会错位），所以推迟到下一次 Layout。
        /// </summary>
        private bool pendingRebuild;

        /// <summary>应用导入设置时，是否把选中项所在目录的处理方式写入配置文件</summary>
        private bool recordToConfig = true;

        /// <summary>压缩 / 应用导入设置 / 从备份恢复成功后，是否自动取消这些条目的勾选</summary>
        private bool clearSelectionAfterProcess = true;

        /// <summary>目录处理记录，首次用到时从 ConfigPath 载入</summary>
        private ConfigData configData;
        private bool overwriteBackup;
        private string backupRoot;
        private string statusMessage = "选择目录后点击「扫描」，列表按尺寸分档显示，组内按文件体积从大到小排列；目录分级设为 1 可再按一级目录分组。";
        private MessageType statusType = MessageType.Info;
        private GUIStyle nameStyle;
        private GUIStyle boldFoldoutStyle;

        /// <summary>GUIStyle 是否已按当前 skin 建好；坏掉时会一直重试</summary>
        private bool stylesReady;
        private bool stylesFailedLogged;

        /// <summary>一条纹理记录（路径 + 源尺寸 + 被引用情况）</summary>
        private sealed class TextureEntry
        {
            public string AssetPath;

            /// <summary>资源 GUID，配置文件里用它在不同机器/重命名后仍能对上</summary>
            public string Guid;

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

        /// <summary>一级目录分组：目录名 + 组内若干尺寸档</summary>
        private sealed class DirectoryGroup
        {
            public string Label;

            /// <summary>该组的完整目录路径（如 Assets/_Res/Sprite），配置文件按它记录处理状态</summary>
            public string DirectoryPath;

            public bool IsRoot;
            public bool Foldout = true;
            public List<TextureTier> Tiers = new List<TextureTier>();

            /// <summary>该目录下的纹理总数（不受筛选影响）</summary>
            public int DirectoryTotal;

            /// <summary>该目录下已登记处理的纹理数（不受筛选影响）</summary>
            public int DirectoryProcessed;

            /// <summary>组内条目总数</summary>
            public int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < Tiers.Count; i++)
                    {
                        count += Tiers[i].Entries.Count;
                    }

                    return count;
                }
            }

            /// <summary>组内已勾选条目数</summary>
            public int SelectedCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < Tiers.Count; i++)
                    {
                        count += Tiers[i].SelectedCount;
                    }

                    return count;
                }
            }

            /// <summary>组内文件体积合计</summary>
            public long TotalSize
            {
                get
                {
                    long total = 0;
                    for (int i = 0; i < Tiers.Count; i++)
                    {
                        total += Tiers[i].TotalSize;
                    }

                    return total;
                }
            }
        }

        /// <summary>配置文件的根对象：一张「目录 → 处理记录」的表，用 JsonUtility 存成 json</summary>
        [Serializable]
        private sealed class ConfigData
        {
            public int Version = 1;
            public List<ConfigDirectory> Directories = new List<ConfigDirectory>();

            /// <summary>按完整目录路径查记录，找不到返回 null</summary>
            public ConfigDirectory Find(string directory)
            {
                for (int i = 0; i < Directories.Count; i++)
                {
                    if (string.Equals(Directories[i].Directory, directory, StringComparison.Ordinal))
                    {
                        return Directories[i];
                    }
                }

                return null;
            }
        }

        /// <summary>单个目录的处理记录：上次应用的导入参数 + 已处理过的纹理</summary>
        [Serializable]
        private sealed class ConfigDirectory
        {
            /// <summary>完整目录路径，如 Assets/_Res/Sprite</summary>
            public string Directory;

            /// <summary>上次写入的 maxTextureSize，0 表示没有指定过</summary>
            public int MaxSize;

            /// <summary>上次写入的 mipmap 方式：0 不变 / 1 关闭 / 2 开启</summary>
            public int Mipmap;

            /// <summary>写入当时该目录是否已全部处理完（快照；界面状态按 Files 与当前条目实时比对）</summary>
            public bool Processed;

            /// <summary>最后写入时间</summary>
            public string UpdatedAt;

            /// <summary>已处理纹理的 GUID 列表</summary>
            public List<string> Files = new List<string>();

            /// <summary>Files 的查询缓存，不参与序列化</summary>
            private HashSet<string> fileSet;

            /// <summary>该纹理是否已登记为处理过</summary>
            public bool Contains(string guid)
            {
                return GetFileSet().Contains(guid);
            }

            /// <summary>登记一张已处理的纹理</summary>
            public void AddFile(string guid)
            {
                if (string.IsNullOrEmpty(guid) || GetFileSet().Contains(guid))
                {
                    return;
                }

                Files.Add(guid);
                fileSet.Add(guid);
            }

            /// <summary>清空已处理列表</summary>
            public void ClearFiles()
            {
                Files.Clear();
                if (fileSet != null)
                {
                    fileSet.Clear();
                }
            }

            private HashSet<string> GetFileSet()
            {
                if (fileSet == null)
                {
                    fileSet = new HashSet<string>(Files, StringComparer.Ordinal);
                }

                return fileSet;
            }
        }

        /// <summary>目录处理状态：未处理 / 包含已处理（部分）/ 已处理</summary>
        private enum DirectoryState
        {
            Untouched = 0,
            Partial = 1,
            Done = 2
        }

        [MenuItem(MenuPath, priority = 133)]
        public static void Open()
        {
            LargeTextureWindow window = GetWindow<LargeTextureWindow>(WindowTitle);
            window.minSize = new Vector2(960f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(960f, 560f);
            percent = Mathf.Clamp(EditorPrefs.GetInt(PrefPercent, 50), 5, 100);

            importMaxSize = EditorPrefs.GetInt(PrefImportMaxSize, 0);
            if (Array.IndexOf(ImportMaxSizeOptions, importMaxSize) < 0)
            {
                importMaxSize = 0;
            }

            if (EditorPrefs.HasKey(PrefMipmapMode))
            {
                mipmapMode = (MipmapMode)EditorPrefs.GetInt(PrefMipmapMode, (int)MipmapMode.Keep);
                if (mipmapMode != MipmapMode.Keep && mipmapMode != MipmapMode.Disable && mipmapMode != MipmapMode.Enable)
                {
                    mipmapMode = MipmapMode.Keep;
                }
            }
            else
            {
                // 迁移旧版本的「关闭 Mipmap」勾选状态
                mipmapMode = EditorPrefs.GetBool(PrefDisableMipmapLegacy, false)
                    ? MipmapMode.Disable
                    : MipmapMode.Keep;
            }
            recordToConfig = EditorPrefs.GetInt(PrefRecordToConfig, 1) != 0;
            clearSelectionAfterProcess = EditorPrefs.GetInt(PrefClearSelectionAfterProcess, 1) != 0;
            displayFilter = (DisplayFilter)Mathf.Clamp(EditorPrefs.GetInt(PrefDisplayFilter, 0), 0,
                DisplayFilterLabels.Length - 1);
            maxSizeFilter = (MaxSizeFilter)Mathf.Clamp(EditorPrefs.GetInt(PrefMaxSizeFilter, 0), 0,
                MaxSizeFilterLabels.Length - 1);
            directoryLevel = Mathf.Clamp(EditorPrefs.GetInt(PrefDirectoryLevel, 0), 0, 8);

            backupRoot = EditorPrefs.GetString(PrefBackupRoot, string.Empty);
            if (string.IsNullOrEmpty(backupRoot))
            {
                backupRoot = Path.GetFullPath(DefaultBackupFolder);
            }

            // EditorStyles 不能在 OnEnable 里取：编辑器启动时恢复上次布局会在这里跑 OnEnable，
            // 此时 GUI skin 还没加载完，EditorStyles.label 内部会抛 NullReferenceException。
            // 样式统一延后到 OnGUI 里按需创建（见 EnsureStyles）。
            nameStyle = null;
            boldFoldoutStyle = null;
            stylesReady = false;
        }

        /// <summary>
        /// 在 OnGUI 里按需创建 GUIStyle。skin 未就绪时 EditorStyles.* 会抛 NRE，
        /// 这里吞掉异常并在下一帧重试，不让一次异常把整个窗口画崩。
        /// </summary>
        private bool EnsureStyles()
        {
            if (stylesReady)
            {
                return true;
            }

            try
            {
                GUIStyle label = EditorStyles.label;
                GUIStyle foldout = EditorStyles.foldout;
                if (label == null || foldout == null)
                {
                    return false;
                }

                nameStyle = new GUIStyle(label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(0, 2, 0, 0),
                    clipping = TextClipping.Clip
                };
                boldFoldoutStyle = new GUIStyle(foldout) { fontStyle = FontStyle.Bold };
                stylesReady = true;
                return true;
            }
            catch (Exception e)
            {
                nameStyle = null;
                boldFoldoutStyle = null;
                if (!stylesFailedLogged)
                {
                    stylesFailedLogged = true;
                    Debug.LogWarning("[LargeTextureWindow] 编辑器 GUI skin 尚未就绪，样式初始化已延后重试：" + e);
                }

                return false;
            }
        }

        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                return;
            }

            if (!EnsureStyles())
            {
                // 样式还没建起来（skin 未就绪），跳过这一帧即可，重绘时会自动重试
                return;
            }

            // 只允许在 Layout 阶段改列表结构：此时控件数量还没被缓存，
            // 若在鼠标事件里改，Unity 会报 "Getting control ... position in a group" 错位。
            if (pendingRebuild && Event.current.type == EventType.Layout)
            {
                pendingRebuild = false;
                RebuildGroups();
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
                scanRoot = EditorGUILayout.TextField(scanRoot, EditorStyles.toolbarTextField, GUILayout.MinWidth(140f));

                GUILayout.Label("最小边长", EditorStyles.miniLabel, GUILayout.Width(52f));
                sizeThreshold = EditorGUILayout.IntField(sizeThreshold, EditorStyles.toolbarTextField, GUILayout.Width(46f));

                GUILayout.Label(new GUIContent("目录分级", "0 = 不分组；1 = 按扫描目录下第 1 层目录分组；2 = 取前两层"),
                    EditorStyles.miniLabel, GUILayout.Width(52f));
                int nextLevel = EditorGUILayout.IntField(directoryLevel, EditorStyles.toolbarTextField,
                    GUILayout.Width(32f));
                if (nextLevel != directoryLevel)
                {
                    directoryLevel = Mathf.Clamp(nextLevel, 0, 8);
                    EditorPrefs.SetInt(PrefDirectoryLevel, directoryLevel);
                    // 分组只影响展示，已有扫描结果直接重排即可
                    if (scanned)
                    {
                        RequestRebuild();
                    }
                }

                GUILayout.Label(new GUIContent("筛选", "按处理状态过滤列表；「未处理」包含处理了一部分的目录"),
                    EditorStyles.miniLabel, GUILayout.Width(32f));
                int nextFilter = EditorGUILayout.Popup((int)displayFilter, DisplayFilterLabels,
                    EditorStyles.toolbarPopup, GUILayout.Width(96f));
                if (nextFilter != (int)displayFilter)
                {
                    displayFilter = (DisplayFilter)nextFilter;
                    EditorPrefs.SetInt(PrefDisplayFilter, (int)displayFilter);
                    // 筛选同样只影响展示，直接按已有扫描结果重排
                    if (scanned)
                    {
                        RequestRebuild();
                    }
                }

                GUILayout.Label(new GUIContent("MaxSize",
                        "按 MaxSize 是否配置过过滤：「已配置」= 导入上限不是 Unity 默认的 2048，或配置记录里写过该图"),
                    EditorStyles.miniLabel, GUILayout.Width(46f));
                int nextMaxSizeFilter = EditorGUILayout.Popup((int)maxSizeFilter, MaxSizeFilterLabels,
                    EditorStyles.toolbarPopup, GUILayout.Width(100f));
                if (nextMaxSizeFilter != (int)maxSizeFilter)
                {
                    maxSizeFilter = (MaxSizeFilter)nextMaxSizeFilter;
                    EditorPrefs.SetInt(PrefMaxSizeFilter, (int)maxSizeFilter);
                    if (scanned)
                    {
                        RequestRebuild();
                    }
                }

                if (GUILayout.Button("扫描", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Scan();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("取消选中", "清除所有勾选（含被折叠、被筛选隐藏的条目）"),
                        EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    ClearSelection();
                }

                // 一个按钮切换全部展开/折叠：当前已全部展开就折叠，否则展开
                bool allExpanded = IsAllExpanded();
                if (GUILayout.Button(new GUIContent(allExpanded ? "全部折叠" : "全部展开",
                            "一键切换：当前已全部展开时折叠所有分组与档位，否则全部展开"),
                        EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    SetAllFoldout(!allExpanded);
                }
            }
        }

        /// <summary>清除所有勾选：遍历扫描全集，折叠中和被筛选隐藏的条目也一并取消</summary>
        private void ClearSelection()
        {
            int count = 0;
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].Selected)
                {
                    allEntries[i].Selected = false;
                    count++;
                }
            }

            if (count == 0)
            {
                SetStatus("当前没有勾选的纹理。", MessageType.Info);
                Repaint();
                return;
            }

            // 有筛选时列表内容会变（取消勾选后不满足条件的行该消失），走延迟重排
            NotifySelectionChanged();
            SetStatus(string.Format("已取消 {0} 张纹理的勾选。", count), MessageType.Info);
            Repaint();
        }

        /// <summary>统计行：视图内的数量与体积、全场勾选情况（已勾选项始终显示，不会被筛选隐藏）</summary>
        private void DrawSummary()
        {
            int viewCount = 0;
            long viewSize = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                viewCount += group.Count;
                viewSize += group.TotalSize;
            }

            int selectedCount = 0;
            long selectedSize = 0;
            for (int i = 0; i < allEntries.Count; i++)
            {
                TextureEntry entry = allEntries[i];
                if (entry.Selected)
                {
                    selectedCount++;
                    selectedSize += entry.FileSize;
                }
            }

            string viewTitle = !IsFiltering
                ? string.Format("共 {0} 张（≥{1}px），合计 {2}", viewCount, sizeThreshold, FormatBytes(viewSize))
                : string.Format("筛选后 {0} 张（≥{1}px），合计 {2}（已勾选项不受筛选影响）", viewCount, sizeThreshold,
                    FormatBytes(viewSize));
            string selectedText = string.Format("已勾选 {0} 张，{1}", selectedCount, FormatBytes(selectedSize));

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(viewTitle, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(selectedText, EditorStyles.miniLabel);
            }
        }

        /// <summary>绘制列表：目录组（可选）→ 尺寸档 → 条目</summary>
        private void DrawTiers()
        {
            if (!scanned)
            {
                EditorGUILayout.HelpBox(
                    string.Format("扫描 {0} 下所有资源引用到的纹理（含模型、材质、ScriptableObject 引用到的外部贴图），\n" +
                                  "只列出边长 ≥ {1} 的文件；Packages 内的资源不参与；「目录分级」设为 1 会再按一级目录分组。",
                        scanRoot, sizeThreshold),
                    MessageType.Info);
                return;
            }

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.Format("{0} 下没有引用到边长 ≥ {1} 的纹理。", scanRoot, sizeThreshold),
                    MessageType.Warning);
                return;
            }

            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.Format("当前筛选「{0} + {1}」下没有符合条件的纹理，把筛选改回「显示所有」/「不限」可查看全部。",
                        DisplayFilterLabels[(int)displayFilter], MaxSizeFilterLabels[(int)maxSizeFilter]),
                    MessageType.Info);
                return;
            }

            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                if (!group.IsRoot)
                {
                    DrawGroupHeader(group);
                    if (!group.Foldout)
                    {
                        continue;
                    }
                }

                for (int t = 0; t < group.Tiers.Count; t++)
                {
                    DrawTier(group.Tiers[t], group.IsRoot ? 0f : 12f);
                }
            }
        }

        /// <summary>绘制目录组头（加粗折叠条 + 处理状态 + 组内全选）</summary>
        private void DrawGroupHeader(DirectoryGroup group)
        {
            GUILayout.Space(8f);

            const float buttonWidth = 54f;
            const float stateWidth = 92f;
            string title = string.Format("{0}/   （{1} 张，{2}）", group.Label, group.Count,
                FormatBytes(group.TotalSize));
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            float foldoutWidth = Mathf.Max(80f, rowRect.width - buttonWidth - stateWidth);
            Rect foldoutRect = new Rect(rowRect.x, rowRect.y, foldoutWidth, rowRect.height);
            Rect stateRect = new Rect(rowRect.xMax - buttonWidth - stateWidth, rowRect.y, stateWidth,
                rowRect.height);
            Rect buttonRect = new Rect(rowRect.xMax - buttonWidth, rowRect.y, buttonWidth, rowRect.height);

            group.Foldout = EditorGUI.Foldout(foldoutRect, group.Foldout, title, true, boldFoldoutStyle);

            DrawDirectoryState(stateRect, group);

            // 右键目录标题：清除该目录记录 / 复制目录路径
            Event current = Event.current;
            if (current.type == EventType.ContextClick && foldoutRect.Contains(current.mousePosition))
            {
                ShowDirectoryContextMenu(group);
                current.Use();
            }

            bool allSelected = group.SelectedCount == group.Count;
            if (GUI.Button(buttonRect, allSelected ? "全不选" : "全选", EditorStyles.miniButton))
            {
                for (int t = 0; t < group.Tiers.Count; t++)
                {
                    List<TextureEntry> entries = group.Tiers[t].Entries;
                    for (int e = 0; e < entries.Count; e++)
                    {
                        entries[e].Selected = !allSelected;
                    }
                }

                NotifySelectionChanged();
            }
        }

        /// <summary>在目录标题右侧画出配置里的处理状态：未处理 / 包含已处理 / 已处理</summary>
        private void DrawDirectoryState(Rect rect, DirectoryGroup group)
        {
            ConfigDirectory record = FindDirectoryRecord(group.DirectoryPath);
            int processedCount = group.DirectoryProcessed;
            int total = group.DirectoryTotal;

            DirectoryState state = processedCount <= 0
                ? DirectoryState.Untouched
                : processedCount >= total
                    ? DirectoryState.Done
                    : DirectoryState.Partial;

            string text;
            Color color;
            if (state == DirectoryState.Done)
            {
                text = "已处理";
                color = new Color(0.45f, 0.85f, 0.45f);
            }
            else if (state == DirectoryState.Partial)
            {
                text = "包含已处理";
                color = new Color(0.95f, 0.8f, 0.35f);
            }
            else
            {
                text = "未处理";
                color = new Color(0.62f, 0.62f, 0.62f);
            }

            string tooltip = record == null
                ? "配置里还没有这个目录的记录"
                : string.Format(
                    "配置记录：MaxSize {0}，Mipmap {1}\n更新于 {2}\n已登记 {3} / {4} 张\n右键目录标题可清除该目录记录",
                    record.MaxSize > 0 ? record.MaxSize.ToString() : "未设置",
                    DescribeMipmapValue(record.Mipmap),
                    string.IsNullOrEmpty(record.UpdatedAt) ? "-" : record.UpdatedAt,
                    processedCount, total);

            Color oldColor = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, new GUIContent(text, tooltip), EditorStyles.miniLabel);
            GUI.contentColor = oldColor;
        }

        /// <summary>目录标题右键菜单</summary>
        private void ShowDirectoryContextMenu(DirectoryGroup group)
        {
            GenericMenu menu = new GenericMenu();
            ConfigDirectory record = FindDirectoryRecord(group.DirectoryPath);
            if (record == null)
            {
                menu.AddDisabledItem(new GUIContent("清除该目录记录（暂无记录）"));
            }
            else
            {
                menu.AddItem(new GUIContent("清除该目录记录"), false,
                    () => ClearDirectoryRecord(group.DirectoryPath));
            }

            menu.AddItem(new GUIContent("复制目录路径"), false,
                () => EditorGUIUtility.systemCopyBuffer = group.DirectoryPath);
            menu.ShowAsContext();
        }

        /// <summary>绘制一个尺寸档位（折叠头 + 组内全选 + 条目列表）</summary>
        private void DrawTier(TextureTier tier, float indent)
        {
            GUILayout.Space(4f);

            // Foldout 会撑满剩余宽度，这里按矩形手工排版，把「全选」按钮固定在右侧
            string title = string.Format("{0}  （{1} 张，{2}）", tier.Label, tier.Entries.Count,
                FormatBytes(tier.TotalSize));
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect foldoutRect = new Rect(rowRect.x + indent, rowRect.y, rowRect.width - 60f - indent,
                rowRect.height);
            Rect buttonRect = new Rect(rowRect.xMax - 54f, rowRect.y, 54f, rowRect.height);

            tier.Foldout = EditorGUI.Foldout(foldoutRect, tier.Foldout, title, true, EditorStyles.foldout);

            bool allSelected = tier.SelectedCount == tier.Entries.Count;
            if (GUI.Button(buttonRect, allSelected ? "全不选" : "全选", EditorStyles.miniButton))
            {
                for (int i = 0; i < tier.Entries.Count; i++)
                {
                    tier.Entries[i].Selected = !allSelected;
                }

                NotifySelectionChanged();
            }

            if (!tier.Foldout)
            {
                return;
            }

            for (int i = 0; i < tier.Entries.Count; i++)
            {
                DrawEntry(tier.Entries[i], indent);
            }
        }

        /// <summary>绘制单条纹理：勾选框、名称（左键定位 / 右键选中引用者）、源尺寸、体积、导入上限、引用数</summary>
        private void DrawEntry(TextureEntry entry, float indent)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16f + indent);
                bool selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(16f));
                if (selected != entry.Selected)
                {
                    entry.Selected = selected;
                    NotifySelectionChanged();
                }

                if (GUILayout.Button(
                        new GUIContent(entry.FileName,
                            "左键：在 Project 窗口定位这张纹理\n右键：选中所有引用它的资源"),
                        nameStyle, GUILayout.Width(190f)))
                {
                    PingAsset(entry.AssetPath);
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

                // 按钮不响应右键，这里按整行矩形补一次右键命中：选中引用者
                Rect lastRect = GUILayoutUtility.GetLastRect();
                HandleEntryContextClick(entry, new Rect(0f, lastRect.y, lastRect.xMax, lastRect.height));
            }
        }

        /// <summary>
        /// 勾选变化后调用：已勾选的条目不受筛选影响，所以该行是否还该显示取决于勾选状态，
        /// 需要重排一次列表；重排只能在 Layout 阶段做，这里打标记留给下一帧。
        /// </summary>
        private void NotifySelectionChanged()
        {
            if (IsFiltering)
            {
                RequestRebuild();
            }
        }

        /// <summary>
        /// 请求重排列表（只影响展示）。真正重建推迟到下一次 Layout 阶段，
        /// 否则在鼠标事件里改动 groups 会和上一帧缓存的控件数量对不上，Unity 会报控件错位。
        /// </summary>
        private void RequestRebuild()
        {
            pendingRebuild = true;
            Repaint();
        }

        /// <summary>右键点条目：把引用这张纹理的资源全部选中（Project 窗口高亮）</summary>
        private void HandleEntryContextClick(TextureEntry entry, Rect rowRect)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 1)
            {
                return;
            }

            if (!rowRect.Contains(current.mousePosition))
            {
                return;
            }

            SelectReferencers(entry);
            current.Use();
        }

        /// <summary>选中所有引用该纹理的资源，并 ping 第一个方便定位</summary>
        private void SelectReferencers(TextureEntry entry)
        {
            if (entry.Referencers.Count == 0)
            {
                SetStatus(string.Format("{0} 没有找到引用它的资源。", entry.FileName), MessageType.Info);
                return;
            }

            List<Object> assets = new List<Object>();
            for (int i = 0; i < entry.Referencers.Count; i++)
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(entry.Referencers[i]);
                if (asset != null && !assets.Contains(asset))
                {
                    assets.Add(asset);
                }
            }

            if (assets.Count == 0)
            {
                SetStatus(string.Format("引用 {0} 的资源已被删除或无法加载。", entry.FileName),
                    MessageType.Warning);
                return;
            }

            Selection.objects = assets.ToArray();
            EditorGUIUtility.PingObject(assets[0]);

            // 扫描时每条纹理最多记 MaxReferencers 个引用者，超出部分按下限提示
            string capped = entry.Referencers.Count >= MaxReferencers
                ? string.Format("（每条纹理最多记录 {0} 个引用者）", MaxReferencers)
                : string.Empty;
            SetStatus(string.Format("已选中引用 {0} 的 {1} 个资源{2}。", entry.FileName, assets.Count, capped),
                MessageType.Info);
            Repaint();
        }

        /// <summary>在 Project 窗口定位单个资源</summary>
        private static void PingAsset(string assetPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
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

                    GUILayout.Label(new GUIContent("Mipmap", "把选中纹理的 mipmap 设为该状态，「不变」则不修改"),
                        GUILayout.Width(56f));
                    mipmapMode = (MipmapMode)EditorGUILayout.Popup((int)mipmapMode, MipmapModeLabels,
                        GUILayout.Width(72f));

                    if (GUILayout.Button("应用导入设置到选中项", GUILayout.Width(160f)))
                    {
                        ApplyImportSettings();
                    }

                    recordToConfig = EditorGUILayout.ToggleLeft(
                        new GUIContent("记录到配置",
                            "勾选后，应用导入设置时把选中项所在目录的处理方式（MaxSize / Mipmap）与已处理纹理写入\n" +
                            ConfigPath),
                        recordToConfig, GUILayout.Width(96f));

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
                    bool clearAfterProcess = EditorGUILayout.ToggleLeft(
                        new GUIContent("处理后取消勾选",
                            "压缩 / 应用导入设置 / 从备份恢复成功后，自动取消这些条目的勾选\n只对成功的条目生效，失败项保持勾选"),
                        clearSelectionAfterProcess, GUILayout.Width(112f));
                    if (clearAfterProcess != clearSelectionAfterProcess)
                    {
                        clearSelectionAfterProcess = clearAfterProcess;
                        EditorPrefs.SetInt(PrefClearSelectionAfterProcess, clearSelectionAfterProcess ? 1 : 0);
                    }

                    GUILayout.FlexibleSpace();

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
            for (int g = 0; g < groups.Count; g++)
            {
                for (int t = 0; t < groups[g].Tiers.Count; t++)
                {
                    List<TextureEntry> entries = groups[g].Tiers[t].Entries;
                    for (int e = 0; e < entries.Count; e++)
                    {
                        TextureEntry entry = entries[e];
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
                            sample = string.Format("示例：{0}  {1}x{2} → {3}x{4}", entry.FileName,
                                entry.SourceWidth, entry.SourceHeight, width, height);
                        }
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
                    if (!IsProjectAsset(dependency))
                    {
                        continue;
                    }

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

                    if (referencers.Count < MaxReferencers && !referencers.Contains(candidates[i]))
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

            tierBounds = BuildTierBounds(sizeThreshold);
            allEntries.Clear();
            foreach (KeyValuePair<string, List<string>> pair in referenceMap)
            {
                TextureEntry entry = BuildEntry(pair.Key, pair.Value);
                if (entry != null)
                {
                    allEntries.Add(entry);
                }
            }

            RebuildGroups();
            scanned = true;

            int total = allEntries.Count;

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

        /// <summary>
        /// 是否参与扫描与处理：只认 Assets 下的资源。
        /// Packages（包内置资源）与工程外路径一律排除 —— 源文件不在工程里，
        /// 压缩/导入设置改不动或会被包更新覆盖，列出来只会刷满包自带贴图。
        /// </summary>
        private static bool IsProjectAsset(string assetPath)
        {
            return assetPath.StartsWith("Assets/", StringComparison.Ordinal);
        }

        /// <summary>把一条纹理路径转成列表条目，不满足阈值或格式不支持时返回 null</summary>
        private TextureEntry BuildEntry(string assetPath, List<string> referencers)
        {
            if (!IsProjectAsset(assetPath))
            {
                return null;
            }

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
                Guid = AssetDatabase.AssetPathToGUID(assetPath),
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
            for (int i = 0; i < tierBounds.Count; i++)
            {
                int min = tierBounds[i];
                int max = i == 0 ? int.MaxValue : tierBounds[i - 1] - 1;
                if (maxSide >= min && maxSide <= max)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按当前「目录分级」与「筛选」把扫描结果重排成 目录组 → 尺寸档 两级结构；
        /// 只重排展示，不重新扫描，切换分级/筛选时立即生效。
        /// </summary>
        private void RebuildGroups()
        {
            // 重建会换掉所有分组对象，先按 目录名 / 目录名+档位 把折叠状态记下来，
            // 重建后还原，避免勾选触发的重排把用户折叠好的组全部展开
            Dictionary<string, bool> groupFoldouts = SnapshotFoldouts(true);
            Dictionary<string, bool> tierFoldouts = SnapshotFoldouts(false);

            groups.Clear();
            if (allEntries.Count == 0)
            {
                return;
            }

            EnsureConfigLoaded();

            int level = Mathf.Clamp(directoryLevel, 0, 8);
            if (level == 0)
            {
                DirectoryGroup root = new DirectoryGroup
                {
                    IsRoot = true,
                    Label = "全部",
                    DirectoryPath = scanRoot.TrimEnd('/')
                };
                root.Tiers = CreateTiers(allEntries, root.DirectoryPath);
                if (root.Tiers.Count == 0)
                {
                    return;
                }

                groups.Add(root);
                RestoreFoldouts(groupFoldouts, tierFoldouts);
                FillDirectoryProgress();
                return;
            }

            Dictionary<string, List<TextureEntry>> map = new Dictionary<string, List<TextureEntry>>();
            for (int i = 0; i < allEntries.Count; i++)
            {
                TextureEntry entry = allEntries[i];
                string key = GetDirectoryKey(entry.AssetPath);
                List<TextureEntry> list;
                if (!map.TryGetValue(key, out list))
                {
                    list = new List<TextureEntry>();
                    map[key] = list;
                }

                list.Add(entry);
            }

            foreach (KeyValuePair<string, List<TextureEntry>> pair in map)
            {
                DirectoryGroup group = new DirectoryGroup
                {
                    Label = pair.Key,
                    DirectoryPath = GetDirectoryPath(pair.Value[0].AssetPath),
                    IsRoot = false
                };
                group.Tiers = CreateTiers(pair.Value, group.DirectoryPath);

                // 当前筛选下一条都不剩的目录整组隐藏
                if (group.Tiers.Count > 0)
                {
                    groups.Add(group);
                }
            }

            // 体积大的目录排前面，体积相同按名称，保证顺序稳定
            groups.Sort((a, b) =>
            {
                int result = b.TotalSize.CompareTo(a.TotalSize);
                return result != 0 ? result : string.CompareOrdinal(a.Label, b.Label);
            });

            RestoreFoldouts(groupFoldouts, tierFoldouts);
            FillDirectoryProgress();
        }

        /// <summary>
        /// 记录当前列表的折叠状态：groups == true 时按目录名，否则按「目录名|档位名」；
        /// 键用显示名而不是引用，重建后名称不变就能对上。
        /// </summary>
        private Dictionary<string, bool> SnapshotFoldouts(bool groupLevel)
        {
            Dictionary<string, bool> snapshot = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                if (groupLevel)
                {
                    snapshot[group.Label] = group.Foldout;
                    continue;
                }

                for (int t = 0; t < group.Tiers.Count; t++)
                {
                    snapshot[group.Label + "|" + group.Tiers[t].Label] = group.Tiers[t].Foldout;
                }
            }

            return snapshot;
        }

        /// <summary>把快照里的折叠状态套回新分组，快照里没有的保持默认（展开）</summary>
        private void RestoreFoldouts(Dictionary<string, bool> groupFoldouts, Dictionary<string, bool> tierFoldouts)
        {
            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                bool foldout;
                if (groupFoldouts.TryGetValue(group.Label, out foldout))
                {
                    group.Foldout = foldout;
                }

                for (int t = 0; t < group.Tiers.Count; t++)
                {
                    if (tierFoldouts.TryGetValue(group.Label + "|" + group.Tiers[t].Label, out foldout))
                    {
                        group.Tiers[t].Foldout = foldout;
                    }
                }
            }
        }

        /// <summary>配置变化后刷新：筛选生效时按新状态重排列表，否则只更新目录进度</summary>
        private void RefreshAfterConfigChange()
        {
            if (allEntries.Count == 0)
            {
                return;
            }

            if (!IsFiltering)
            {
                // 没有筛选时列表结构不变，只刷进度，别动折叠状态
                FillDirectoryProgress();
            }
            else
            {
                // 有筛选时列表内容可能变化（刚处理完的行该从「未处理」里消失），走延迟重排
                RequestRebuild();
            }
        }

        /// <summary>统计每个目录的真实处理进度（遍历扫描全集，不受筛选影响）</summary>
        private void FillDirectoryProgress()
        {
            EnsureConfigLoaded();

            Dictionary<string, DirectoryGroup> byPath =
                new Dictionary<string, DirectoryGroup>(StringComparer.Ordinal);
            Dictionary<string, ConfigDirectory> records =
                new Dictionary<string, ConfigDirectory>(StringComparer.Ordinal);

            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                group.DirectoryTotal = 0;
                group.DirectoryProcessed = 0;
                if (!byPath.ContainsKey(group.DirectoryPath))
                {
                    byPath[group.DirectoryPath] = group;
                    records[group.DirectoryPath] = configData.Find(group.DirectoryPath);
                }
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                TextureEntry entry = allEntries[i];
                DirectoryGroup group;
                if (!byPath.TryGetValue(GetDirectoryPath(entry.AssetPath), out group))
                {
                    continue;
                }

                group.DirectoryTotal++;
                ConfigDirectory record = records[group.DirectoryPath];
                if (record != null && record.Contains(entry.Guid))
                {
                    group.DirectoryProcessed++;
                }
            }
        }

        /// <summary>按当前档位下限与筛选，给一批条目建出尺寸档；空档直接丢弃，档内按体积降序</summary>
        private List<TextureTier> CreateTiers(List<TextureEntry> entries, string directoryPath)
        {
            // 两个筛选都可能是「无」，都不生效时不查目录记录
            ConfigDirectory record = IsFiltering ? FindDirectoryRecord(directoryPath) : null;

            List<TextureTier> list = new List<TextureTier>();
            for (int i = 0; i < tierBounds.Count; i++)
            {
                list.Add(new TextureTier
                {
                    Min = tierBounds[i],
                    Max = i == 0 ? int.MaxValue : tierBounds[i - 1] - 1,
                    Label = i == 0
                        ? string.Format("≥ {0}", tierBounds[i])
                        : string.Format("{0} ~ {1}", tierBounds[i], tierBounds[i - 1] - 1)
                });
            }

            for (int i = 0; i < entries.Count; i++)
            {
                TextureEntry entry = entries[i];
                if (!PassesFilter(entry, record))
                {
                    continue;
                }

                int index = FindTierIndex(Mathf.Max(entry.SourceWidth, entry.SourceHeight));
                if (index >= 0)
                {
                    list[index].Entries.Add(entry);
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Entries.Sort(CompareBySize);
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Entries.Count == 0)
                {
                    list.RemoveAt(i);
                }
            }

            return list;
        }

        /// <summary>
        /// 取纹理路径的前 N 层目录作为分组键：扫描目录内的纹理以扫描目录为基准，
        /// 目录外被引用的贴图以 Assets 为基准，这样外部资源能看出真实来源。
        /// </summary>
        private string GetDirectoryKey(string assetPath)
        {
            string prefix = scanRoot.TrimEnd('/') + "/";
            string relative;
            if (assetPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                relative = assetPath.Substring(prefix.Length);
            }
            else if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                relative = assetPath.Substring("Assets/".Length);
            }
            else
            {
                relative = assetPath;
            }

            string[] parts = relative.Split('/');
            int directoryCount = parts.Length - 1;
            if (directoryCount <= 0)
            {
                return "（根目录）";
            }

            int take = Mathf.Clamp(directoryLevel, 1, directoryCount);
            return string.Join("/", parts, 0, take);
        }

        /// <summary>
        /// 取纹理所属分组的完整目录路径（配置文件里的键）：目录内纹理以扫描目录为基准，
        /// 目录外被引用的贴图以 Assets 为基准；目录分级为 0 时统一归到扫描目录本身。
        /// </summary>
        private string GetDirectoryPath(string assetPath)
        {
            string scanPrefix = scanRoot.TrimEnd('/') + "/";
            string basePath;
            string relative;
            if (assetPath.StartsWith(scanPrefix, StringComparison.Ordinal))
            {
                basePath = scanRoot.TrimEnd('/');
                relative = assetPath.Substring(scanPrefix.Length);
            }
            else if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                basePath = "Assets";
                relative = assetPath.Substring("Assets/".Length);
            }
            else
            {
                string folder = Path.GetDirectoryName(assetPath);
                return string.IsNullOrEmpty(folder) ? assetPath : folder.Replace('\\', '/');
            }

            if (directoryLevel <= 0)
            {
                return basePath;
            }

            string[] parts = relative.Split('/');
            int directoryCount = parts.Length - 1;
            if (directoryCount <= 0)
            {
                return basePath;
            }

            int take = Mathf.Clamp(directoryLevel, 1, directoryCount);
            return basePath + "/" + string.Join("/", parts, 0, take);
        }

        // ================= 目录处理记录（配置文件） =================

        /// <summary>确保配置已从磁盘载入（只读一次；文件缺失或损坏时按空配置处理）</summary>
        private void EnsureConfigLoaded()
        {
            if (configData != null)
            {
                return;
            }

            configData = new ConfigData();
            if (!File.Exists(ConfigPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                ConfigData loaded = JsonUtility.FromJson<ConfigData>(json);
                if (loaded == null)
                {
                    return;
                }

                configData = loaded;
                if (configData.Directories == null)
                {
                    configData.Directories = new List<ConfigDirectory>();
                }

                // 手改 json 时可能漏字段，这里补全后界面才不至于空引用
                for (int i = 0; i < configData.Directories.Count; i++)
                {
                    ConfigDirectory item = configData.Directories[i];
                    if (item == null)
                    {
                        configData.Directories.RemoveAt(i--);
                        continue;
                    }

                    if (string.IsNullOrEmpty(item.Directory))
                    {
                        configData.Directories.RemoveAt(i--);
                        continue;
                    }

                    if (item.Files == null)
                    {
                        item.Files = new List<string>();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[大纹理工具] 读取目录配置失败，按空配置处理：" + e.Message);
            }
        }

        /// <summary>把内存里的配置写回工具目录下的 json（会触发一次 TextAsset 导入，不引起重编译）</summary>
        private void SaveConfig()
        {
            EnsureConfigLoaded();
            try
            {
                string folder = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(ConfigPath, JsonUtility.ToJson(configData, true), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception e)
            {
                Debug.LogError("[大纹理工具] 写入目录配置失败：" + e.Message);
            }
        }

        /// <summary>按完整目录路径取记录，没有返回 null</summary>
        private ConfigDirectory FindDirectoryRecord(string directoryPath)
        {
            EnsureConfigLoaded();
            return configData.Find(directoryPath);
        }

        /// <summary>
        /// 把本次成功应用过导入设置的纹理按目录写入配置：记下 maxSize / mipmap 方式，
        /// 并登记这批纹理的 GUID，返回写入的目录数量。
        /// </summary>
        private int RecordProcessedDirectories(List<TextureEntry> processed)
        {
            if (processed.Count == 0)
            {
                return 0;
            }

            EnsureConfigLoaded();

            Dictionary<string, List<TextureEntry>> map = new Dictionary<string, List<TextureEntry>>();
            for (int i = 0; i < processed.Count; i++)
            {
                TextureEntry entry = processed[i];
                string key = GetDirectoryPath(entry.AssetPath);
                List<TextureEntry> list;
                if (!map.TryGetValue(key, out list))
                {
                    list = new List<TextureEntry>();
                    map[key] = list;
                }

                list.Add(entry);
            }

            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (KeyValuePair<string, List<TextureEntry>> pair in map)
            {
                ConfigDirectory record = configData.Find(pair.Key);
                if (record == null)
                {
                    record = new ConfigDirectory { Directory = pair.Key };
                    configData.Directories.Add(record);
                }

                // 本次没动的项保持原记录，避免「只改 mipmap」把 maxSize 冲成 0
                if (importMaxSize > 0)
                {
                    record.MaxSize = importMaxSize;
                }

                if (mipmapMode != MipmapMode.Keep)
                {
                    record.Mipmap = (int)mipmapMode;
                }

                record.UpdatedAt = stamp;

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    record.AddFile(pair.Value[i].Guid);
                }

                record.Processed = IsDirectoryFullyRecorded(pair.Key);
            }

            configData.Directories.Sort((a, b) => string.CompareOrdinal(a.Directory, b.Directory));
            SaveConfig();
            return map.Count;
        }

        /// <summary>当前列表里属于该目录的纹理是否已全部登记过</summary>
        private bool IsDirectoryFullyRecorded(string directoryPath)
        {
            ConfigDirectory record = configData.Find(directoryPath);
            if (record == null)
            {
                return false;
            }

            int total = 0;
            for (int i = 0; i < allEntries.Count; i++)
            {
                TextureEntry entry = allEntries[i];
                if (!string.Equals(GetDirectoryPath(entry.AssetPath), directoryPath, StringComparison.Ordinal))
                {
                    continue;
                }

                total++;
                if (!record.Contains(entry.Guid))
                {
                    return false;
                }
            }

            return total > 0;
        }

        /// <summary>删除某个目录的处理记录</summary>
        private void ClearDirectoryRecord(string directoryPath)
        {
            EnsureConfigLoaded();
            ConfigDirectory record = configData.Find(directoryPath);
            if (record == null)
            {
                return;
            }

            configData.Directories.Remove(record);
            SaveConfig();
            RefreshAfterConfigChange();
            SetStatus(string.Format("已清除目录记录：{0}", directoryPath), MessageType.Info);
            Repaint();
        }

        /// <summary>配置里存的 mipmap 数值转成中文</summary>
        private static string DescribeMipmapValue(int value)
        {
            if (value == (int)MipmapMode.Enable)
            {
                return "开启";
            }

            return value == (int)MipmapMode.Disable ? "关闭" : "不变";
        }

        /// <summary>是否处于筛选状态（处理状态筛选或 MaxSize 筛选任一生效）</summary>
        private bool IsFiltering
        {
            get { return displayFilter != DisplayFilter.All || maxSizeFilter != MaxSizeFilter.All; }
        }

        /// <summary>
        /// 条目是否通过当前两个筛选（两者叠加，取交集）。
        /// 已勾选的条目始终通过：勾了却在筛选后消失会让人以为勾选丢了。
        /// </summary>
        private bool PassesFilter(TextureEntry entry, ConfigDirectory record)
        {
            if (entry.Selected || !IsFiltering)
            {
                return true;
            }

            if (displayFilter != DisplayFilter.All)
            {
                bool processed = record != null && record.Contains(entry.Guid);
                if (displayFilter == DisplayFilter.Processed ? !processed : processed)
                {
                    return false;
                }
            }

            if (maxSizeFilter != MaxSizeFilter.All)
            {
                bool configured = IsMaxSizeConfigured(entry, record);
                if (maxSizeFilter == MaxSizeFilter.Configured ? !configured : configured)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// MaxSize 是否配置过：导入上限不等于 Unity 默认的 2048（被人改过），
        /// 或配置记录里写过该目录的 MaxSize 且登记过这张图（本工具批次应用过）。
        /// </summary>
        private static bool IsMaxSizeConfigured(TextureEntry entry, ConfigDirectory record)
        {
            // ImportMaxSize 为 0 说明没读到 TextureImporter（异常资源），不当成「已配置」
            if (entry.ImportMaxSize > 0 && entry.ImportMaxSize != DefaultImportMaxSize)
            {
                return true;
            }

            return record != null && record.MaxSize > 0 && record.Contains(entry.Guid);
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

        /// <summary>当前 mipmap 方式的中文描述，用于预览与确认弹窗</summary>
        private string DescribeMipmapMode()
        {
            return mipmapMode == MipmapMode.Enable ? "开启 Mipmap" : "关闭 Mipmap";
        }

        /// <summary>导入设置那一行的说明文字：会改什么、影响多少张</summary>
        private string BuildImportPreview()
        {
            if (importMaxSize <= 0 && mipmapMode == MipmapMode.Keep)
            {
                return "导入设置：未选择修改项（Import MaxSize 为「不修改」且 Mipmap 为「不变」）。";
            }

            List<string> changes = new List<string>();
            if (importMaxSize > 0)
            {
                changes.Add(string.Format("Import MaxSize = {0}", importMaxSize));
            }

            if (mipmapMode != MipmapMode.Keep)
            {
                changes.Add(DescribeMipmapMode());
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
            EditorPrefs.SetInt(PrefMipmapMode, (int)mipmapMode);
            EditorPrefs.SetInt(PrefRecordToConfig, recordToConfig ? 1 : 0);

            if (importMaxSize <= 0 && mipmapMode == MipmapMode.Keep)
            {
                SetStatus("Import MaxSize 为「不修改」且 Mipmap 为「不变」，没有需要应用的设置。",
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

            if (mipmapMode != MipmapMode.Keep)
            {
                changes.Add(DescribeMipmapMode());
            }

            string message = string.Format("将对 {0} 张纹理执行：{1}。\n\n只修改导入设置并重新导入，不动源图文件，不需要备份。",
                targets.Count, string.Join("、", changes.ToArray()));
            if (!EditorUtility.DisplayDialog("应用导入设置", message, "应用", "取消"))
            {
                return;
            }

            int success = 0;
            List<TextureEntry> processed = new List<TextureEntry>();
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

                    if (mipmapMode != MipmapMode.Keep)
                    {
                        importer.mipmapEnabled = mipmapMode == MipmapMode.Enable;
                    }

                    //立刻重导入, 保证回读的导入上限/mipmap 状态与列表一致
                    importer.SaveAndReimport();
                    entry.ImportMaxSize = importer.maxTextureSize;
                    entry.MipmapEnabled = importer.mipmapEnabled;
                    processed.Add(entry);
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

            int recordedCount = recordToConfig ? RecordProcessedDirectories(processed) : 0;
            int clearedCount = clearSelectionAfterProcess ? ClearSelectionOf(processed) : 0;
            RefreshAfterConfigChange();

            List<string> infos = new List<string>();
            if (recordedCount > 0)
            {
                infos.Add(string.Format("已写入配置：{0} 个目录（{1}）", recordedCount, ConfigPath));
            }

            if (clearedCount > 0)
            {
                infos.Add(string.Format("已取消 {0} 张的勾选", clearedCount));
            }

            ReportResult("应用导入设置", success, failures,
                infos.Count > 0 ? string.Join("\n", infos.ToArray()) : null);
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

            int clearedCount = clearSelectionAfterProcess ? ClearSelectionOf(changed) : 0;

            AssetDatabase.Refresh();
            ReportResult(string.Format("压缩 {0}%", percent), changed.Count, failures,
                clearedCount > 0 ? string.Format("已取消 {0} 张的勾选", clearedCount) : null);
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

            int clearedCount = clearSelectionAfterProcess ? ClearSelectionOf(changed) : 0;

            AssetDatabase.Refresh();
            ReportResult("从备份恢复", changed.Count, failures,
                clearedCount > 0 ? string.Format("已取消 {0} 张的勾选", clearedCount) : null);
        }

        /// <summary>处理成功后取消这批条目的勾选，返回实际取消的数量</summary>
        private int ClearSelectionOf(List<TextureEntry> entries)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].Selected)
                {
                    continue;
                }

                entries[i].Selected = false;
                count++;
            }

            if (count > 0)
            {
                NotifySelectionChanged();
            }

            return count;
        }

        /// <summary>统一输出处理结果</summary>
        private void ReportResult(string action, int successCount, List<string> failures, string extraInfo = null)
        {
            string message = string.Format("{0}完成：成功 {1} 张", action, successCount);
            if (!string.IsNullOrEmpty(extraInfo))
            {
                message += "\n" + extraInfo;
            }

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

        /// <summary>收集已勾选的纹理：走扫描全集，筛选隐藏的行不会丢勾选</summary>
        private List<TextureEntry> CollectSelected()
        {
            List<TextureEntry> selected = new List<TextureEntry>();
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].Selected)
                {
                    selected.Add(allEntries[i]);
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

        /// <summary>是否所有目录组与尺寸档都已展开（用于切换按钮的显示与动作）；没有分组时视为未展开</summary>
        private bool IsAllExpanded()
        {
            if (groups.Count == 0)
            {
                return false;
            }

            for (int g = 0; g < groups.Count; g++)
            {
                DirectoryGroup group = groups[g];
                if (!group.Foldout)
                {
                    return false;
                }

                for (int t = 0; t < group.Tiers.Count; t++)
                {
                    if (!group.Tiers[t].Foldout)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void SetAllFoldout(bool foldout)
        {
            for (int g = 0; g < groups.Count; g++)
            {
                groups[g].Foldout = foldout;
                for (int t = 0; t < groups[g].Tiers.Count; t++)
                {
                    groups[g].Tiers[t].Foldout = foldout;
                }
            }
        }

        private void ClearResults()
        {
            groups.Clear();
            allEntries.Clear();
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
