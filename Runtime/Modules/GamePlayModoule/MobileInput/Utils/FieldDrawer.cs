#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MFPC.Utils
{
    public static class FieldDrawer
    {
        public static object DrawField(object value, System.Type type, string fieldName)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{fieldName}:", GUILayout.Width(150));
            if (type == typeof(int))
            {
                value = EditorGUILayout.IntField((int)value);
            }
            else if (type == typeof(float))
            {
                value = EditorGUILayout.FloatField((float)value);
            }
            else if (type == typeof(bool))
            {
                value = EditorGUILayout.Toggle((bool)value);
            }
            else if (type == typeof(string))
            {
                value = EditorGUILayout.TextField((string)value);
            }
            else if (type == typeof(AudioClip))
            {
                value = EditorGUILayout.ObjectField((AudioClip)value, typeof(AudioClip), false);
            }
            else if (type == typeof(Vector2))
            {
                value = EditorGUILayout.Vector2Field(string.Empty, (Vector2)value);
            }
            EditorGUILayout.EndHorizontal();
            
            return value;
        }
    }
}
#endif