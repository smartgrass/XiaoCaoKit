using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Serialization;

public class EditorObjects : ScriptableObject
{
    [Label("")]
    [Expandable]
    public ScriptableObject So;
}
