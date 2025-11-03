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
        private Dictionary<string, IXCDrawAttribute> _layoutDic;
        private List<int> methPosSortList;

        private Dictionary<string, SavedBool> _foldouts = new Dictionary<string, SavedBool>();
        private Dictionary<Object, SerializedObject> SoDic = new Dictionary<Object, SerializedObject>();

        protected virtual void OnEnable()
        {
            _nonSerializedFields = ReflectionUtility.GetAllFields(
                target, f => f.GetCustomAttributes(typeof(ShowNonSerializedFieldAttribute), true).Length > 0);

            _nativeProperties = ReflectionUtility.GetAllProperties(
                target, p => p.GetCustomAttributes(typeof(ShowNativePropertyAttribute), true).Length > 0);

            CacheLayoutDic();

            GetSortMethods();
        }

        private void CacheLayoutDic()
        {
            var fields = ReflectionUtility.GetAllFields(
                target, f => f.GetCustomAttributes(typeof(IXCDrawAttribute), true).Length > 0);
            foreach (var field in fields)
            {
                //layoutDic.Add(field.GetValue )
            }
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
            int i = 1;
            // Draw non-grouped serialized properties
            foreach (var property in GetProperties(_serializedProperties))
            {
                if (DrawScriptHander(property))
                {
                    continue;
                }

                IXCDrawAttribute[] xcLayouts = PropertyUtility.GetAttributes<IXCDrawAttribute>(property);
                if (xcLayouts.Length == 0)
                {
                    NaughtyEditorGUI.PropertyField_Layout(property, true);
                }
                else
                {
                    int length = xcLayouts.Length;
                    bool hasHor = false;
                    for (int j = 0; j < length; j++)
                    {
                        var xcLayout = xcLayouts[j];
                        if (length == 1)
                        {
                            xcLayout.OnDraw(serializedObject.targetObject,
                                () => { NaughtyEditorGUI.PropertyField_Layout(property, true); });
                            continue;
                        }


                        bool isLast = j == length - 1;
                        bool isFrist = j == 0;
                        if (isFrist)
                        {
                            xcLayout.OnDraw(serializedObject.targetObject, () =>
                            {
                                GUILayout.BeginHorizontal();
                                hasHor = true;
                                NaughtyEditorGUI.PropertyField_Layout(property, true);
                            });
                        }

                        if (hasHor && xcLayout.GetType() == typeof(XCHeaderAttribute))
                        {
                            GUILayout.EndHorizontal();
                            hasHor = false;
                        }

                        if (isLast)
                        {
                            xcLayout.OnDraw(serializedObject.targetObject, () =>
                            {
                                if (hasHor)
                                {
                                    GUILayout.EndHorizontal();
                                }
                            });
                        }


                        if (!isFrist && !isLast)
                        {
                            xcLayout.OnDraw(serializedObject.targetObject, null);
                        }
                    }
                }


                //按钮
                if (_methodDic.ContainsKey(-i))
                {
                    NaughtyEditorGUI.ButtonList(serializedObject.targetObject, _methodDic[-i]);
                }

                i++;
            }
            ///<see cref="FoldoutAttribute"/>
            //Obsolete BoxGroupAttribute 
            // Draw grouped serialized properties

            /*
            foreach (var group in GetGroupedProperties(_serializedProperties))
            {
                IEnumerable<SerializedProperty> visibleProperties = group.Where(p => PropertyUtility.IsVisible(p));
                if (!visibleProperties.Any())
                {
                    continue;
                }

                NaughtyEditorGUI.BeginBoxGroup_Layout(group.Key);
                foreach (var property in visibleProperties)
                {
                    NaughtyEditorGUI.PropertyField_Layout(property, true);
                }

                NaughtyEditorGUI.EndBoxGroup_Layout();
            }
            */

            // Draw foldout serialized properties
            foreach (var group in GetFoldoutProperties(_serializedProperties))
            {
                IEnumerable<SerializedProperty> visibleProperties = group.Where(p => PropertyUtility.IsVisible(p));
                if (!visibleProperties.Any())
                {
                    continue;
                }

                if (!_foldouts.ContainsKey(group.Key))
                {
                    _foldouts[group.Key] = new SavedBool($"{target.GetInstanceID()}.{group.Key}", false);
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


            serializedObject.ApplyModifiedProperties();
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