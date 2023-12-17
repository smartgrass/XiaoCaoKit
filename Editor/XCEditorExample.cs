using NaughtyAttributes.Editor;
using OdinSerializer.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

public class XCEditorExample
{
    //布局相关
    void LayoutExample()
    {
        //行高
        float doubleLineHeight = EditorGUIUtility.singleLineHeight;
        //获取Rect
        Rect tweenRect = GUILayoutUtility.GetLastRect();
    }

    ///<see cref="NaughtyInspector.OnInspectorGUI"/>
    //绘制一个对象 Inspector
    void DrawExample(Object target)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        NaughtyEditorGUI.DoDrawDefaultInspector(serializedObject);

        //或者
        Editor editor = Editor.CreateEditor(target);
        editor.OnInspectorGUI();
    }


    //绘制单个字段
    private static void DrawPropertyExample(SerializedObject serializedObject)
    {
        //绘制单个字段
        SerializedProperty property = serializedObject.FindProperty("name");
        EditorGUILayout.PropertyField(property, true);
        // 或者
        NaughtyEditorGUI.PropertyField_Layout(property, true);

        //或者
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.y += EditorGUIUtility.singleLineHeight;
        rect.height = EditorGUI.GetPropertyHeight(property, true);
        NaughtyEditorGUI.PropertyField(rect, property, true);
    }


    //简单的编辑器窗口
    public XiaoCaoEexample_1 win;

}
