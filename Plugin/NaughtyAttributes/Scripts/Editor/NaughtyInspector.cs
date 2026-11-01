using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using XiaoCao;
using Object = UnityEngine.Object;

namespace NaughtyAttributes.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class NaughtyInspector : UnityEditor.Editor
    {
        private List<SerializedProperty> _serializedProperties = new List<SerializedProperty>();
        private IEnumerable<FieldInfo> _nonSerializedFields;

        private IEnumerable<PropertyInfo> _nativeProperties;

        //private IEnumerable<MethodInfo> _methods;
        private Dictionary<int, List<MethodInfo>> _methodDic;
        private List<int> methPosSortList;

        private Dictionary<string, SavedBool> _foldouts = new Dictionary<string, SavedBool>();
        private Dictionary<Object, SerializedObject> SoDic = new Dictionary<Object, SerializedObject>();

        // 当前是否处在 [HorLayout(true)] 打开的水平行里, 以及打开该行的字段名(用于失衡告警)
        private bool _rowOpen;
        private string _rowOpener;
        private bool _rowWarned;

        protected virtual void OnEnable()
        {
            _nonSerializedFields = ReflectionUtility.GetAllFields(
                target, f => f.GetCustomAttributes(typeof(ShowNonSerializedFieldAttribute), true).Length > 0);

            _nativeProperties = ReflectionUtility.GetAllProperties(
                target, p => p.GetCustomAttributes(typeof(ShowNativePropertyAttribute), true).Length > 0);

            GetSortMethods();
        }

        private void GetSortMethods()
        {
            _methodDic = GetMethodInfoDic(target, out var sortList);
            methPosSortList = sortList;
        }

        public static Dictionary<int, List<MethodInfo>> GetMethodInfoDic(object target, out List<int> sortList)
        {
            IEnumerable<MethodInfo> allMethods = ReflectionUtility.GetAllMethods(target, m => true);
            var methodDic = new Dictionary<int, List<MethodInfo>>();
            sortList = new List<int>();
            foreach (var item in allMethods)
            {
                var ButtonAttributes = item.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (ButtonAttributes.Length > 0)
                {
                    ButtonAttribute att = ButtonAttributes[0] as ButtonAttribute;
                    if (!methodDic.ContainsKey(att.Pos))
                    {
                        //nmae
                        methodDic.Add(att.Pos, new List<MethodInfo>() { item });
                        sortList.Add(att.Pos);
                    }
                    else
                    {
                        methodDic[att.Pos].Add(item);
                    }
                }
            }

            sortList.Sort();
            return methodDic;
        }


        protected virtual void OnDisable()
        {
            ReorderableListPropertyDrawer.Instance.ClearCache();
        }


        /// <summary>
        /// 绘制
        ///SerializedProperty property = this.serializedObject.GetIterator();
        ///NaughtyInspector.DrawContent(property);
        /// </summary>
        /// <param name="property"></param>
        public static void DrawContent(SerializedProperty property)
        {
            bool isExpend = true; //第一次是整个类, 需要展开子项
            int index = 0;
            while (property.NextVisible(isExpend))
            {
                if (!isExpend)
                {
                    if (PropertyUtility.IsVisible(property))
                    {
                        EditorGUI.BeginChangeCheck();


                        EditorGUILayout.PropertyField(property, new GUIContent(PropertyUtility.GetLabel(property)),
                            true);


                        if (EditorGUI.EndChangeCheck())
                        {
                            //EditorUtility.SetDirty(this);
                            //this.SaveChanges();
                            PropertyUtility.CallOnValueChangedCallbacks(property);
                        }
                    }
                }

                isExpend = false; //关闭展开子项
                index++;
            }
        }

        public override void OnInspectorGUI()
        {
            GetSerializedProperties(ref _serializedProperties);

            bool anyNaughtyAttribute =
                _serializedProperties.Any(p => PropertyUtility.GetAttribute<INaughtyAttribute>(p) != null);
            if (!anyNaughtyAttribute && methPosSortList.Count == 0)
            {
                DrawDefaultInspector();
            }
            else
            {
                DrawSerializedProperties();
            }

            DrawNonSerializedFields();
            DrawNativeProperties();
            DrawButtons();

            if (IsDrawDebug(target.GetType()))
            {
                ComponentViewHelper.Draw(target);
            }
        }

        public bool IsDrawDebug(Type type)
        {
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return false;
            }

            return true;
        }

        protected void DrawButtons(bool drawHeader = true)
        {
            foreach (var item in methPosSortList)
            {
                if (item >= 0)
                {
                    NaughtyEditorGUI.ButtonList(serializedObject.targetObject, _methodDic[item], item != 0);
                }
            }
        }

        protected void GetSerializedProperties(ref List<SerializedProperty> outSerializedProperties)
        {
            outSerializedProperties.Clear();
            using (var iterator = serializedObject.GetIterator())
            {
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        outSerializedProperties.Add(serializedObject.FindProperty(iterator.name));
                    } while (iterator.NextVisible(false));
                }
            }
        }


        //弃用
        //public bool CheckDrawSubProperty(SerializedProperty property)
        //{
        //if (property.propertyType == SerializedPropertyType.ObjectReference)
        //{
        //    if (property.objectReferenceValue && PropertyUtility.GetAttribute<SerializeField>(property) != null)
        //    {
        //        var so = GetSO(property.objectReferenceValue);

        //        property.objectReferenceValue = EditorGUILayout.ObjectField(PropertyUtility.GetLabel(property), property.objectReferenceValue, typeof(Object), true);


        //        if (property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName))
        //        {
        //            NaughtyEditorGUI.DoDrawDefaultInspector(so);
        //        }
        //        return true;
        //    }
        //}
        //return false;
        //}


        protected void DrawSerializedProperties()
        {
            serializedObject.Update();

            DrawLayoutProperties();
            DrawFoldoutProperties();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 逐个字段按 IXCDrawAttribute 声明的阶段绘制。
        /// [HorLayout(true)] 打开的水平行会跨字段存在, 一直延续到后面出现 [HorLayout(false)] 的字段;
        /// 用 try/finally 兜底, 避免字段绘制抛异常时漏掉 EndHorizontal 导致整个面板布局错乱。
        /// </summary>
        private void DrawLayoutProperties()
        {
            _rowOpen = false;
            _rowOpener = null;
            int i = 1;
            try
            {
                foreach (var property in _serializedProperties)
                {
                    if (DrawScriptHander(property))
                    {
                        continue;
                    }

                    DrawPropertyWithLayout(property);

                    // 按钮: [Button(pos = -i)] 挂在该字段下方
                    if (_methodDic.TryGetValue(-i, out var methods))
                    {
                        NaughtyEditorGUI.ButtonList(serializedObject.targetObject, methods);
                    }

                    i++;
                }
            }
            finally
            {
                if (_rowOpen)
                {
                    WarnUnbalancedRow();
                    CloseRow();
                }
            }
        }

        /// <summary>打开水平行, 已在行内时不再嵌套</summary>
        private void OpenRow(SerializedProperty property)
        {
            if (_rowOpen)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            _rowOpen = true;
            _rowOpener = property.name;
        }

        /// <summary>关闭水平行, 没有打开过则忽略</summary>
        private void CloseRow()
        {
            if (!_rowOpen)
            {
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            _rowOpen = false;
            _rowOpener = null;
        }

        /// <summary>[HorLayout(true)] 没有配套的 [HorLayout(false)] 时告警, 每个 Inspector 只报一次, 避免刷屏</summary>
        private void WarnUnbalancedRow()
        {
            if (_rowWarned)
            {
                return;
            }

            _rowWarned = true;
            Debug.LogWarning($"[{target.GetType().Name}] 字段 {_rowOpener} 上的 [HorLayout(true)] 没有配套的 [HorLayout(false)], 已强制收尾");
        }

        /// <summary>
        /// 按阶段绘制单个字段: 整行前置(标题) -> 行边界 -> 字段本体 -> 行内控件(按钮)
        /// </summary>
        private void DrawPropertyWithLayout(SerializedProperty property)
        {
            IXCDrawAttribute[] xcLayouts = PropertyUtility.GetAttributes<IXCDrawAttribute>(property);

            // 0. [Foldout] 之类声明了 Skip 的字段交给分组流程绘制
            foreach (var xcLayout in xcLayouts)
            {
                if (xcLayout.Stage == XCDrawStage.Skip)
                {
                    return;
                }
            }

            // 1. 标题类特性必须画在水平行之外, 画进行内会和标签/输入框互相挤占宽度
            foreach (var xcLayout in xcLayouts)
            {
                if (xcLayout.Stage == XCDrawStage.Before)
                {
                    xcLayout.OnDraw(serializedObject.targetObject, null);
                }
            }

            bool beginRow = false;
            bool endRow = false;
            bool hasInline = false;
            foreach (var xcLayout in xcLayouts)
            {
                switch (xcLayout.Stage)
                {
                    case XCDrawStage.BeginRow:
                        beginRow = true;
                        break;
                    case XCDrawStage.EndRow:
                        endRow = true;
                        break;
                    case XCDrawStage.Inline:
                        hasInline = true;
                        break;
                }
            }

            // 2. 带 [Header]/[Space] 的字段由 Unity 自己画标题, 必须独占整行, 不参与水平行
            bool standalone = NaughtyEditorGUI.HasBuiltinDecorators(property);
            bool openedHere = false;
            if (!standalone && (beginRow || hasInline))
            {
                openedHere = !_rowOpen;
                OpenRow(property);
            }

            // 3. 字段本体
            NaughtyEditorGUI.PropertyField_Layout(property, true);

            // 4. 行内控件(如 [MiniBtn] 按钮), 紧跟在字段后面
            if (!standalone)
            {
                foreach (var xcLayout in xcLayouts)
                {
                    if (xcLayout.Stage == XCDrawStage.Inline)
                    {
                        xcLayout.OnDraw(serializedObject.targetObject, null);
                    }
                }
            }

            // 5. 关行:
            //    声明了 EndRow 的字段负责收尾;
            //    只有 [MiniBtn] 这类没有 BeginRow 的行内字段是自开自行(本字段画完就关);
            //    [HorLayout(true)] 打开的行留给后续字段, 由后面带 [HorLayout(false)] 的字段关闭
            if (endRow || (openedHere && hasInline && !beginRow))
            {
                CloseRow();
            }
        }

        /// <summary>绘制 [Foldout] 分组的字段</summary>
        private void DrawFoldoutProperties()
        {
            foreach (var group in GetFoldoutProperties(_serializedProperties))
            {
                IEnumerable<SerializedProperty> visibleProperties = group.Where(p => PropertyUtility.IsVisible(p));
                if (!visibleProperties.Any())
                {
                    continue;
                }

                if (!_foldouts.ContainsKey(group.Key))
                {
                    _foldouts[group.Key] = new SavedBool($"{target.GetEntityId()}.{group.Key}", false);
                }

                _foldouts[group.Key].Value = EditorGUILayout.Foldout(_foldouts[group.Key].Value, group.Key);
                if (_foldouts[group.Key].Value)
                {
                    foreach (var property in visibleProperties)
                    {
                        NaughtyEditorGUI.PropertyField_Layout(property, true);
                    }
                }
            }
        }

        private bool DrawScriptHander(SerializedProperty property)
        {
            bool isHander = property.name.Equals("m_Script", System.StringComparison.Ordinal);
            if (!isHander)
            {
                return false;
            }

            using (new EditorGUI.DisabledScope(disabled: true))
            {
                if (property.objectReferenceValue == null)
                {
                    //MonoScript ms = MonoScript.FromScriptableObject(so);  
                    var namePro = serializedObject.FindProperty("m_Name");
                    EditorGUILayout.ObjectField(namePro.stringValue, targets[0], typeof(Object), true);
                }
                else
                {
                    NaughtyEditorGUI.PropertyField_Layout(property, true);
                }
            }

            return true;
        }


        protected void DrawNonSerializedFields(bool drawHeader = false)
        {
            if (_nonSerializedFields.Any())
            {
                if (drawHeader)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Non-Serialized Fields", GetHeaderGUIStyle());
                    NaughtyEditorGUI.HorizontalLine(
                        EditorGUILayout.GetControlRect(false), HorizontalLineAttribute.DefaultHeight,
                        HorizontalLineAttribute.DefaultColor.GetColor());
                }

                foreach (var field in _nonSerializedFields)
                {
                    NaughtyEditorGUI.NonSerializedField_Layout(serializedObject.targetObject, field);
                }
            }
        }

        protected void DrawNativeProperties(bool drawHeader = false)
        {
            if (_nativeProperties.Any())
            {
                if (drawHeader)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Native Properties", GetHeaderGUIStyle());
                    NaughtyEditorGUI.HorizontalLine(
                        EditorGUILayout.GetControlRect(false), HorizontalLineAttribute.DefaultHeight,
                        HorizontalLineAttribute.DefaultColor.GetColor());
                }

                foreach (var property in _nativeProperties)
                {
                    NaughtyEditorGUI.NativeProperty_Layout(serializedObject.targetObject, property);
                }
            }
        }

        private static IEnumerable<SerializedProperty> GetProperties(IEnumerable<SerializedProperty> properties)
        {
            return properties;
        }

        private static IEnumerable<SerializedProperty> GetNonGroupedProperties(
            IEnumerable<SerializedProperty> properties)
        {
            return properties.Where(p => PropertyUtility.GetAttribute<IGroupAttribute>(p) == null);
        }

        //private static IEnumerable<IGrouping<string, SerializedProperty>> GetGroupedProperties(IEnumerable<SerializedProperty> properties)
        //{
        //    return properties
        //        .Where(p => PropertyUtility.GetAttribute<BoxGroupAttribute>(p) != null)
        //        .GroupBy(p => PropertyUtility.GetAttribute<BoxGroupAttribute>(p).Name);
        //}

        private static IEnumerable<IGrouping<string, SerializedProperty>> GetFoldoutProperties(
            IEnumerable<SerializedProperty> properties)
        {
            return properties
                .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) != null)
                .GroupBy(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p).Name);
        }

        private static GUIStyle GetHeaderGUIStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperCenter;

            return style;
        }
    }
}