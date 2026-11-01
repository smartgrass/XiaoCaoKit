using NaughtyAttributes;
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

        /// <summary>true 打开一个水平行, false 关闭</summary>
        public XCDrawStage Stage => IsBegin ? XCDrawStage.BeginRow : XCDrawStage.EndRow;



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
            // 带 action 时自己开一行(单独使用); Inspector 统一调度时传 null, 行已由外层打开, 这里只画按钮
            bool ownRow = action != null;
            if (ownRow)
            {
                GUILayout.BeginHorizontal();
                action?.Invoke();
            }

            var getMethod = ReflectionUtil.GetMethod(targetObject, funName);
            if (getMethod != null)
            {
                if (IsButtonDown())
                {
                    getMethod.Invoke(targetObject, null);
                }
            }

            if (ownRow)
            {
                GUILayout.EndHorizontal();
            }
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

        /// <summary>标题必须独占整行, 在水平行打开之前绘制</summary>
        public XCDrawStage Stage => XCDrawStage.Before;

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


