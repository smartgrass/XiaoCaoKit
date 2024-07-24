using ET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NaughtyAttributes.Editor
{
    //示例代码 设置CustomEditor中需要绘制的类即可
    //[CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ComponentViewHelper.Draw(target);
        }
    }


    public static class ComponentViewHelper
    {
        private static readonly Dictionary<Type, ITypeDrawer> typeDrawers = new Dictionary<Type, ITypeDrawer>();
        private static readonly List<IOtherTypeDrawer> otherDrawers = new List<IOtherTypeDrawer>();
        private static readonly UnityObjectTypeDrawer objectDrawer = new UnityObjectTypeDrawer();
        private static readonly MethodInfo SetListMethod = typeof(ComponentViewHelper).GetMethod(nameof(SetList), (BindingFlags)~BindingFlags.Default);

        private static BaseTypes types = new BaseTypes();
        private static Type IDictionaryType = typeof(IDictionary);
        private static Type IListType = typeof(IList);

        private static int maxlayer = 20;
        private static int curLayer = 0;
        private static bool isDebug;

        private static Dictionary<string, EditorSavedBool> _foldouts = new Dictionary<string, EditorSavedBool>();

        static ComponentViewHelper()
        {
            Assembly assembly = typeof(ComponentViewHelper).Assembly;
            Type drawType = typeof(ITypeDrawer);
            foreach (Type type in assembly.GetTypes())
            {
                if (!type.IsDefined(typeof(TypeDrawerAttribute)))
                {
                    continue;
                }
                if (drawType.IsAssignableFrom(type))
                {
                    ITypeDrawer typeDrawer = (ITypeDrawer)Activator.CreateInstance(type);
                    typeDrawers.Add(typeDrawer.HandlesType(), typeDrawer);
                }
                else
                {
                    IOtherTypeDrawer otherTypeDrawer = (IOtherTypeDrawer)Activator.CreateInstance(type);
                    otherDrawers.Add(otherTypeDrawer);
                }
            }



            curLayer = 0;
        }

        public static void Draw(object entity, string searchStr = "")
        {
            if (entity == null)
                return;
            try
            {
                if (curLayer == 0)
                {
                    isDebug = EditorGUILayout.Toggle("===Editor View===", (bool)isDebug);
                    if (!isDebug)
                    {
                        return;
                    }
                }
                DrawShowProperty(entity);

                FieldInfo[] fields = entity.GetType()
        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                EditorGUILayout.BeginVertical();

                foreach (FieldInfo fieldInfo in fields)
                {
                    Type type = fieldInfo.FieldType;
                    string fieldName = fieldInfo.Name;
                    string curSearch = searchStr;

                    if (type.IsDefined(typeof(HideInInspector), false))
                    {
                        continue;
                    }

                    var searchState = GetSearchState(curSearch, fieldName);
                    if (searchState == SearchState.IsTarget)
                    {
                        curSearch = "";
                    }
                    else if (searchState == SearchState.NotTarget)
                    {
                        continue;
                    }


                    object value = fieldInfo.GetValue(entity);
                    bool isDrawed = false;
                    //值类型绘制
                    if (typeDrawers.ContainsKey(type))
                    {
                        try
                        {
                            value = typeDrawers[type].DrawAndGetNewValue(type, fieldName, value, null);
                            isDrawed = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                        fieldInfo.SetValue(entity, value);
                        continue;
                    }


                    bool isArray = type.IsArray;
                    //派生类绘制, 如枚举
                    foreach (var item in otherDrawers)
                    {
                        if (item.HandlesType(type))
                        {
                            try
                            {
                                value = item.DrawAndGetNewValue(type, fieldName, value, null);
                                isDrawed = true;
                            }
                            catch (Exception e)
                            {
                                Debug.LogErrorFormat("{0}: {1}", type, e);
                            }
                            fieldInfo.SetValue(entity, value);
                            break;
                        }
                    }

                    //可展开类型绘制
                    if (!isDrawed && curLayer < maxlayer)
                    {
                        //绘制Object类型
                        if (CheckAndDrawObject(type, value, fieldInfo, entity))
                        {
                            continue;
                        }

                        bool isCollection = IsICollectionType(type, out Type elementType);
                        //绘制数组类型
                        if (isCollection)
                        {
                            if (value == null)
                            {
                                GUILayout.Label($"{fieldName} null", EditorStyles.boldLabel);
                                continue;
                            }

                            if (value is ICollection listValue)
                            {
                                int count = listValue.Count;
                                if (IsFoldoutKey(type, fieldName, count))
                                {
                                    bool isObjectType = objectDrawer.IsObjectType(elementType);
                                    bool isDicType = IDictionaryType.IsAssignableFrom(type);
                                    curLayer++;
                                    EditorGUI.indentLevel++;
                                    bool isList = listValue is IList;
                                    (int, object) change = (-1, null);
                                    //展开数组类
                                    int i = 0;
                                    foreach (var sub in listValue)
                                    {
                                        if (isObjectType)
                                        {
                                            var newSub = DrawObject(elementType, sub, i.ToString());
                                            if (sub != newSub)
                                            {
                                                change = (i, newSub);
                                            }
                                        }
                                        else
                                        {
                                            if (typeDrawers.TryGetValue(elementType, out ITypeDrawer drawer))
                                            {
                                                var newSub = drawer.DrawAndGetNewValue(elementType, i.ToString(), sub, null);
                                                if (!sub.Equals(newSub))
                                                {
                                                    change = (i, newSub);
                                                }
                                            }
                                            else
                                            {
                                                Draw(sub, curSearch);
                                            }
                                        }
                                        i++;
                                    }

                                    //List<值类型> 暂时无法修改..未知原因
                                    if (isList && change.Item1 >= 0)
                                    {
                                        TrySetListValue(change.Item2, listValue, change.Item1);
                                    }


                                    EditorGUI.indentLevel--;
                                    curLayer--;
                                }
                                continue;
                            }
                        }

                        //绘制普通类
                        if (IsFoldoutKey(type, fieldName))
                        {
                            DrawChildren(value);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"component view error: {entity.GetType().FullName} {e}");
            }
        }
        /// <summary>
        /// 绘制用 [ShowNativeProperty] 标记属性
        /// </summary>
        private static void DrawShowProperty(object entity)
        {
            #if NaughtyAttributes
            IEnumerable<PropertyInfo> _nativeProperties = ReflectionUtility.GetAllProperties(entity, p => p.GetCustomAttributes(typeof(ShowNativePropertyAttribute), true).Length > 0);
            foreach (var property in _nativeProperties)
            {
                NaughtyEditorGUI.Field_Layout(property.GetValue(entity, null), property.Name);
            }
            #endif  
        }


        private static void TrySetListValue(object setValue, ICollection listValue, int i)
        {
            try
            {
                var listType = listValue.GetType();
                if (listType.IsArray)
                {
                    Array array = (Array)listValue;
                    array.SetValue(setValue, i);
                }
                else if (listType.IsGenericType)
                {
                    var elementTypes = listType.GetGenericArguments();
                    if (elementTypes.Length == 1)
                    {
                        Debug.Log($"--- elementTypes[0] {elementTypes[0]} {setValue} ");
                        SetListMethod.MakeGenericMethod(elementTypes[0]).Invoke(null, new object[] { listValue, setValue, i });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"--- TrySetListValue {e}");
            }

        }

        public static void SetList<T>(List<T> list, T value, int i) where T : class
        {
            list[i] = value;
        }

        private static bool CheckAndDrawObject(Type type, object value, FieldInfo fieldInfo, object entity)
        {
            string showName = fieldInfo.Name;
            bool isObject = objectDrawer.IsObjectType(type);
            if (isObject)
            {
                value = DrawObject(type, value, showName);
                fieldInfo.SetValue(entity, value);
            }
            return isObject;
        }

        private static object DrawObject(Type type, object value, string showName)
        {
            bool isNotNull = value != null;
            if (isNotNull)
            {
                type = value.GetType();
            }
            value = objectDrawer.DrawObject(type, showName, value);
            if (isNotNull && objectDrawer.IsDrawChildren(type))
            {
                if (IsFoldoutKey(type, showName))
                {
                    DrawChildren(value);
                }
            }
            return value;
        }
        //是否展开
        private static bool IsFoldoutKey(Type type, string fieldInfoName, int chirldCount = -1)
        {
            string key = $"{type.Name}.{fieldInfoName}.{curLayer}";
            string label = fieldInfoName;
            if (chirldCount >= 0)
            {
                label = $"{fieldInfoName} (len = {chirldCount})";
            }
            if (!_foldouts.ContainsKey(key))
            {
                _foldouts[key] = new EditorSavedBool(key, false);
            }
            _foldouts[key].Value = EditorGUILayout.Foldout(_foldouts[key].Value, label);

            return _foldouts[key].Value;
        }

        private static bool IsICollectionType(Type type, out Type elementType)
        {

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }
            else if (type.IsGenericType && IListType.IsAssignableFrom(type))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
            else if (type.IsGenericType && IDictionaryType.IsAssignableFrom(type))
            {
                elementType = type;//.GetGenericArguments()[1];
                return true;
            }
            else
            {
                elementType = type;
                return false;
            }
        }

        private static void DrawChildren(object value)
        {
            curLayer++;
            EditorGUI.indentLevel++;
            Draw(value);
            EditorGUI.indentLevel--;
            curLayer--;
        }
        private static bool IsValueType(Type type)
        {
            return type == types._string
                || type.IsSubclassOf(types.ValueType)
                || type.IsSubclassOf(types.Enum);
        }

        public static SearchState GetSearchState(string searchStr, string fieldName)
        {
            if (string.IsNullOrEmpty(searchStr))
            {
                return SearchState.Empty;
            }

            if (fieldName.ToLower().Contains(searchStr.ToLower()))
            {
                return SearchState.IsTarget;
            }

            return SearchState.NotTarget;
        }
        public enum SearchState
        {
            IsTarget,
            NotTarget,
            Empty
        }
    }

    internal class EditorSavedBool
    {
        private bool _value;
        private string _name;

        public bool Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;
                EditorPrefs.SetBool(_name, value);
            }
        }

        public EditorSavedBool(string name, bool value)
        {
            _name = name;
            _value = EditorPrefs.GetBool(name, value);
        }
    }
    public class BaseTypes
    {
        public Type _string = typeof(string);
        public Type ValueType = typeof(ValueType);
        public Type ScriptableObject = typeof(ScriptableObject);
        public Type Enum = typeof(Enum);
        public Type IDictionary = typeof(IDictionary);
        public Type IList = typeof(IList);
    }

    public class UnityObjectTypeDrawer
    {
        private Type objectType = typeof(UnityEngine.Object);
        public bool IsObjectType(Type type)
        {
            return type.IsSubclassOf(objectType) || type == objectType;
        }

        internal bool IsDrawChildren(Type type)
        {
            return type.IsSubclassOf(typeof(UnityEngine.Component));
        }

        public object DrawObject(Type memberType, string memberName, object value)
        {
            return EditorGUILayout.ObjectField(memberName, (UnityEngine.Object)value, memberType, true);
        }


    }
}