using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
/// <summary>
/// 标记需要清除静态对象[StaticCache]
/// </summary>
public class StaticCacheAttribute : PropertyAttribute
{

}

//获取所有类消耗比较大, 少用, 或缓存
public class StaticCacheClearer
{
    public static void ClearStaticCacheFields()
    {
        // 获取所有程序集中的类型  
        var types = AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(assembly => assembly.GetTypes())
                             .ToList();

        foreach (var type in types)
        {
            // 获取所有带有 [StaticCache] 属性的静态字段  
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .Where(field => Attribute.IsDefined(field, typeof(StaticCacheAttribute)))
                             .ToList();

            foreach (var field in fields)
            {
                // 将字段设置为 null  
                field.SetValue(null, null);
                Debug.Log($"Cleared field: {field.Name} in type: {type.FullName}");
            }
        }
    }
}
