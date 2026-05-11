using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShaderVariantsCollector
{
    public class ShaderVariantsCollectorSettingsProvider : SettingsProvider
    {
        private SerializedObject serializedSettings;
        private ExclusionSettings settings;

        public ShaderVariantsCollectorSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return ExclusionSettings.GetOrCreateSettings() != null;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settings = ExclusionSettings.GetOrCreateSettings();
            serializedSettings = new SerializedObject(settings);
        }

        public override void OnGUI(string searchContext)
        {
            if (settings == null)
            {
                settings = ExclusionSettings.GetOrCreateSettings();
                serializedSettings = new SerializedObject(settings);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shader Variants Collector - 剔除配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            serializedSettings.Update();

            // 剔除的Shader列表
            EditorGUILayout.LabelField("剔除的Shader名称:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("支持部分匹配，例如输入 'Hidden/' 将剔除所有以 'Hidden/' 开头的Shader", MessageType.Info);
            SerializedProperty shadersProperty = serializedSettings.FindProperty("excludedShaderNames");
            EditorGUILayout.PropertyField(shadersProperty, true);

            EditorGUILayout.Space();

            // 剔除的关键字列表
            EditorGUILayout.LabelField("剔除的关键字:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("包含任一关键字的变体将被剔除", MessageType.Info);
            SerializedProperty keywordsProperty = serializedSettings.FindProperty("excludedKeywords");
            EditorGUILayout.PropertyField(keywordsProperty, true);

            EditorGUILayout.Space();

            // 快捷操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置为默认设置"))
            {
                settings.excludedShaderNames.Clear();
                settings.excludedKeywords.Clear();
                
                settings.excludedShaderNames.Add("Hidden/");
                settings.excludedShaderNames.Add("Legacy Shaders/");
                settings.excludedKeywords.Add("_DEBUG");
                settings.excludedKeywords.Add("EDITOR_VISUALIZATION");
                
                EditorUtility.SetDirty(settings);
            }
            
            if (GUILayout.Button("清空所有设置"))
            {
                settings.excludedShaderNames.Clear();
                settings.excludedKeywords.Clear();
                EditorUtility.SetDirty(settings);
            }
            EditorGUILayout.EndHorizontal();

            if (serializedSettings.ApplyModifiedProperties())
            {
                settings.SaveSettings();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateShaderVariantsCollectorSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new ShaderVariantsCollectorSettingsProvider("Project/Shader Variants Collector", SettingsScope.Project);
                provider.keywords = new string[] { "shader", "variants", "collector", "exclusion", "剔除" };
                return provider;
            }

            return null;
        }
    }
} 