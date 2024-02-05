using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class EditorObjects : ScriptableObject
{
    [Label("收藏夾")]
    public List<Object> ObjectList;
    [Label("So")]
    [Expandable]
    public ScriptableObject So;
}
