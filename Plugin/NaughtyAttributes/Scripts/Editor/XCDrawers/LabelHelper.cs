using UnityEngine;
using UnityEditor;
using XiaoCao;
using System.Reflection;
using System.Xml.Linq;

namespace NaughtyAttributes.Editor
{
    public class LabelHelper       
    {
        public static string GetShowText(SerializedProperty property,LabelAttribute att ){
            
            if (att.getValue != "")
            {
                object value = GetValues(property, att.getValue);
                return $"{att.label}({value})";
            }
            else{
                return att.label;
            }
        }
        
        private static object GetValues(SerializedProperty property, string valuesName)
        {
            object target = PropertyUtility.GetTargetObjectWithProperty(property);

            FieldInfo valuesFieldInfo = ReflectionUtility.GetField(target, valuesName);
            if (valuesFieldInfo != null)
            {
                return valuesFieldInfo.GetValue(target);
            }

            PropertyInfo valuesPropertyInfo = ReflectionUtility.GetProperty(target, valuesName);
            if (valuesPropertyInfo != null)
            {
                return valuesPropertyInfo.GetValue(target);
            }

            MethodInfo methodValuesInfo = ReflectionUtility.GetMethod(target, valuesName);
            if (methodValuesInfo != null &&
                methodValuesInfo.ReturnType != typeof(void) &&
                methodValuesInfo.GetParameters().Length == 0)
            {
                return methodValuesInfo.Invoke(target, null);
            }
            return null;
        }
    }
}