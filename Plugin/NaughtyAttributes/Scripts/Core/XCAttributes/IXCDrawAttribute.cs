
using System;
using UnityEditor;

namespace XiaoCao
{
    /// <summary>
    ///  AllowMultiple = false
    /// </summary>
    public interface IXCDrawAttribute
    {
        //是否需要开头结尾对称
        bool IsStartAndEnd { get => false; }
        void OnDraw(UnityEngine.Object targetObject, Action action);
    }
}
