using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionUtil
{
    public static MethodInfo GetMethod(object target, string methodName)
    {
        return GetAllMethods(target, m => m.Name.Equals(methodName, StringComparison.InvariantCulture)).FirstOrDefault();
    }
    public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
    {
        if (target == null)
        {
            Debug.LogError("The target object is null. Check for missing scripts.");
            return null;
        }

        IEnumerable<MethodInfo> methodInfos = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(predicate);

        return methodInfos;
    }
}


