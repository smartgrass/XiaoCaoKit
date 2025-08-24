using System.Collections.Generic;
using UnityEngine;
using System;
public class DialogueEventManager : MonoBehaviour,IMgr
{
    public static DialogueEventManager Instance;

    private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    public void RegisterEvent(string eventName, Action action)
    {
        if (eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName] += action;
        else
            eventDictionary.Add(eventName, action);
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public void UnregisterEvent(string eventName, Action action)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] -= action;
            if (eventDictionary[eventName] == null)
                eventDictionary.Remove(eventName);
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void TriggerEvent(string eventName)
    {
        if (eventDictionary.TryGetValue(eventName, out Action action))
            action?.Invoke();
        else
            Debug.LogWarning($"未注册的对话事件：{eventName}");
    }
}
