using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Serialization;

public class EditorObjects : ScriptableObject
{
    [SerializeField]
    public List<AssetsUsing> assetsUsing;
    
    [Label("")]
    [Expandable]
    public ScriptableObject So;
}
