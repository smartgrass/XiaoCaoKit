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
    public EditorObjects _objects;

    public override Object DrawTarget => _objects;

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
    public override void OnEnable()
    {
        string path = "Assets/Ignore/Editor/XCObjectUsing.asset";
        _objects = XCAseetTool.GetOrNewSO<EditorObjects>(path);
        base.OnEnable();
    }

    [MenuItem(XCEditorTools.ObjectsWindow)]
    static void Open()
    {
        instance = OpenWindow<ObjectsWindow>("对象收藏夹");
    }



}
