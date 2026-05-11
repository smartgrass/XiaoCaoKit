using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ShaderVariantsCollector
{
    public class AddShaderDialog : EditorWindow
    {
        private Shader selectedShader;
        private PassType selectedPassType = PassType.Normal;
        private List<string> availableKeywords = new List<string>();
        private List<string> selectedKeywords = new List<string>();
        private Vector2 keywordScrollPosition;
        private ShaderVariantsCollectorWindow parentWindow;

        public static void ShowDialog(ShaderVariantsCollectorWindow parent)
        {
            var window = GetWindow<AddShaderDialog>("Add Shader");
            window.parentWindow = parent;
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            
            // Shader选择
            EditorGUILayout.LabelField("Select Shader:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedShader = (Shader)EditorGUILayout.ObjectField("Shader", selectedShader, typeof(Shader), false);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateAvailableKeywords();
            }
            
            EditorGUILayout.Space();
            
            // PassType 选择
            EditorGUILayout.LabelField("Pass Type:", EditorStyles.boldLabel);
            var newPass = (PassType)EditorGUILayout.EnumPopup("Pass", selectedPassType);
            if (newPass != selectedPassType)
            {
                selectedPassType = newPass;
                UpdateAvailableKeywords();
            }
            
            EditorGUILayout.Space();
            
            if (selectedShader != null)
            {
                // 可用关键字
                EditorGUILayout.LabelField("Available Keywords:", EditorStyles.boldLabel);
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
                        selectedShader, selectedPassType, prospective);

                    // 规则2：No Keywords 与其他关键字互斥
                    bool hasNoKeywordSelected = selectedKeywords.Contains("");
                    bool isNoKeyword = string.IsNullOrEmpty(keyword);
                    bool exclusivityBlocked = (hasNoKeywordSelected && !isNoKeyword) || (isNoKeyword && selectedKeywords.Any(k => !string.IsNullOrEmpty(k)));

                    // 规则3：在当前 Pass 下，选择该关键字后组合必须可构造
                    bool wouldBeInvalid = false;
                    var kwsArr = prospective.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                    try
                    {
                        var _ = new ShaderVariantCollection.ShaderVariant(selectedShader, selectedPassType, kwsArr);
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
                if (selectedKeywords.Count > 0)
                {
                    EditorGUILayout.LabelField("Selected Keywords:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(string.Join(", ", selectedKeywords), EditorStyles.helpBox);
                }
                
                EditorGUILayout.Space();
                
                // 按钮
                EditorGUILayout.BeginHorizontal();
                
                            if (GUILayout.Button("Add Shader"))
            {
                AddShaderToCollection();
            }
            
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
                EditorGUILayout.HelpBox("Please select a shader first.", MessageType.Info);
            }
        }

        private void UpdateAvailableKeywords()
        {
            availableKeywords.Clear();
            selectedKeywords.Clear();
            
            if (selectedShader == null)
                return;
            
            try
            {
                // 读取 LocalKeywordSpace 并按所选 PassType 过滤
                var localSpace = selectedShader.keywordSpace;
                var localKeywords = localSpace.keywords; // LocalKeyword[]
                for (int i = 0; i < localKeywords.Length; i++)
                {
                    string name = localKeywords[i].name;
                    if (string.IsNullOrEmpty(name)) continue;
                    try
                    {
                        var test = new ShaderVariantCollection.ShaderVariant(selectedShader, selectedPassType, new string[]{ name });
                        availableKeywords.Add(name);
                    }
                    catch
                    {
                        // 不支持该 PassType 的关键字，忽略
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to read/filter LocalKeywordSpace for shader {selectedShader.name}: {e.Message}");
            }
            
            // 提供 No Keywords 选项
            if (!availableKeywords.Contains(""))
                availableKeywords.Insert(0, "");
            
            availableKeywords = availableKeywords.Distinct().OrderBy(k => k).ToList();
        }

        private void AddShaderToCollection()
        {
            if (selectedShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a shader first.", "OK");
                return;
            }
            
            // 创建变体数据（包含 PassType）
            var variant = new ShaderVariantData(selectedShader, selectedPassType, selectedKeywords);
            
            // 通知父窗口添加变体
            if (parentWindow != null)
            {
                parentWindow.OnVariantConfirmed(variant);
            }
            
            Close();
        }
    }
} 