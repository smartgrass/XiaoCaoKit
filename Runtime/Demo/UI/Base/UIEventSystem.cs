using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// UI事件管理基类
    /// 提供通用的事件监听和发送功能
    /// </summary>
    public class UIEventSystem
    {
        // 存储事件监听器的字典
        protected Dictionary<string, Delegate> eventListeners = new Dictionary<string, Delegate>();

        #region 事件监听注册接口

        /// <summary>
        /// 添加无参事件监听
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void AddEventListener(string eventType, Action handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = (Action)eventListeners[eventType] + handler;
            }
            else
            {
                eventListeners[eventType] = handler;
            }
        }

        /// <summary>
        /// 添加单参数事件监听
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void AddEventListener<T>(string eventType, Action<T> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = (Action<T>)eventListeners[eventType] + handler;
            }
            else
            {
                eventListeners[eventType] = handler;
            }
        }

        /// <summary>
        /// 添加双参数事件监听
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void AddEventListener<T1, T2>(string eventType, Action<T1, T2> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = (Action<T1, T2>)eventListeners[eventType] + handler;
            }
            else
            {
                eventListeners[eventType] = handler;
            }
        }

        /// <summary>
        /// 添加三参数事件监听
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void AddEventListener<T1, T2, T3>(string eventType, Action<T1, T2, T3> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = (Action<T1, T2, T3>)eventListeners[eventType] + handler;
            }
            else
            {
                eventListeners[eventType] = handler;
            }
        }

        #endregion

        #region 事件监听移除接口

        /// <summary>
        /// 移除无参事件监听
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void RemoveEventListener(string eventType, Action handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                var existingHandler = eventListeners[eventType] as Action;
                if (existingHandler != null)
                {
                    existingHandler = (Action)existingHandler - handler;
                    if (existingHandler != null)
                    {
                        eventListeners[eventType] = existingHandler;
                    }
                    else
                    {
                        eventListeners.Remove(eventType);
                    }
                }
                else
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 移除单参数事件监听
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void RemoveEventListener<T>(string eventType, Action<T> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                var existingHandler = eventListeners[eventType] as Action<T>;
                if (existingHandler != null)
                {
                    existingHandler = (Action<T>)existingHandler - handler;
                    if (existingHandler != null)
                    {
                        eventListeners[eventType] = existingHandler;
                    }
                    else
                    {
                        eventListeners.Remove(eventType);
                    }
                }
                else
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 移除双参数事件监听
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void RemoveEventListener<T1, T2>(string eventType, Action<T1, T2> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                var existingHandler = eventListeners[eventType] as Action<T1, T2>;
                if (existingHandler != null)
                {
                    existingHandler = (Action<T1, T2>)existingHandler - handler;
                    if (existingHandler != null)
                    {
                        eventListeners[eventType] = existingHandler;
                    }
                    else
                    {
                        eventListeners.Remove(eventType);
                    }
                }
                else
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 移除三参数事件监听
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">事件处理回调</param>
        public void RemoveEventListener<T1, T2, T3>(string eventType, Action<T1, T2, T3> handler)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                var existingHandler = eventListeners[eventType] as Action<T1, T2, T3>;
                if (existingHandler != null)
                {
                    existingHandler = (Action<T1, T2, T3>)existingHandler - handler;
                    if (existingHandler != null)
                    {
                        eventListeners[eventType] = existingHandler;
                    }
                    else
                    {
                        eventListeners.Remove(eventType);
                    }
                }
                else
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        #endregion

        #region 事件发送接口

        /// <summary>
        /// 发送无参事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        public void SendEvent(string eventType)
        {
            if (eventListeners.ContainsKey(eventType) && eventListeners[eventType] != null)
            {
                try
                {
                    ((Action)eventListeners[eventType])?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking event {eventType}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 发送单参数事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="arg">参数值</param>
        public void SendEvent<T>(string eventType, T arg)
        {
            if (eventListeners.ContainsKey(eventType) && eventListeners[eventType] != null)
            {
                try
                {
                    ((Action<T>)eventListeners[eventType])?.Invoke(arg);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking event {eventType}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 发送双参数事件
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="arg1">参数1值</param>
        /// <param name="arg2">参数2值</param>
        public void SendEvent<T1, T2>(string eventType, T1 arg1, T2 arg2)
        {
            if (eventListeners.ContainsKey(eventType) && eventListeners[eventType] != null)
            {
                try
                {
                    ((Action<T1, T2>)eventListeners[eventType])?.Invoke(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking event {eventType}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 发送三参数事件
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="arg1">参数1值</param>
        /// <param name="arg2">参数2值</param>
        /// <param name="arg3">参数3值</param>
        public void SendEvent<T1, T2, T3>(string eventType, T1 arg1, T2 arg2, T3 arg3)
        {
            if (eventListeners.ContainsKey(eventType) && eventListeners[eventType] != null)
            {
                try
                {
                    ((Action<T1, T2, T3>)eventListeners[eventType])?.Invoke(arg1, arg2, arg3);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking event {eventType}: {e.Message}");
                }
            }
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 清除所有事件监听
        /// </summary>
        public void ClearAllEvents()
        {
            eventListeners.Clear();
        }

        /// <summary>
        /// 清除指定类型的事件监听
        /// </summary>
        /// <param name="eventType">事件类型</param>
        public void ClearEvent(string eventType)
        {
            eventListeners.Remove(eventType);
        }

        #endregion
    }
}