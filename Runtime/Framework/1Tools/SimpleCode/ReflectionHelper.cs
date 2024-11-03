
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XiaoCao
{
    /// <summary>
    /// 反射相关
    /// </summary>
    public static class ReflectionHelper
    {

        public static List<FieldInfo> GetFields(Type type)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            // 如果type继承自 MonoBehaviour,那么递归到此为止
            while (type != null && type != typeof(MonoBehaviour) && type != typeof(object))
            {
                fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            return fields;
        }

        internal static MethodInfo GetInstanceMethod(Type declaringType, string name)
            => declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static MethodInfo GetStaticMethod(Type declaringType, string name)
            => declaringType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal static MethodInfo GetInstanceMethod(Type declaringType, string name, Type[] types)
        {
            if (types is null) types = Type.EmptyTypes;
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, types, null);
        }

        internal static bool IsSubclassOf(Type type, Type baseClass)
            => type.IsSubclassOf(baseClass);

        public static ProtoTypeCode GetTypeCode(Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Empty:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    return (ProtoTypeCode)code;
            }
            if (type == typeof(TimeSpan)) return ProtoTypeCode.TimeSpan;
            if (type == typeof(Guid)) return ProtoTypeCode.Guid;
            if (type == typeof(Uri)) return ProtoTypeCode.Uri;
            if (type == typeof(byte[])) return ProtoTypeCode.ByteArray;
            if (type == typeof(ArraySegment<byte>)) return ProtoTypeCode.ByteArraySegment;
            //if (type == typeof(Memory<byte>)) return ProtoTypeCode.ByteMemory;
            //if (type == typeof(ReadOnlyMemory<byte>)) return ProtoTypeCode.ByteReadOnlyMemory;
            if (type == typeof(Type)) return ProtoTypeCode.Type;
            if (type == typeof(IntPtr)) return ProtoTypeCode.IntPtr;
            if (type == typeof(UIntPtr)) return ProtoTypeCode.UIntPtr;

            return ProtoTypeCode.Unknown;
        }

        internal static MethodInfo GetGetMethod(PropertyInfo property, bool nonPublic, bool allowInternal)
        {
            if (property is null) return null;
            var method = property.GetGetMethod(nonPublic);
            if (method is null && !nonPublic && allowInternal)
            { // could be "internal" or "protected internal"; look for a non-public, then back-check
                method = property.GetGetMethod(true);
                if (method != null && !(method.IsAssembly || method.IsFamilyOrAssembly))
                {
                    method = null;
                }
            }
            return method;
        }

        internal static MethodInfo GetSetMethod(PropertyInfo property, bool nonPublic, bool allowInternal)
        {
            if (property is null) return null;

            var method = property.GetSetMethod(nonPublic);
            if (method is null && !nonPublic && allowInternal)
            { // could be "internal" or "protected internal"; look for a non-public, then back-check
                method = property.GetGetMethod(true);
                if (method != null && !(method.IsAssembly || method.IsFamilyOrAssembly))
                {
                    method = null;
                }
            }
            return method;
        }

        internal static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes, bool nonPublic)
        {
            return type.GetConstructor(
                nonPublic ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                          : BindingFlags.Instance | BindingFlags.Public,
                    null, parameterTypes, null);
        }
        internal static ConstructorInfo[] GetConstructors(Type type, bool nonPublic)
        {
            return type.GetConstructors(
                nonPublic ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                          : BindingFlags.Instance | BindingFlags.Public);
        }

        internal static void GetBuffer(MemoryStream stream, out ArraySegment<byte> segment)
        {
            if (stream is null || !stream.TryGetBuffer(out segment))
            {
                // ThrowHelper.ThrowInvalidOperationException("Unable to obtain buffer from MemoryStream");
                segment = default;
            }

        }

        internal static PropertyInfo GetProperty(Type type, string name, bool nonPublic)
        {
            return type.GetProperty(name,
                nonPublic ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                          : BindingFlags.Instance | BindingFlags.Public);
        }

        internal static MemberInfo[] GetInstanceFieldsAndProperties(Type type, bool publicOnly)
        {
            var flags = publicOnly ? BindingFlags.Public | BindingFlags.Instance : BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var props = type.GetProperties(flags);
            var fields = type.GetFields(flags);
            var members = new MemberInfo[fields.Length + props.Length];
            props.CopyTo(members, 0);
            fields.CopyTo(members, props.Length);
            return members;
        }





        // 尝试根据路径获取静态数据的值  

        public static object GetValueFromPath(string path)
        {
            try
            {
                string[] parts = path.Split('.');
                object current = null;
                Type currentType = null;
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    // 如果是第一部分，特殊处理以获取静态字段或属性  
                    if (i == 0)
                    {
                        // 获取类型（假设路径的第一部分是类的完全限定名或简单名）  
                        currentType = Type.GetType(part, false) ?? Type.GetType("XiaoCao." + part, false);

                        if (currentType == null)
                        {
                            Debug.LogError($"Type '{part}' not found.");
                            return null;
                        }


                        // 获取静态字段或属性  
                        FieldInfo fieldInfo = currentType.GetField(part, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        PropertyInfo propertyInfo = fieldInfo == null ? currentType.GetProperty(part, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;

                        if (fieldInfo != null)
                        {
                            current = fieldInfo.GetValue(null);
                        }
                        else if (propertyInfo != null)
                        {
                            current = propertyInfo.GetValue(null, null);
                        }
                        else
                        {
                            Debug.LogError($"Static field or property '{part}' not found in type '{currentType.FullName}'.");
                            return null;
                        }
                    }

                    else
                    {
                        // 对于后续部分，假设它们是非静态属性或字段  
                        if (current == null)
                        {
                            Debug.LogError($"Cannot access property or field '{part}' because the previous value is null.");
                            return null;
                        }



                        currentType = current.GetType();

                        var fieldInfo = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            current = fieldInfo.GetValue(current);
                        }
                        else
                        {
                            var propertyInfo = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (propertyInfo != null)
                            {
                                current = propertyInfo.GetValue(current, null);
                            }
                            else
                            {
                                Debug.LogError($"Field or property '{part}' not found in type '{currentType.FullName}'.");
                                return null;
                            }
                        }
                    }
                }
                return current;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error retrieving value from path: {path}\n{e}");
                return null;
            }
        }

    }
    /// <summary>
    /// Intended to be a direct map to regular TypeCode, but:
    /// - with missing types
    /// - existing on WinRT
    /// </summary>
    public enum ProtoTypeCode
    {
        Empty = 0,
        Unknown = 1, // maps to TypeCode.Object
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10,
        Int64 = 11,
        UInt64 = 12,
        Single = 13,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18,

        // additions
        TimeSpan = 100,
        ByteArray = 101,
        Guid = 102,
        Uri = 103,
        Type = 104,
        ByteArraySegment = 105,
        ByteMemory = 106,
        ByteReadOnlyMemory = 107,
        IntPtr = 108,
        UIntPtr = 109,
    }


}