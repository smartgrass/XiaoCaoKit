using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Flux;
using XiaoCao;

namespace FluxEditor
{
    /// <summary>
    /// FCommandEvent 的 Inspector:按指令名提供模版下拉框,
    /// 并用编辑器配置驱动 BaseMsg 勾选项与 otherMsgs 预设
    /// </summary>
    [CustomEditor(typeof(FCommandEvent), true)]
    [CanEditMultipleObjects]
    public class FCommandEventInspector : FEventInspector
    {
        private const string TemplateConfigFolder = "Assets/XiaoCaoKit/Editor/ModuleEditor/FluxEditor/XCEditor/Config";
        private const string TemplateConfigAssetPath = TemplateConfigFolder + "/XCCommandTemplateConfig.asset";
        private const string TemplateConfigParentFolder = "Assets/XiaoCaoKit/Editor/ModuleEditor/FluxEditor/XCEditor";
        private const string TemplateConfigFolderName = "Config";

        private static readonly List<XCCommandTemplate> EmptyTemplates = new List<XCCommandTemplate>();

        private SerializedProperty commandNameProperty;
        private SerializedProperty baseMsgProperty;
        private SerializedProperty otherMsgsProperty;

        private string[] commandValues = new string[0];
        private string[] commandDisplayNames = new string[0];
        private int selectedCommandIndex;
        private int selectedTemplateIndex;
        private int selectedPresetIndex;

        private XCCommandTemplateConfig _templateConfig;

        protected override void OnEnable()
        {
            base.OnEnable();

            commandNameProperty = serializedObject.FindProperty("commandName");
            baseMsgProperty = serializedObject.FindProperty("baseMsg");
            otherMsgsProperty = serializedObject.FindProperty("otherMsgs");

            RefreshCommandOptions();
        }

        protected override void DrawEventFields()
        {
            SyncSelectedCommandIndex();
            //每次绘制都重新取资产,避免配置改动后被缓存的旧实例一直显示为 0 条模版
            _templateConfig = LoadTemplateConfig();

            DrawTemplateConfigBar();
            DrawCommandPopup();

            XCCommandTemplateConfig.CommandGroup group = GetCurrentGroup();
            DrawTemplateSelector(group);
            DrawBaseMsg();
            DrawOtherMsgs(group);
        }

        /// <summary>
        /// 绘制模版配置资产的状态,并提供"选中配置"入口(缺失时先创建)
        /// </summary>
        private void DrawTemplateConfigBar()
        {
            EditorGUILayout.BeginHorizontal();

            if (_templateConfig == null)
            {
                EditorGUILayout.HelpBox("未找到指令模版配置资产", MessageType.Warning);
                if (GUILayout.Button("创建配置", GUILayout.Width(70)))
                {
                    _templateConfig = CreateTemplateConfigAsset();
                    SelectTemplateConfig();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"指令模版配置: {_templateConfig.name} (共 {_templateConfig.TotalTemplateCount} 条)", EditorStyles.miniLabel);
                if (GUILayout.Button("选中配置", GUILayout.Width(70)))
                {
                    SelectTemplateConfig();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 在 Project 窗口中选中模版配置资产,方便直接编辑模版内容
        /// </summary>
        private void SelectTemplateConfig()
        {
            if (_templateConfig == null)
            {
                _templateConfig = LoadTemplateConfig();
            }

            if (_templateConfig == null)
            {
                _templateConfig = CreateTemplateConfigAsset();
            }

            Selection.activeObject = _templateConfig;
            EditorGUIUtility.PingObject(_templateConfig);
            EditorUtility.FocusProjectWindow();
        }

        private void DrawCommandPopup()
        {
            EditorGUI.BeginChangeCheck();
            selectedCommandIndex = EditorGUILayout.Popup("Command Name", selectedCommandIndex, commandDisplayNames);
            if (EditorGUI.EndChangeCheck() && selectedCommandIndex >= 0 && selectedCommandIndex < commandValues.Length)
            {
                commandNameProperty.stringValue = commandValues[selectedCommandIndex];
                //指令变化后,之前选中的模版不再适用
                selectedTemplateIndex = 0;
            }
        }

        /// <summary>
        /// 绘制当前指令的模版下拉框:首项显示模版数量,其余为模版名,选中后写入 BaseMsg
        /// </summary>
        private void DrawTemplateSelector(XCCommandTemplateConfig.CommandGroup group)
        {
            List<XCCommandTemplate> templates = group == null ? EmptyTemplates : group.templates;
            string[] options = BuildTemplateOptions(templates);

            if (selectedTemplateIndex < 0 || selectedTemplateIndex >= options.Length)
            {
                selectedTemplateIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            selectedTemplateIndex = EditorGUILayout.Popup("模版", selectedTemplateIndex, options);
            if (EditorGUI.EndChangeCheck() && selectedTemplateIndex > 0)
            {
                ApplyTemplate(templates[selectedTemplateIndex - 1]);
            }

            if (selectedTemplateIndex > 0)
            {
                string description = templates[selectedTemplateIndex - 1].description;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    EditorGUILayout.HelpBox(description, MessageType.Info);
                }
            }
        }

        /// <summary>
        /// 绘制 BaseMsg 字段
        /// </summary>
        private void DrawBaseMsg()
        {
            if (baseMsgProperty == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(baseMsgProperty, true);
        }

        /// <summary>
        /// 绘制 otherMsgs 字段,以及配置里声明的预设
        /// </summary>
        private void DrawOtherMsgs(XCCommandTemplateConfig.CommandGroup group)
        {
            if (otherMsgsProperty == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(otherMsgsProperty, true);

            if (group == null || group.otherMsgPresets.Count == 0)
            {
                return;
            }

            if (selectedPresetIndex < 0 || selectedPresetIndex > group.otherMsgPresets.Count)
            {
                selectedPresetIndex = 0;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OtherMsgs Quick Config", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            selectedPresetIndex = EditorGUILayout.Popup("Template", selectedPresetIndex, BuildPresetOptions(group));
            using (new EditorGUI.DisabledScope(selectedPresetIndex <= 0))
            {
                if (GUILayout.Button("Add", GUILayout.Width(80)))
                {
                    AppendOtherMsgs(group.otherMsgPresets[selectedPresetIndex - 1].values);
                }

                if (GUILayout.Button("Replace", GUILayout.Width(80)))
                {
                    ReplaceOtherMsgs(group.otherMsgPresets[selectedPresetIndex - 1].values);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (selectedPresetIndex > 0)
            {
                List<string> values = group.otherMsgPresets[selectedPresetIndex - 1].values;
                EditorGUILayout.HelpBox($"格式示例: [{string.Join(", ", values.Select(x => $"\"{x}\""))}]", MessageType.Info);
            }
        }

        private void AppendOtherMsgs(List<string> values)
        {
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                int newIndex = otherMsgsProperty.arraySize;
                otherMsgsProperty.InsertArrayElementAtIndex(newIndex);
                otherMsgsProperty.GetArrayElementAtIndex(newIndex).stringValue = value;
            }
        }

        private void ReplaceOtherMsgs(List<string> values)
        {
            otherMsgsProperty.arraySize = 0;
            AppendOtherMsgs(values);
        }

        /// <summary>
        /// 把模版里配置的 BaseMsg 写入当前指令(state 是运行时字段,不参与赋值)
        /// </summary>
        private void ApplyTemplate(XCCommandTemplate template)
        {
            //0默认不修改
            if (template.baseMsg.numMsg != 0)
            {
                baseMsgProperty.FindPropertyRelative("numMsg").floatValue = template.baseMsg.numMsg;
            }

            baseMsgProperty.FindPropertyRelative("strMsg").stringValue = template.baseMsg.strMsg ?? string.Empty;
        }

        private XCCommandTemplateConfig.CommandGroup GetCurrentGroup()
        {
            if (_templateConfig == null)
            {
                return null;
            }

            return _templateConfig.GetGroup(commandNameProperty.stringValue);
        }

        /// <summary>
        /// 生成模版下拉框选项:第 0 项显示模版数量,之后依次为模版名
        /// </summary>
        private static string[] BuildTemplateOptions(List<XCCommandTemplate> templates)
        {
            string[] options = new string[templates.Count + 1];
            options[0] = $"模版数量: {templates.Count}";

            for (int i = 0; i < templates.Count; i++)
            {
                string templateName = templates[i].templateName;
                options[i + 1] = string.IsNullOrWhiteSpace(templateName) ? $"未命名模版 {i + 1}" : templateName;
            }

            return options;
        }

        /// <summary>
        /// 生成 otherMsgs 预设下拉框选项:第 0 项为占位提示
        /// </summary>
        private static string[] BuildPresetOptions(XCCommandTemplateConfig.CommandGroup group)
        {
            string[] options = new string[group.otherMsgPresets.Count + 1];
            options[0] = "Choose Template...";

            for (int i = 0; i < group.otherMsgPresets.Count; i++)
            {
                string presetName = group.otherMsgPresets[i].presetName;
                options[i + 1] = string.IsNullOrWhiteSpace(presetName) ? $"未命名预设 {i + 1}" : presetName;
            }

            return options;
        }

        private void RefreshCommandOptions()
        {
            List<string> names = new List<string> { string.Empty };

            if (XCCommandBinder.Inst != null)
            {
                names.AddRange(XCCommandBinder.Inst.GetAllCommandTypes().Keys.OrderBy(x => x));
            }

            commandValues = names.ToArray();
            commandDisplayNames = commandValues
                .Select(x => string.IsNullOrEmpty(x) ? "Choose Command..." : x)
                .ToArray();

            SyncSelectedCommandIndex();
        }

        private void SyncSelectedCommandIndex()
        {
            if (commandNameProperty == null)
            {
                return;
            }

            selectedCommandIndex = Array.IndexOf(commandValues, commandNameProperty.stringValue);
            if (selectedCommandIndex < 0)
            {
                selectedCommandIndex = 0;
            }
        }

        /// <summary>
        /// 读取指令模版配置资产:优先取约定路径,其次在工程里搜索
        /// </summary>
        private static XCCommandTemplateConfig LoadTemplateConfig()
        {
            XCCommandTemplateConfig config = AssetDatabase.LoadAssetAtPath<XCCommandTemplateConfig>(TemplateConfigAssetPath);
            if (config != null)
            {
                return config;
            }

            string[] guids = AssetDatabase.FindAssets("t:XCCommandTemplateConfig");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                config = AssetDatabase.LoadAssetAtPath<XCCommandTemplateConfig>(path);
                if (config != null)
                {
                    return config;
                }
            }

            return null;
        }

        /// <summary>
        /// 在编辑器目录下创建空的指令模版配置资产
        /// </summary>
        private static XCCommandTemplateConfig CreateTemplateConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder(TemplateConfigFolder))
            {
                AssetDatabase.CreateFolder(TemplateConfigParentFolder, TemplateConfigFolderName);
            }

            XCCommandTemplateConfig config = ScriptableObject.CreateInstance<XCCommandTemplateConfig>();
            AssetDatabase.CreateAsset(config, TemplateConfigAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
