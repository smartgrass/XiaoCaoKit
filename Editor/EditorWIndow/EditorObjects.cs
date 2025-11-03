using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Serialization;
using XiaoCao;

public class EditorObjects : ScriptableObject
{
    [SerializeField] public List<AssetsUsing> assetsUsing;

    [Dropdown(nameof(GetAssetsUsingName))] [OnValueChanged(nameof(OnSelectChange))] [Label("")]
    public int selectIndex;

    [Label("")] [Expandable] public ScriptableObject So;


    private void OnSelectChange()
    {
        So = assetsUsing[selectIndex];
    }

    private DropdownList<int> GetAssetsUsingName()
    {
        var list = new DropdownList<int>();
        //写入序号和名字
        for (int i = 0; i < assetsUsing.Count; i++)
        {
            var asset = assetsUsing[i];
            string name = asset != null ? asset.name : "None";
            list.Add($"{i}: {name}", i);
        }

        return list;
    }
}