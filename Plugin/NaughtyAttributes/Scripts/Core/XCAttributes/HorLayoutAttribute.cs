﻿using NaughtyAttributes;
using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
#endif

namespace XiaoCao
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HorLayoutAttribute : MetaAttribute, IXCDrawAttribute
    {
        public bool IsBegin { get; set; }

        public HorLayoutAttribute(bool isBegin)
        {
            IsBegin = isBegin;
        }



        public void OnDraw(UnityEngine.Object targetObject, Action action)
        {
#if UNITY_EDITOR
            if (IsBegin)
            {
                GUILayout.BeginHorizontal();
                action?.Invoke();
            }
            if (!IsBegin)
            {
                action?.Invoke();
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
#endif
        }
    }


    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]

    public class MiniBtnAttribute : MetaAttribute, IXCDrawAttribute
    {
        public string showName;
        public string funName;
        public int width;

        public MiniBtnAttribute(string funName, string showName = "", int width = -1)
        {
            this.showName = showName;
            this.funName = funName;
            this.width = width;
            if (showName == "")
            {
                this.showName = funName;
            }
        }

        public void OnDraw(Object targetObject, Action action)
        {
            GUILayout.BeginHorizontal();
            action?.Invoke();
            var getMethod = ReflectionUtil.GetMethod(targetObject, funName);
            if (getMethod != null)
            {
                if (IsButtonDown())
                {
                    getMethod.Invoke(targetObject, null);
                }
            }
            GUILayout.EndHorizontal();
        }

        bool IsButtonDown()
        {
            if (width < 0)
            {
                if (GUILayout.Button(showName))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (GUILayout.Button(showName, GUILayout.Width(width)))
                {
                    return true;
                }
                return false;
            }
        }
    }


    /// <summary>
    /// XCEditor中代替Header
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]

    public class XCHeaderAttribute : MetaAttribute, IXCDrawAttribute
    {
        public XCHeaderAttribute(string header = "") { this.Header = header; }

        public string Header;

        public void OnDraw(Object targetObject, Action action)
        {
            if (!string.IsNullOrEmpty(Header))
            {
                GUILayout.Label(Header);
            }
            action?.Invoke();
        }
    }
}


