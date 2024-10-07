using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AttributeValue
{
    public float BaseValue;
    public float CurrentValue;
    public Dictionary<string, AttributeModifier> Modifiers = new Dictionary<string, AttributeModifier>();

    public void UpdateAttributeModifiers()
    {
        float add = 0f;
        float multiply = 1;
        foreach (var item in Modifiers.Values)
        {
            add += item.Add;
            multiply += item.Multiply;
        }
        CurrentValue = (BaseValue + add) * multiply;
    }


    public void AddModifier(string key, AttributeModifier modifier)
    {
        Modifiers[key] = modifier;
        UpdateAttributeModifiers();
    }

    public void RemoveModifier(string key)
    {
        if (Modifiers.ContainsKey(key))
        {
            Modifiers.Remove(key);
            UpdateAttributeModifiers();
        }
    }
}

[Serializable]
public struct AttributeModifier
{
    public float Add;
    public float Multiply;
}
