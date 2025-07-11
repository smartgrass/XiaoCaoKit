
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
public partial class LevelSettingReader
{
    private readonly System.Collections.Generic.Dictionary<string, LevelSetting> _dataMap;
    private readonly System.Collections.Generic.List<LevelSetting> _dataList;
    
    public LevelSettingReader(ByteBuf _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<string, LevelSetting>();
        _dataList = new System.Collections.Generic.List<LevelSetting>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            LevelSetting _v;
            _v = LevelSetting.DeserializeLevelSetting(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.Id, _v);
        }
    }

    public System.Collections.Generic.Dictionary<string, LevelSetting> DataMap => _dataMap;
    public System.Collections.Generic.List<LevelSetting> DataList => _dataList;

    public LevelSetting GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public LevelSetting Get(string key) => _dataMap[key];
    public LevelSetting this[string key] => _dataMap[key];

    public void ResolveRef(Tables tables)
    {
        foreach(var _v in _dataList)
        {
            _v.ResolveRef(tables);
        }
    }

}

}

