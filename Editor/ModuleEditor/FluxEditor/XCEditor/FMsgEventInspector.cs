using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Flux;
using XiaoCao;

namespace FluxEditor
{
    /// <summary>
    /// FMsgEvent 的 Inspector:为 msgList 里的每个 XCEventMsg 单独绘制面板,
    /// 并按 EntityMsgType 提供模版下拉框(选中后写入 BaseMsg)与模版描述
    /// </summary>
    [CustomEditor(typeof(FMsgEvent), true)]
    [CanEditMultipleObjects]
    public class FMsgEventInspector : FEventInspector
    {
        private const string MsgListPropertyName = "msgList";
        private const string ScriptPropertyName = "m_Script";
        private const string TemplateConfigFolder = "Assets/XiaoCaoKit/Editor/ModuleEditor/FluxEditor/XCEditor/Config";
        private const string TemplateConfigAssetPath = TemplateConfigFolder + "/XCEventMsgTemplateConfig.asset";
        private const string TemplateConfigParentFolder = "Assets/XiaoCaoKit/Editor/ModuleEditor/FluxEditor/XCEditor";
        private const string TemplateConfigFolderName = "Config";

        /// <summary>
        /// 单个消息面板上的数组操作,延迟到绘制结束后执行,避免遍历时下标错乱
        /// </summary>
        private enum ElementAction
        {
            None,
            MoveUp,
            MoveDown,
            Duplicate,
            Remove,
        }

        private static readonly List<XCEventMsgTemplate> EmptyTemplates = new List<XCEventMsgTemplate>();

        private SerializedProperty _msgListProperty;
        private XCEventMsgTemplateConfig _templateConfig;

        /// <summary>每条消息当前选中的模版下标,0 表示停留在"模版数量"选项</summary>
        private readonly List<int> _templateSelections = new List<int>();

        private ElementAction _pendingAction = ElementAction.None;
        private int _pendingActionIndex = -1;

        protected override void OnEnable()
        {
            base.OnEnable();

            _msgListProperty = serializedObject.FindProperty(MsgListPropertyName);
            _templateSelections.Clear();
            _templateConfig = LoadTemplateConfig();
        }

        protected override void DrawEventFields()
        {
            if (_msgListProperty == null)
            {
                base.DrawEventFields();
                return;
            }

            DrawSerializedFieldsExceptMsgList();

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("多选状态下 msgList 使用默认绘制,模版面板仅在单选时可用。", MessageType.Info);
                EditorGUILayout.PropertyField(_msgListProperty, true);
                return;
            }

            //每次绘制都重新取资产,避免配置改动后被缓存的旧实例一直显示为 0 条模版
            _templateConfig = LoadTemplateConfig();

            DrawMsgList();
        }

        /// <summary>
        /// 绘制除 msgList 以外的字段,避免自定义绘制后漏掉事件自身的字段
        /// </summary>
        private void DrawSerializedFieldsExceptMsgList()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == ScriptPropertyName || iterator.propertyPath == MsgListPropertyName)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        /// <summary>
        /// 绘制 msgList 的标题行与每个 XCEventMsg 的独立面板
        /// </summary>
        private void DrawMsgList()
        {
            EditorGUILayout.Space();
            DrawTemplateConfigBar();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Msg List ({_msgListProperty.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("新增", GUILayout.Width(50)))
            {
                AppendElement();
            }
            EditorGUILayout.EndHorizontal();

            SyncTemplateSelections();

            for (int i = 0; i < _msgListProperty.arraySize; i++)
            {
                DrawMsgElement(i);
            }

            ApplyPendingAction();
        }

        /// <summary>
        /// 绘制单条 XCEventMsg 面板:消息类型、BaseMsg、反转标记与模版下拉框
        /// </summary>
        private void DrawMsgElement(int index)
        {
            SerializedProperty element = _msgListProperty.GetArrayElementAtIndex(index);
            SerializedProperty msgTypeProperty = element.FindPropertyRelative("msgType");
            SerializedProperty baseMsgProperty = element.FindPropertyRelative("baseMsg");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, $"[{index}] {GetMsgTypeLabel(msgTypeProperty)}", true);
            if (GUILayout.Button("删除", GUILayout.Width(46)))
            {
                SetPendingAction(ElementAction.Remove, index);
            }
            EditorGUILayout.EndHorizontal();

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(msgTypeProperty);
                if (EditorGUI.EndChangeCheck())
                {
                    //消息类型变化后,之前选中的模版不再适用
                    _templateSelections[index] = 0;
                }

                EditorGUILayout.PropertyField(baseMsgProperty, true);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("isUndoOnFinish"));
                DrawTemplateSelector(index, msgTypeProperty, element);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("上移"))
                {
                    SetPendingAction(ElementAction.MoveUp, index);
                }
                if (GUILayout.Button("下移"))
                {
                    SetPendingAction(ElementAction.MoveDown, index);
                }
                if (GUILayout.Button("复制"))
                {
                    SetPendingAction(ElementAction.Duplicate, index);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制模版下拉框:首项显示模版数量,其余为模版名,选中后把模版的 BaseMsg 写入当前消息
        /// </summary>
        private void DrawTemplateSelector(int index, SerializedProperty msgTypeProperty, SerializedProperty element)
        {
            List<XCEventMsgTemplate> templates = GetTemplates(msgTypeProperty);
            string[] options = BuildTemplateOptions(templates);

            int selection = _templateSelections[index];
            if (selection < 0 || selection >= options.Length)
            {
                selection = 0;
            }

            EditorGUI.BeginChangeCheck();
            selection = EditorGUILayout.Popup("模版", selection, options);
            if (EditorGUI.EndChangeCheck())
            {
                _templateSelections[index] = selection;
                if (selection > 0)
                {
                    ApplyTemplate(element, templates[selection - 1]);
                }
            }

            if (selection > 0)
            {
                string description = templates[selection - 1].description;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    EditorGUILayout.HelpBox(description, MessageType.Info);
                }
            }
        }

        /// <summary>
        /// 绘制模版配置资产的状态,并提供"选中配置"入口(缺失时先创建)
        /// </summary>
        private void DrawTemplateConfigBar()
        {
            EditorGUILayout.BeginHorizontal();

            if (_templateConfig == null)
            {
                EditorGUILayout.HelpBox("未找到消息模版配置资产", MessageType.Warning);
                if (GUILayout.Button("创建配置", GUILayout.Width(70)))
                {
                    _templateConfig = CreateTemplateConfigAsset();
                    SelectTemplateConfig();
                }
            }
            else
            {
                // EditorGUILayout.LabelField($"模版配置: {_templateConfig.name}", EditorStyles.miniLabel);
                EditorGUILayout.ObjectField("配置模版",_templateConfig, typeof(Object));
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

        /// <summary>
        /// 在末尾追加一条消息,内容以最后一条为副本,便于连续配置同类消息
        /// </summary>
        private void AppendElement()
        {
            if (_msgListProperty.arraySize == 0)
            {
                _msgListProperty.InsertArrayElementAtIndex(0);
                _msgListProperty.GetArrayElementAtIndex(0).isExpanded = true;
            }
            else
            {
                DuplicateElement(_msgListProperty.arraySize - 1);
            }

            ReloadMsgListProperty();
            ResetTemplateSelections();
        }

        /// <summary>
        /// 在第 index 条之后插入一条副本(state 是运行时字段,不参与复制)
        /// </summary>
        private void DuplicateElement(int index)
        {
            SerializedProperty source = _msgListProperty.GetArrayElementAtIndex(index);
            int msgType = source.FindPropertyRelative("msgType").intValue;
            SerializedProperty sourceBaseMsg = source.FindPropertyRelative("baseMsg");
            float numMsg = sourceBaseMsg.FindPropertyRelative("numMsg").floatValue;
            string strMsg = sourceBaseMsg.FindPropertyRelative("strMsg").stringValue;
            bool isUndoOnFinish = source.FindPropertyRelative("isUndoOnFinish").boolValue;

            int newIndex = index + 1;
            _msgListProperty.InsertArrayElementAtIndex(newIndex);

            //InsertArrayElementAtIndex 的复制行为在不同编辑器版本上不一致,这里显式写入,保证结果确定
            SerializedProperty target = _msgListProperty.GetArrayElementAtIndex(newIndex);
            target.FindPropertyRelative("msgType").intValue = msgType;
            SerializedProperty targetBaseMsg = target.FindPropertyRelative("baseMsg");
            targetBaseMsg.FindPropertyRelative("numMsg").floatValue = numMsg;
            targetBaseMsg.FindPropertyRelative("strMsg").stringValue = strMsg;
            target.FindPropertyRelative("isUndoOnFinish").boolValue = isUndoOnFinish;
            target.isExpanded = true;
        }

        /// <summary>
        /// 应用本帧缓存的数组操作
        /// </summary>
        private void ApplyPendingAction()
        {
            if (_pendingAction == ElementAction.None)
            {
                return;
            }

            int index = _pendingActionIndex;
            switch (_pendingAction)
            {
                case ElementAction.MoveUp:
                    if (index > 0)
                    {
                        _msgListProperty.MoveArrayElement(index, index - 1);
                    }
                    break;
                case ElementAction.MoveDown:
                    if (index < _msgListProperty.arraySize - 1)
                    {
                        _msgListProperty.MoveArrayElement(index, index + 1);
                    }
                    break;
                case ElementAction.Duplicate:
                    DuplicateElement(index);
                    break;
                case ElementAction.Remove:
                    _msgListProperty.DeleteArrayElementAtIndex(index);
                    break;
            }

            _pendingAction = ElementAction.None;
            _pendingActionIndex = -1;

            ReloadMsgListProperty();
            ResetTemplateSelections();
        }

        private void SetPendingAction(ElementAction action, int index)
        {
            _pendingAction = action;
            _pendingActionIndex = index;
        }

        private void ReloadMsgListProperty()
        {
            //数组结构变化后重新取属性,避免继续使用失效的 SerializedProperty
            _msgListProperty = serializedObject.FindProperty(MsgListPropertyName);
        }

        /// <summary>
        /// 让模版选择状态与 msgList 数量保持一致
        /// </summary>
        private void SyncTemplateSelections()
        {
            while (_templateSelections.Count < _msgListProperty.arraySize)
            {
                _templateSelections.Add(0);
            }

            while (_templateSelections.Count > _msgListProperty.arraySize)
            {
                _templateSelections.RemoveAt(_templateSelections.Count - 1);
            }
        }

        private void ResetTemplateSelections()
        {
            _templateSelections.Clear();
        }

        /// <summary>
        /// 取出与当前消息类型匹配的模版列表
        /// </summary>
        private List<XCEventMsgTemplate> GetTemplates(SerializedProperty msgTypeProperty)
        {
            if (_templateConfig == null)
            {
                return EmptyTemplates;
            }

            return _templateConfig.GetTemplates((EntityMsgType)msgTypeProperty.intValue);
        }

        /// <summary>
        /// 生成下拉框选项:第 0 项显示模版数量,之后依次为模版名
        /// </summary>
        private static string[] BuildTemplateOptions(List<XCEventMsgTemplate> templates)
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
        /// 把模版里配置的 BaseMsg 写入当前消息(state 是运行时字段,不参与赋值);
        /// 模版勾选了 needIsUndoOnFinish 时才覆盖消息的反转标记,否则保留原数据
        /// </summary>
        private static void ApplyTemplate(SerializedProperty element, XCEventMsgTemplate template)
        {
            SerializedProperty baseMsgProperty = element.FindPropertyRelative("baseMsg");
            baseMsgProperty.FindPropertyRelative("numMsg").floatValue = template.baseMsg.numMsg;
            baseMsgProperty.FindPropertyRelative("strMsg").stringValue = template.baseMsg.strMsg ?? string.Empty;

            if (template.needIsUndoOnFinish)
            {
                element.FindPropertyRelative("isUndoOnFinish").boolValue = true;
            }
        }

        /// <summary>
        /// 取折叠标题上显示的消息类型名
        /// </summary>
        private static string GetMsgTypeLabel(SerializedProperty msgTypeProperty)
        {
            string[] displayNames = msgTypeProperty.enumDisplayNames;
            int index = msgTypeProperty.enumValueIndex;
            if (displayNames != null && index >= 0 && index < displayNames.Length)
            {
                return displayNames[index];
            }

            return "未设置";
        }

        /// <summary>
        /// 读取模版配置资产:优先取约定路径,其次在工程里搜索
        /// </summary>
        private static XCEventMsgTemplateConfig LoadTemplateConfig()
        {
            XCEventMsgTemplateConfig config = AssetDatabase.LoadAssetAtPath<XCEventMsgTemplateConfig>(TemplateConfigAssetPath);
            if (config != null)
            {
                return config;
            }

            string[] guids = AssetDatabase.FindAssets("t:XCEventMsgTemplateConfig");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                config = AssetDatabase.LoadAssetAtPath<XCEventMsgTemplateConfig>(path);
                if (config != null)
                {
                    return config;
                }
            }

            return null;
        }

        /// <summary>
        /// 在编辑器目录下创建空的模版配置资产
        /// </summary>
        private static XCEventMsgTemplateConfig CreateTemplateConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder(TemplateConfigFolder))
            {
                AssetDatabase.CreateFolder(TemplateConfigParentFolder, TemplateConfigFolderName);
            }

            XCEventMsgTemplateConfig config = ScriptableObject.CreateInstance<XCEventMsgTemplateConfig>();
            AssetDatabase.CreateAsset(config, TemplateConfigAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
