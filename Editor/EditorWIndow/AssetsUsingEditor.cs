#if UNITY_EDITOR
//using AssetEditor.Editor.Window;
using NaughtyAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
[CreateAssetMenu(menuName = "SO/AssetsUsing")]
public  class AssetsUsingEditor :ScriptableObject
{
    [Label("")]
    public List<ObjectList> d;  
}
[Serializable]
public class ObjectList
{
    public string name;
    public List<Object> list;
}
#endif