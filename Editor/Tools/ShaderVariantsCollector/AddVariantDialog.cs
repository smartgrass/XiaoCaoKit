using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShaderVariantsCollector
{
    public class AddVariantDialog : EditorWindow
    {
        private Shader targetShader;
        private UnityEngine.Rendering.PassType selectedPassType = UnityEngine.Rendering.PassType.Normal;
        private List<string> availableKeywords = new List<string>();
        private List<string> selectedKeywords = new List<string>();
        private Vector2 keywordScrollPosition;
        private ShaderVariantsCollectorWindow parentWindow;
        private string customKeyword = "";
        private bool showCustomKeyword = false;

        public static void ShowDialog(Shader shader, ShaderVariantsCollectorWindow parent)
        {
            var window = GetWindow<AddVariantDialog>("Add Variant");
            window.targetShader = shader;
            window.parentWindow = parent;
            window.minSize = new Vector2(560, 560);
            window.selectedPassType = UnityEngine.Rendering.PassType.Normal;
            window.UpdateAvailableKeywords();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            
            // 显示目标Shader
            EditorGUILayout.LabelField("Target Shader:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Shader", targetShader, typeof(Shader), false);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 选择 PassType
            EditorGUILayout.LabelField("Pass Type:", EditorStyles.boldLabel);
            var newPass = (UnityEngine.Rendering.PassType)EditorGUILayout.EnumPopup("Pass", selectedPassType);
            if (newPass != selectedPassType)
            {
                selectedPassType = newPass;
                UpdateAvailableKeywords();
            }
            
            EditorGUILayout.Space();
            
            if (targetShader != null)
            {
                // 显示帮助信息
                EditorGUILayout.HelpBox(
                    "Select keywords that you want to include in this shader variant. " +
                    "Keywords that are not supported by the shader will be validated when you click 'Add Variant'.", 
                    MessageType.Info);
                
                EditorGUILayout.Space();
                
                // 可用关键字
                EditorGUILayout.LabelField($"Available Keywords ({availableKeywords.Count}):", EditorStyles.boldLabel);
                keywordScrollPosition = EditorGUILayout.BeginScrollView(keywordScrollPosition, GUILayout.Height(200));
                
                foreach (var keyword in availableKeywords)
                {
                    bool isSelected = selectedKeywords.Contains(keyword);
                    string displayName = string.IsNullOrEmpty(keyword) ? "No Keywords" : keyword;

                    // 计算选择该关键字后的新组合
                    var prospective = new HashSet<string>(selectedKeywords);
                    if (!isSelected) prospective.Add(keyword);
                    else prospective.Remove(keyword);

                    // 规则1：禁止导致与集合中已有变体完全重复的组合
                    bool wouldDuplicate = parentWindow != null && parentWindow.CollectionHasVariant(
                        targetShader, selectedPassType, prospective);

                    // 规则2：No Keywords 与其他关键字互斥
                    bool hasNoKeywordSelected = selectedKeywords.Contains("");
                    bool isNoKeyword = string.IsNullOrEmpty(keyword);
                    bool exclusivityBlocked = (hasNoKeywordSelected && !isNoKeyword) || (isNoKeyword && selectedKeywords.Any(k => !string.IsNullOrEmpty(k)));

                    // 规则3：在当前 Pass 下，选择该关键字后组合必须可构造
                    bool wouldBeInvalid = false;
                    var kwsArr = prospective.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                    try
                    {
                        var _ = new ShaderVariantCollection.ShaderVariant(targetShader, selectedPassType, kwsArr);
                    }
                    catch (Exception e)
                    {
                        // Debug.LogException(e);
                        wouldBeInvalid = true;
                    }

                    bool disabled = wouldDuplicate || exclusivityBlocked || wouldBeInvalid;
                    using (new EditorGUI.DisabledScope(disabled))
                    {
                        bool newSelected = EditorGUILayout.ToggleLeft(displayName, isSelected);
                        if (newSelected != isSelected && !disabled)
                        {
                            if (newSelected)
                                selectedKeywords.Add(keyword);
                            else
                                selectedKeywords.Remove(keyword);
                        }
                    }

                    if (disabled)
                    {
                        string reason = wouldDuplicate ? "This combination already exists in the collection." : "This combination is not valid for any pass.";
                        var helpStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, normal = { textColor = Color.gray } };
                        EditorGUILayout.LabelField($"  - {reason}", helpStyle);
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space();
                
                // 选中的关键字
                EditorGUILayout.LabelField("Selected Keywords:", EditorStyles.boldLabel);
                if (selectedKeywords.Count == 0)
                {
                    EditorGUILayout.HelpBox("No keywords selected.", MessageType.Info);
                }
                else
                {
                    var displayKeywords = selectedKeywords.Select(k => string.IsNullOrEmpty(k) ? "No Keywords" : k);
                    EditorGUILayout.LabelField(string.Join(", ", displayKeywords), EditorStyles.helpBox);
                }
                
                EditorGUILayout.Space();
                
                // 按钮
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Add Variant"))
                {
                    AddVariantToCollection();
                }
                
                // 显示当前选择的 Pass 与关键词提示
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Pass: {selectedPassType}", GUILayout.Width(160));
                
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No shader selected.", MessageType.Error);
            }
        }

        private void UpdateAvailableKeywords()
        {
            availableKeywords.Clear();
            selectedKeywords.Clear();
            
            if (targetShader == null)
            {
                return;
            }
            
            Debug.Log($"Updating keywords for shader: {targetShader.name} - Pass: {selectedPassType}");
            
            try
            {
                // 优先使用 LocalKeywordSpace 读取所有本地关键字
                var localSpace = targetShader.keywordSpace;
                var localKeywords = localSpace.keywords;    // LocalKeyword[]
                
                // 基于所选 PassType 做支持性过滤：
                // 策略：尝试用单一关键字创建一个临时 ShaderVariant；能创建成功则认为该关键字对该 Pass 支持
                for (int i = 0; i < localKeywords.Length; i++)
                {
                    string name = localKeywords[i].name;
                    if (string.IsNullOrEmpty(name)) continue;
                    try
                    {
                        // 验证该关键字在所选 PassType 下是否可用
                        var test = new ShaderVariantCollection.ShaderVariant(targetShader, selectedPassType, new string[]{ name });
                        availableKeywords.Add(name);
                    }
                    catch
                    {
                        // 该关键字在此 PassType 下不可用，忽略
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to read/filter LocalKeywordSpace for shader {targetShader.name}: {e.Message}");
            }
            
            // 不再进行推断，若读取为空，则仅提供 No Keywords 选项
            
            // 始终提供 No Keywords 选项（空字符串）
            if (!availableKeywords.Contains(""))
            {
                availableKeywords.Insert(0, "");
            }
            
            // 去重并排序
            availableKeywords = availableKeywords.Distinct().OrderBy(k => k).ToList();
            Debug.Log($"Final keywords for {targetShader.name}: {availableKeywords.Count}");
        }
        
        private List<string> GetShaderKeywords(Shader shader)
        {
            var keywords = new List<string>();
            try
            {
                var localSpace = shader.keywordSpace;
                var localKeywords = localSpace.keywords;
                for (int i = 0; i < localKeywords.Length; i++)
                {
                    string name = localKeywords[i].name;
                    if (string.IsNullOrEmpty(name)) continue;
                    try
                    {
                        var test = new ShaderVariantCollection.ShaderVariant(shader, selectedPassType, new string[]{ name });
                        keywords.Add(name);
                    }
                    catch { }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to get LocalKeywordSpace for {shader.name}: {e.Message}");
            }
            
            if (!keywords.Contains(""))
                keywords.Insert(0, "");
            
            return keywords.Distinct().OrderBy(k => k).ToList();
        }
        
        private void AddVariantToCollection()
        {
            if (targetShader == null)
            {
                EditorUtility.DisplayDialog("Error", "No shader selected.", "OK");
                return;
            }
            
            // 不在对话框中进行验证，交给主窗口尝试不同的 PassType 以避免在 OnGUI 中抛异常
            try
            {
                var variant = new ShaderVariantData(targetShader, selectedPassType, selectedKeywords);
                if (parentWindow != null)
                {
                    parentWindow.OnVariantConfirmed(variant);
                }
                Close();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to add variant: {e.Message}", "OK");
                Debug.LogError($"Failed to add shader variant: {e}");
            }
        }
    }
} 