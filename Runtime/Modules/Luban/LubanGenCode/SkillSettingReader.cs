
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
public partial class SkillSettingReader
{
    private readonly System.Collections.Generic.Dictionary<string, SkillSetting> _dataMap;
    private readonly System.Collections.Generic.List<SkillSetting> _dataList;
    
    public SkillSettingReader(ByteBuf _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<string, SkillSetting>();
        _dataList = new System.Collections.Generic.List<SkillSetting>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            SkillSetting _v;
            _v = SkillSetting.DeserializeSkillSetting(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.Id, _v);
        }
    }

    public System.Collections.Generic.Dictionary<string, SkillSetting> DataMap => _dataMap;
    public System.Collections.Generic.List<SkillSetting> DataList => _dataList;

    public SkillSetting GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public SkillSetting Get(string key) => _dataMap[key];
    public SkillSetting this[string key] => _dataMap[key];

    public void ResolveRef(Tables tables)
    {
        foreach(var _v in _dataList)
        {
            _v.ResolveRef(tables);
        }
    }

}

}

