using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;
/// <summary>
/// 收藏夹 窗口
/// </summary>
public class ObjectsWindow : XiaoCaoWindow
{
    [Expandable()]
    public EditorObjects objects;

    public override Object DrawTarget => objects;

    private static ObjectsWindow instance;
    public static ObjectsWindow Instance
    {
        get
        {
            if (instance == null)
                Open();
            return instance;
        }
    }

    [MenuItem("XiaoCao/对象收藏夹")]
    static void Open()
    {
        instance = OpenWindow<ObjectsWindow>("对象收藏夹");
    }


    public void OnEnable()
    {
        string path = "Assets/Ignore/Editor/XCObjectUsing.asset";
        objects = XCAseetTool.GetOrNewSO<EditorObjects>(path);
    }
}


public static class XiaoCaoObjectUsingExtend
{
    [MenuItem("Assets/XiaoCao/添加到收藏", priority = 100)]
    private static void AddToFavor()
    {
        var objs = Selection.objects;
        var win = ObjectsWindow.Instance;
        if (win.objects.ObjectList == null)
        {
            win.objects.ObjectList = new List<Object>();
        }

        foreach (var item in objs)
        {
            win.objects.ObjectList.Add(item);
            Debug.Log($"yns add {item.name}");
        }
    }
}


public class EditorObjects : ScriptableObject
{
    [NaughtyAttributes.Label("收藏夾")]
    public List<Object> ObjectList;
}

