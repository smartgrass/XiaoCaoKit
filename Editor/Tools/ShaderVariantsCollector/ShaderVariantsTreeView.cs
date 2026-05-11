using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderVariantsCollector
{
    public class ShaderVariantsTreeView : TreeView
    {
        private ShaderVariantCollection collection;
        private ShaderVariantsCollectorWindow window;
        private List<ShaderVariantData> newVariants = new List<ShaderVariantData>();
        private Dictionary<Shader, List<ShaderVariantData>> shaderVariantsMap = new Dictionary<Shader, List<ShaderVariantData>>();
        private Vector2 scrollPosition = Vector2.zero;

        // PassType颜色映射 - 恢复原来的浅色背景
        private static readonly Dictionary<PassType, Color> PassTypeColors = new Dictionary<PassType, Color>
        {
            { PassType.Normal, new Color(0.8f, 0.9f, 1f, 0.8f) }, // 浅蓝色
            { PassType.Vertex, new Color(0.9f, 1f, 0.8f, 0.8f) }, // 浅绿色
            { PassType.VertexLM, new Color(1f, 0.9f, 0.8f, 0.8f) }, // 浅橙色
            { PassType.VertexLMRGBM, new Color(1f, 0.8f, 0.9f, 0.8f) }, // 浅粉色
            { PassType.ForwardBase, new Color(0.9f, 0.8f, 1f, 0.8f) }, // 浅紫色
            { PassType.ForwardAdd, new Color(0.8f, 1f, 0.9f, 0.8f) }, // 浅青色
            { PassType.LightPrePassBase, new Color(1f, 1f, 0.8f, 0.8f) }, // 浅黄色
            { PassType.LightPrePassFinal, new Color(0.8f, 0.8f, 1f, 0.8f) }, // 浅紫蓝色
            { PassType.ShadowCaster, new Color(0.6f, 0.6f, 0.6f, 0.8f) }, // 灰色
            { PassType.Deferred, new Color(1f, 0.8f, 0.8f, 0.8f) }, // 浅红色
            { PassType.Meta, new Color(0.9f, 0.9f, 0.9f, 0.8f) }, // 浅灰色
            { PassType.MotionVectors, new Color(0.8f, 1f, 1f, 0.8f) }, // 浅天蓝色
            { PassType.ScriptableRenderPipeline, new Color(1f, 0.9f, 1f, 0.8f) }, // 浅洋红色
            { PassType.ScriptableRenderPipelineDefaultUnlit, new Color(0.85f, 0.85f, 1f, 0.8f) } // 浅薰衣草色
        };
        
        // PassType文字颜色映射 - 使用中等深度的文字颜色以降低对比度
        private static readonly Dictionary<PassType, Color> PassTypeTextColors = new Dictionary<PassType, Color>
        {
            { PassType.Normal, new Color(0.2f, 0.3f, 0.6f) }, // 中深蓝色文字
            { PassType.Vertex, new Color(0.2f, 0.6f, 0.2f) }, // 中深绿色文字
            { PassType.VertexLM, new Color(0.6f, 0.4f, 0.2f) }, // 中深橙色文字
            { PassType.VertexLMRGBM, new Color(0.6f, 0.2f, 0.4f) }, // 中深粉色文字
            { PassType.ForwardBase, new Color(0.4f, 0.2f, 0.6f) }, // 中深紫色文字
            { PassType.ForwardAdd, new Color(0.2f, 0.6f, 0.4f) }, // 中深青色文字
            { PassType.LightPrePassBase, new Color(0.6f, 0.5f, 0.2f) }, // 中深黄色文字
            { PassType.LightPrePassFinal, new Color(0.3f, 0.3f, 0.6f) }, // 中深紫蓝色文字
            { PassType.ShadowCaster, new Color(0.3f, 0.3f, 0.3f) }, // 中深灰色文字
            { PassType.Deferred, new Color(0.6f, 0.3f, 0.3f) }, // 中深红色文字
            { PassType.Meta, new Color(0.4f, 0.4f, 0.4f) }, // 中深灰色文字
            { PassType.MotionVectors, new Color(0.2f, 0.5f, 0.6f) }, // 中深天蓝色文字
            { PassType.ScriptableRenderPipeline, new Color(0.6f, 0.2f, 0.6f) }, // 中深洋红色文字
            { PassType.ScriptableRenderPipelineDefaultUnlit, new Color(0.3f, 0.3f, 0.6f) } // 中深薰衣草色文字
        };

        public ShaderVariantsTreeView(TreeViewState state, ShaderVariantsCollectorWindow window) : base(state)
        {
            this.window = window;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            useScrollView = true;
            
            Reload();
        }

        public void SetCollection(ShaderVariantCollection collection)
        {
            this.collection = collection;
            BuildShaderVariantsMap();
        }

        private void BuildShaderVariantsMap()
        {
            shaderVariantsMap.Clear();
            
            if (collection != null)
            {
                // 从ShaderVariantCollection中获取变体数据
                var variants = GetVariantsFromCollection();
                foreach (var variant in variants)
                {
                    if (!shaderVariantsMap.ContainsKey(variant.shader))
                        shaderVariantsMap[variant.shader] = new List<ShaderVariantData>();
                    
                    shaderVariantsMap[variant.shader].Add(variant);
                }
            }
        }

        private List<ShaderVariantData> GetVariantsFromCollection()
        {
            return ShaderVariantCollector.CollectVariantsFromCollection(collection);
        }

        /// <summary>
        /// 按Shader名称获取稳定排序后的变体快照。
        /// </summary>
        private List<KeyValuePair<Shader, List<ShaderVariantData>>> GetSortedShaderVariantSnapshot()
        {
            return shaderVariantsMap
                .OrderBy(kvp => kvp.Key != null ? kvp.Key.name : string.Empty, StringComparer.Ordinal)
                .Select(kvp => new KeyValuePair<Shader, List<ShaderVariantData>>(kvp.Key, SortVariantsByName(kvp.Value)))
                .ToList();
        }

        /// <summary>
        /// 按Shader名称、PassType和关键字排序变体。
        /// </summary>
        private List<ShaderVariantData> SortVariantsByName(IEnumerable<ShaderVariantData> variants)
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

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();
            
            int id = 1;
            
            // 添加现有的Shader变体
            var snapshot = GetSortedShaderVariantSnapshot();
            foreach (var kvp in snapshot)
            {
                var shader = kvp.Key;
                var variants = SortVariantsByName(kvp.Value);
                
                var shaderItem = new ShaderTreeViewItem(id++, shader, 0);
                allItems.Add(shaderItem);
                
                foreach (var variant in variants)
                {
                    var variantItem = new VariantTreeViewItem(id++, variant, 1);
                    shaderItem.AddChild(variantItem);
                    allItems.Add(variantItem);
                }
                
                // 在每个Shader的最后添加"添加变体"按钮
                var addVariantItem = new AddVariantTreeViewItem(id++, shader, 1);
                shaderItem.AddChild(addVariantItem);
                allItems.Add(addVariantItem);
            }
            
            // 添加新收集的变体
            foreach (var newVariant in newVariants)
            {
                if (!shaderVariantsMap.ContainsKey(newVariant.shader))
                {
                    var newShaderItem = new ShaderTreeViewItem(id++, newVariant.shader, 0);
                    allItems.Add(newShaderItem);
                    shaderVariantsMap[newVariant.shader] = new List<ShaderVariantData>();
                }
                
                var variantItem = new NewVariantTreeViewItem(id++, newVariant, 1);
                var existingShaderItem = allItems.FirstOrDefault(item => item is ShaderTreeViewItem shader && shader.shader == newVariant.shader) as ShaderTreeViewItem;
                if (existingShaderItem != null)
                {
                    existingShaderItem.AddChild(variantItem);
                }
                allItems.Add(variantItem);
            }
            
            // 添加"添加Shader"按钮
            var addShaderItem = new AddShaderTreeViewItem(id++, 0);
            allItems.Add(addShaderItem);
            
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        public override void OnGUI(Rect rect)
        {
            // 完全禁用TreeView的默认绘制
            // 使用自定义的GUI绘制逻辑
            
            // 绘制背景
            GUI.Box(rect, "");
            
            // 计算滚动区域
            var scrollRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, new Rect(0, 0, rect.width - 20, GetTotalHeight()));
            
            // 绘制所有项目
            DrawAllItems(rect);
            
            GUI.EndScrollView();
        }
        
        private float GetTotalHeight()
        {
            float height = 0;
            var snapshot = shaderVariantsMap.ToList();
            foreach (var kvp in snapshot)
            {
                height += 20; // Shader行高度
                if (IsExpanded(kvp.Key))
                {
                    height += kvp.Value.Count * 20; // 变体行高度
                    var newVariantsForShader = newVariants.Where(v => v.shader == kvp.Key).ToList();
                    height += newVariantsForShader.Count * 20; // 新变体行高度
                    height += 20; // Add Variant按钮高度
                }
            }
            height += 20; // Add Shader按钮高度
            return height;
        }
        
        private bool IsExpanded(Shader shader)
        {
            // 简单的展开状态管理
            return expandedShaders.Contains(shader);
        }
        
        private HashSet<Shader> expandedShaders = new HashSet<Shader>();
        
        private void DrawAllItems(Rect rect)
        {
            float y = 0;
            
            var snapshot = GetSortedShaderVariantSnapshot();
            foreach (var kvp in snapshot)
            {
                var shader = kvp.Key;
                var variants = SortVariantsByName(kvp.Value);
                
                // 绘制Shader行
                var shaderRect = new Rect(0, y, rect.width, 20);
                DrawShaderRow(shaderRect, shader, variants.Count);
                y += 20;
                
                // 如果展开，绘制变体
                if (IsExpanded(shader))
                {
                    foreach (var variant in variants)
                    {
                        var variantRect = new Rect(20, y, rect.width - 20, 20);
                        DrawVariantRow(variantRect, variant);
                        y += 20;
                    }
                    
                    // 绘制该Shader的新变体
                    var newVariantsForShader = newVariants.Where(v => v.shader == shader).ToList();
                    foreach (var newVariant in newVariantsForShader)
                    {
                        var newVariantRect = new Rect(20, y, rect.width - 20, 20);
                        DrawNewVariantRow(newVariantRect, newVariant);
                        y += 20;
                    }
                    
                    // 绘制Add Variant按钮
                    var addVariantRect = new Rect(20, y, rect.width - 20, 20);
                    DrawAddVariantRow(addVariantRect, shader);
                    y += 20;
                }
            }
            
            // 绘制Add Shader按钮
            var addShaderRect = new Rect(0, y, rect.width, 20);
            DrawAddShaderRow(addShaderRect);
        }

        private void DrawPassTypeLabel(Rect rect, PassType passType)
        {
            var passTypeText = passType.ToString();
            var maxWidth = 180; // 增加最大宽度
            var calculatedWidth = GUI.skin.label.CalcSize(new GUIContent(passTypeText)).x + 10;
            var passTypeLabelWidth = Mathf.Min(calculatedWidth, maxWidth);
            var passTypeLabelRect = new Rect(rect.x, rect.y, passTypeLabelWidth, rect.height);
            
            // 绘制PassType背景色
            if (PassTypeColors.TryGetValue(passType, out var backgroundColor))
            {
                EditorGUI.DrawRect(passTypeLabelRect, backgroundColor);
            }
            
            // 绘制PassType文本，使用对应的文字颜色
            var textColor = PassTypeTextColors.TryGetValue(passType, out var color) ? color : Color.black;
            var textStyle = new GUIStyle(GUI.skin.label) 
            { 
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor },
                fontStyle = FontStyle.Bold, // 加粗文字以进一步提高可读性
                clipping = TextClipping.Clip // 启用文字裁剪
            };
            
            // 如果文字太长，添加省略号
            var displayText = passTypeText;
            if (calculatedWidth > maxWidth)
            {
                // 计算能显示的字符数
                var availableWidth = maxWidth - 20; // 留出省略号的空间
                var charWidth = GUI.skin.label.CalcSize(new GUIContent("A")).x;
                var maxChars = Mathf.FloorToInt(availableWidth / charWidth);
                if (maxChars > 3)
                {
                    displayText = passTypeText.Substring(0, Mathf.Min(maxChars - 3, passTypeText.Length)) + "...";
                }
            }
            
            var labelRect = new Rect(passTypeLabelRect.x + 2, passTypeLabelRect.y, passTypeLabelRect.width - 4, passTypeLabelRect.height);
            EditorGUI.LabelField(labelRect, displayText, textStyle);
            
            // 添加工具提示显示完整文本
            if (calculatedWidth > maxWidth)
            {
                GUI.Label(passTypeLabelRect, new GUIContent("", passTypeText));
            }
        }
        
        private void DrawShaderRow(Rect rect, Shader shader, int variantCount)
        {
            // 绘制展开/折叠按钮
            var expandRect = new Rect(rect.x, rect.y, 20, rect.height);
            if (GUI.Button(expandRect, IsExpanded(shader) ? "▼" : "▶"))
            {
                if (IsExpanded(shader))
                    expandedShaders.Remove(shader);
                else
                    expandedShaders.Add(shader);
            }
            
            // 右侧自右向左布局按钮
            float xRight = rect.x + rect.width;

            // Del
            var deleteShaderRect = new Rect(xRight - 60, rect.y, 60, rect.height);
            xRight -= 60;

            // Locate
            var locateRect = new Rect(xRight - 60, rect.y, 60, rect.height);
            xRight -= 60;

            // 如果该Shader有新变体，则显示"Confirm All/Ignore All"两个按钮
            int newCountForShader = newVariants.Count(v => v.shader == shader);
            Rect confirmRect = default, ignoreRect = default;
            if (newCountForShader > 0)
            {
                confirmRect = new Rect(xRight - 80, rect.y, 80, rect.height);
                xRight -= 80;
                ignoreRect = new Rect(xRight - 80, rect.y, 80, rect.height);
                xRight -= 80;
            }

            // 变体数量
            var countRect = new Rect(xRight - 40, rect.y, 40, rect.height);
            xRight -= 40;

            // 放大镜按钮：只收集该Shader
            var searchRect = new Rect(xRight - 24, rect.y, 24, rect.height);
            xRight -= 24;

            // Shader 名称（留出右侧按钮区域）
            var labelRect = new Rect(rect.x + 20, rect.y, (xRight - (rect.x + 20)), rect.height);
            EditorGUI.LabelField(labelRect, shader.name, EditorStyles.boldLabel);

            // 放大镜按钮
            if (GUI.Button(searchRect, EditorGUIUtility.IconContent("Search Icon")))
            {
                CollectOnlyThisShader(shader);
            }

            // 数量
            EditorGUI.LabelField(countRect, $"({variantCount})");

            // Locate
            if (GUI.Button(locateRect, "Locate"))
            {
                Selection.activeObject = shader;
                EditorGUIUtility.PingObject(shader);
            }

            // Del
            if (GUI.Button(deleteShaderRect, "Del"))
            {
                DeleteShaderFromCollection(shader);
            }

            // 新变体批量操作
            if (newCountForShader > 0)
            {
                if (GUI.Button(ignoreRect, "Ignore All"))
                {
                    var shaderCopy = shader;
                    EditorApplication.delayCall += () => IgnoreAllNewVariantsForShader(shaderCopy);
                }
                if (GUI.Button(confirmRect, "Confirm All"))
                {
                    var shaderCopy = shader;
                    EditorApplication.delayCall += () => ConfirmAllNewVariantsForShader(shaderCopy);
                }
            }
        }
        
        private void DrawVariantRow(Rect rect, ShaderVariantData variant)
        {
            // 绘制已有变体的淡绿色背景
            EditorGUI.DrawRect(rect, new Color(0.8f, 1f, 0.8f, 0.3f)); // 淡绿色背景
            
            // 绘制PassType标签
            DrawPassTypeLabel(rect, variant.passType);
            
            // 计算PassType标签宽度
            var passTypeText = variant.passType.ToString();
            var passTypeLabelWidth = Mathf.Min(GUI.skin.label.CalcSize(new GUIContent(passTypeText)).x + 10, 180);
            
            // 绘制关键字（在PassType标签右侧）
            var labelRect = new Rect(rect.x + passTypeLabelWidth, rect.y, rect.width - passTypeLabelWidth - 120, rect.height);
            EditorGUI.LabelField(labelRect, variant.GetKeywordsString());
            
            // 绘制References按钮
            var refRect = new Rect(rect.x + rect.width - 120, rect.y, 60, rect.height);
            if (GUI.Button(refRect, "Refs"))
            {
                VariantReferencesWindow.ShowWindow(variant);
            }
            
            // 绘制删除变体按钮
            var deleteVariantRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(deleteVariantRect, "Del"))
            {
                DeleteVariantFromCollection(variant);
            }
        }
        
        private void DrawNewVariantRow(Rect rect, ShaderVariantData variant)
        {
            // 绘制新变体的淡红色背景
            EditorGUI.DrawRect(rect, new Color(1f, 0.8f, 0.8f, 0.3f)); // 淡红色背景
            
            // 绘制PassType标签
            DrawPassTypeLabel(rect, variant.passType);
            
            // 计算PassType标签宽度
            var passTypeText = variant.passType.ToString();
            var passTypeLabelWidth = Mathf.Min(GUI.skin.label.CalcSize(new GUIContent(passTypeText)).x + 10, 180);
            
            // 绘制关键字（在PassType标签右侧）
            var labelRect = new Rect(rect.x + passTypeLabelWidth, rect.y, rect.width - passTypeLabelWidth - 180, rect.height);
            EditorGUI.LabelField(labelRect, variant.GetKeywordsString());
            
            // 绘制确认按钮
            var confirmRect = new Rect(rect.x + rect.width - 180, rect.y, 60, rect.height);
            if (GUI.Button(confirmRect, "Confirm"))
            {
                window.OnVariantConfirmed(variant);
            }
            
            // 绘制References按钮
            var refRect = new Rect(rect.x + rect.width - 120, rect.y, 60, rect.height);
            if (GUI.Button(refRect, "Refs"))
            {
                VariantReferencesWindow.ShowWindow(variant);
            }
            
            // 绘制忽略按钮
            var ignoreRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(ignoreRect, "Ignore"))
            {
                newVariants.Remove(variant);
                Reload();
            }
        }
        

        
        private void DrawAddVariantRow(Rect rect, Shader shader)
        {
            // 绘制Add按钮，与Del按钮对齐
            var addRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(addRect, "Add"))
            {
                ShowAddVariantDialog(shader);
            }
        }
        
        private void DrawAddShaderRow(Rect rect)
        {
            // 绘制Add按钮，与Del按钮对齐
            var addRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(addRect, "Add"))
            {
                ShowAddShaderDialog();
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;
            
            // 清除默认绘制
            var rect = args.rowRect;
            
            if (item is ShaderTreeViewItem shaderItem)
            {
                DrawShaderItem(args, shaderItem);
            }
            else if (item is VariantTreeViewItem variantItem)
            {
                DrawVariantItem(args, variantItem);
            }
            else if (item is NewVariantTreeViewItem newVariantItem)
            {
                DrawNewVariantItem(args, newVariantItem);
            }
            else if (item is AddVariantTreeViewItem addVariantItem)
            {
                DrawAddVariantItem(args, addVariantItem);
            }
            else if (item is AddShaderTreeViewItem addShaderItem)
            {
                DrawAddShaderItem(args, addShaderItem);
            }
            else
            {
                // 对于其他类型的item，只显示displayName
                var labelRect = new Rect(rect.x + GetContentIndent(item), rect.y, rect.width - GetContentIndent(item), rect.height);
                EditorGUI.LabelField(labelRect, item.displayName);
            }
        }

        private void DrawShaderItem(RowGUIArgs args, ShaderTreeViewItem item)
        {
            var rect = args.rowRect;
            
            // 只绘制Shader名称，不显示关键字
            var labelRect = new Rect(rect.x + GetContentIndent(item), rect.y, rect.width - GetContentIndent(item) - 60, rect.height);
            EditorGUI.LabelField(labelRect, item.shader != null ? item.shader.name : "<null>", EditorStyles.boldLabel);
            
            // 绘制索引按钮
            var buttonRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(buttonRect, "Locate"))
            {
                Selection.activeObject = item.shader;
                EditorGUIUtility.PingObject(item.shader);
            }
        }

        private void DrawVariantItem(RowGUIArgs args, VariantTreeViewItem item)
        {
            var rect = args.rowRect;
            
            // 绘制变体信息
            var labelRect = new Rect(rect.x + GetContentIndent(item), rect.y, rect.width - GetContentIndent(item) - 120, rect.height);
            EditorGUI.LabelField(labelRect, item.variant.GetKeywordsString());
            
            // 绘制引用按钮
            var refButtonRect = new Rect(rect.x + rect.width - 120, rect.y, 60, rect.height);
            if (GUI.Button(refButtonRect, "References"))
            {
                ShowVariantReferences(item.variant);
            }
        }

        private void DrawNewVariantItem(RowGUIArgs args, NewVariantTreeViewItem item)
        {
            var rect = args.rowRect;
            
            // 绘制新变体信息（淡红色背景）
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.8f, 0.3f); // 淡红色
            
            // 绘制关键字
            var labelRect = new Rect(rect.x + GetContentIndent(item), rect.y, rect.width - GetContentIndent(item) - 180, rect.height);
            EditorGUI.LabelField(labelRect, item.variant.GetKeywordsString(), EditorStyles.helpBox);
            
            // 绘制确认按钮
            var confirmRect = new Rect(rect.x + rect.width - 180, rect.y, 60, rect.height);
            if (GUI.Button(confirmRect, "Confirm"))
            {
                window.OnVariantConfirmed(item.variant);
            }
            
            // 绘制忽略按钮
            var ignoreRect = new Rect(rect.x + rect.width - 120, rect.y, 60, rect.height);
            if (GUI.Button(ignoreRect, "Ignore"))
            {
                window.OnVariantIgnored(item.variant);
            }
            
            // 恢复原始颜色
            GUI.backgroundColor = originalColor;
        }

        private void DrawAddVariantItem(RowGUIArgs args, AddVariantTreeViewItem item)
        {
            var rect = args.rowRect;
            
            var buttonRect = new Rect(rect.x + GetContentIndent(item), rect.y, 100, rect.height);
            if (GUI.Button(buttonRect, "Add Variant"))
            {
                ShowAddVariantDialog(item.shader);
            }
        }

        private void DrawAddShaderItem(RowGUIArgs args, AddShaderTreeViewItem item)
        {
            var rect = args.rowRect;
            
            var buttonRect = new Rect(rect.x + GetContentIndent(item), rect.y, 100, rect.height);
            if (GUI.Button(buttonRect, "Add Shader"))
            {
                ShowAddShaderDialog();
            }
        }

        private void ShowVariantReferences(ShaderVariantData variant)
        {
            var references = FindVariantReferences(variant);
            var materials = ShaderVariantCollector.FindMaterialsForVariant(variant);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"References for variant:");
            sb.AppendLine($"Shader: {variant.shader.name}");
            sb.AppendLine($"Keywords: {variant.GetKeywordsString()}");
            sb.AppendLine();
            sb.AppendLine("Materials:");
            if (materials.Count > 0)
            {
                foreach (var mat in materials)
                {
                    sb.AppendLine($"- {mat.name}");
                }
            }
            else
            {
                sb.AppendLine("- (none)");
            }

            sb.AppendLine();
            sb.AppendLine("Prefabs:");
            if (references.Count > 0)
            {
                foreach (var go in references)
                {
                    sb.AppendLine($"- {go.name}");
                }
            }
            else
            {
                sb.AppendLine("- (none)");
            }

            EditorUtility.DisplayDialog("Variant References", sb.ToString(), "OK");
        }

        private List<GameObject> FindVariantReferences(ShaderVariantData variant)
        {
            return ShaderVariantCollector.FindVariantReferences(variant);
        }

        private void ShowAddVariantDialog(Shader shader)
        {
            AddVariantDialog.ShowDialog(shader, window);
        }

        private void ShowAddShaderDialog()
        {
            AddShaderDialog.ShowDialog(window);
        }

        public void AddNewVariants(List<ShaderVariantData> variants)
        {
            newVariants.AddRange(variants);
            newVariants = SortVariantsByName(newVariants);
        }

        public void RemoveNewVariant(ShaderVariantData variant)
        {
            newVariants.Remove(variant);
        }

        public void ClearNewVariants()
        {
            newVariants.Clear();
        }

        private void DeleteShaderFromCollection(Shader shader)
        {
            if (collection == null || shader == null)
                return;
                
            // 确认删除
            if (!EditorUtility.DisplayDialog("Delete Shader", 
                $"Are you sure you want to delete shader '{shader.name}' and all its variants from the collection?", 
                "Delete", "Cancel"))
            {
                return;
            }
            
            try
            {
                // 获取该Shader的所有变体
                var variants = shaderVariantsMap.ContainsKey(shader) ? shaderVariantsMap[shader] : new List<ShaderVariantData>();
                
                // 删除所有变体（使用对象初始化语法跳过验证）
                foreach (var variant in new List<ShaderVariantData>(variants))
                {
                    try
                    {
                        var kws = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                        var variantToRemove = new ShaderVariantCollection.ShaderVariant
                        {
                            shader = variant.shader,
                            keywords = kws,
                            passType = variant.passType
                        };
                        collection.Remove(variantToRemove);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to remove variant during shader delete: {ex.Message}");
                    }
                }
                
                // 清理空的Shader条目
                CleanupEmptyShaderEntries();
                
                // 标记资源为已修改并保存
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                
                // 刷新显示
                BuildShaderVariantsMap();
                
                Debug.Log($"Deleted shader '{shader.name}' and {variants.Count} variants from collection");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deleting shader: {e.Message}");
            }
        }

        private void DeleteVariantFromCollection(ShaderVariantData variant)
        {
            if (collection == null || variant == null || variant.shader == null)
                return;
                
            // 确认删除
            if (!EditorUtility.DisplayDialog("Delete Variant", 
                $"Are you sure you want to delete variant '{variant.GetKeywordsString()}' from shader '{variant.shader.name}'?", 
                "Delete", "Cancel"))
            {
                return;
            }
            
            try
            {
                // 使用对象初始化语法创建要删除的变体，跳过构造函数验证
                var kws = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                var variantToRemove = new ShaderVariantCollection.ShaderVariant
                {
                    shader = variant.shader,
                    keywords = kws,
                    passType = variant.passType
                };
                
                collection.Remove(variantToRemove);
                
                // 检查该Shader是否还有其他变体
                var remainingVariants = ShaderVariantCollector.CollectVariantsFromCollection(collection)
                    .Where(v => v.shader == variant.shader).ToList();
                
                // 如果没有其他变体了，清理空的Shader条目
                if (remainingVariants.Count == 0)
                {
                    CleanupEmptyShaderEntries();
                }
                
                // 标记资源为已修改并保存
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                
                // 刷新显示
                BuildShaderVariantsMap();
                
                Debug.Log($"Deleted variant '{variant.GetKeywordsString()}' from shader '{variant.shader.name}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deleting variant: {e.Message}");
            }
        }

        private void CollectOnlyThisShader(Shader shader)
        {
            if (shader == null) return;
            // 重建缓存以确保结果最新（与主Collect一致）
            try { SVCCache.RefreshAll(); } catch (System.Exception e) { Debug.LogWarning($"Refresh cache failed: {e.Message}"); }

            // 收集该Shader的变体
            var exclusion = window != null ? window.GetExclusionSettings() : null;
            var collected = ShaderVariantCollector.CollectVariantsForShader(shader, exclusion);
            if (collected == null || collected.Count == 0)
            {
                EditorUtility.DisplayDialog("Collect", $"No new variants found for shader: {shader.name}", "OK");
                return;
            }

            // 过滤已存在的
            var existing = ShaderVariantCollector.CollectVariantsFromCollection(collection);
            var newOnes = collected.Where(v => !existing.Contains(v)).ToList();
            if (newOnes.Count == 0)
            {
                EditorUtility.DisplayDialog("Collect", $"No new variants found for shader: {shader.name}", "OK");
                return;
            }

            // 标记并加入到新变体列表中显示
            foreach (var v in newOnes)
            {
                v.isCollected = true;
                newVariants.Add(v);
            }

            // 展开该Shader并刷新
            expandedShaders.Add(shader);
            Reload();
        }

        private void ConfirmAllNewVariantsForShader(Shader shader)
        {
            if (collection == null || shader == null) return;

            var toConfirm = new List<ShaderVariantData>(newVariants.Where(v => v.shader == shader));
            int success = 0;
            foreach (var variant in toConfirm)
            {
                if (AddCollectedVariantToCollection(variant))
                {
                    success++;
                    newVariants.Remove(variant);
                }
            }

            if (success > 0)
            {
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
            }

            Reload();
        }

        private void IgnoreAllNewVariantsForShader(Shader shader)
        {
            newVariants.RemoveAll(v => v.shader == shader);
            Reload();
        }

        private bool AddCollectedVariantToCollection(ShaderVariantData variant)
        {
            try
            {
                var kws = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                var shaderVariant = new ShaderVariantCollection.ShaderVariant
                {
                    shader = variant.shader,
                    keywords = kws,
                    passType = variant.passType
                };
                collection.Add(shaderVariant);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to add collected variant: {variant.shader?.name} - {variant.GetKeywordsString()} ({ex.Message})");
                return false;
            }
        }

        private void CleanupEmptyShaderEntries()
        {
            if (collection == null) return;

            try
            {
                // 重新构建一个干净的collection来移除空的Shader条目
                var allVariants = ShaderVariantCollector.CollectVariantsFromCollection(collection);
                var tempCollection = new ShaderVariantCollection();
                
                // 将所有非空变体添加到临时collection
                foreach (var variant in allVariants)
                {
                    try
                    {
                        var kws = variant.keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                        var shaderVariant = new ShaderVariantCollection.ShaderVariant
                        {
                            shader = variant.shader,
                            keywords = kws,
                            passType = variant.passType
                        };
                        tempCollection.Add(shaderVariant);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to re-add variant during cleanup: {ex.Message}");
                    }
                }

                // 使用反射清空原collection并复制内容
                var originalShadersField = typeof(ShaderVariantCollection).GetField("m_Shaders", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tempShadersField = typeof(ShaderVariantCollection).GetField("m_Shaders", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (originalShadersField != null && tempShadersField != null)
                {
                    var tempShaders = tempShadersField.GetValue(tempCollection);
                    originalShadersField.SetValue(collection, tempShaders);
                    Debug.Log("Successfully cleaned up empty shader entries from collection.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to cleanup empty shader entries: {ex.Message}");
            }
        }
    }

    // TreeView Item Classes
    public class ShaderTreeViewItem : TreeViewItem
    {
        public Shader shader;
        
        public ShaderTreeViewItem(int id, Shader shader, int depth) : base(id, depth, "")
        {
            this.shader = shader;
        }
    }

    public class VariantTreeViewItem : TreeViewItem
    {
        public ShaderVariantData variant;
        
        public VariantTreeViewItem(int id, ShaderVariantData variant, int depth) : base(id, depth, variant.GetKeywordsString())
        {
            this.variant = variant;
        }
    }

    public class NewVariantTreeViewItem : TreeViewItem
    {
        public ShaderVariantData variant;
        
        public NewVariantTreeViewItem(int id, ShaderVariantData variant, int depth) : base(id, depth, variant.GetKeywordsString())
        {
            this.variant = variant;
        }
    }

    public class AddVariantTreeViewItem : TreeViewItem
    {
        public Shader shader;
        
        public AddVariantTreeViewItem(int id, Shader shader, int depth) : base(id, depth, "Add Variant")
        {
            this.shader = shader;
        }
    }

    public class AddShaderTreeViewItem : TreeViewItem
    {
        public AddShaderTreeViewItem(int id, int depth) : base(id, depth, "Add Shader")
        {
        }
    }
}
