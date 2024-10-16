
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;


namespace cfg
{
public partial class SkillUpgradeSettingReader
{
    private readonly System.Collections.Generic.Dictionary<int, SkillUpgradeSetting> _dataMap;
    private readonly System.Collections.Generic.List<SkillUpgradeSetting> _dataList;
    
    public SkillUpgradeSettingReader(ByteBuf _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<int, SkillUpgradeSetting>();
        _dataList = new System.Collections.Generic.List<SkillUpgradeSetting>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            SkillUpgradeSetting _v;
            _v = SkillUpgradeSetting.DeserializeSkillUpgradeSetting(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.Id, _v);
        }
    }

    public System.Collections.Generic.Dictionary<int, SkillUpgradeSetting> DataMap => _dataMap;
    public System.Collections.Generic.List<SkillUpgradeSetting> DataList => _dataList;

    public SkillUpgradeSetting GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public SkillUpgradeSetting Get(int key) => _dataMap[key];
    public SkillUpgradeSetting this[int key] => _dataMap[key];

    public void ResolveRef(Tables tables)
    {
        foreach(var _v in _dataList)
        {
            _v.ResolveRef(tables);
        }
    }

}

}

