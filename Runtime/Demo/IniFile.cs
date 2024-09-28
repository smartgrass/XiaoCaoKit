using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XiaoCao;

public class IniFile
{
    private List<IniSection> m_sectionList;

    public List<IniSection> SectionList => m_sectionList;

    public IniFile()
    {
        m_sectionList = new List<IniSection>();
    }
    public void LoadFromFile(string fileName)
    {
        string strFullPath = XCPathConfig.GetGameConfigFile(fileName);
        if (!File.Exists(strFullPath))
        {
            return;
        }
        using (FileStream fs = new FileStream(strFullPath, FileMode.Open))
        {
            LoadFromStream(fs);
        }
    }
    /// <summary>
    /// 取得配置文件中所有的头名称
    /// </summary>
    /// <returns></returns>
    public List<string> GetAllSectionName()
    {
        List<string> sectionList = new List<string>();
        foreach (var sec in m_sectionList)
        {
            sectionList.Add(sec.SectionName.ToLower());
        }
        return sectionList;
    }
    /// <summary>
    /// 取得头部相关的value
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public string GetValue(string sectionName, string key, string defaultValue)
    {
        IniSection section = GetSection(sectionName);
        if (section != null)
        {
            return section.GetValue(key, defaultValue);
        }
        return defaultValue;
    }    
    public bool TryGetValue(string sectionName, string key, out string ret)
    {
        IniSection section = GetSection(sectionName);
        if (section != null)
        {
            if (section.Dic.TryGetValue(key,out string value))
            {
                ret = value;
                return true;
            }
        }
        ret = "";
        return false;
    }
    private void LoadFromStream(FileStream fs)
    {
        using (StreamReader sr = new StreamReader(fs))
        {
            m_sectionList.Clear();
            string line = null;
            IniSection section = null;
            int equalSignPos = 0;//=号的标记的位置
            string key, value;
            while (true)
            {
                line = sr.ReadLine();
                if (null == line)
                {
                    break;
                }
                line = line.Trim();
                if (line == "")
                {
                    continue;
                }
                //跳过注释
                if (line.Length >= 2 && line[0] == '/' && line[1] == '/')
                {
                    continue;
                }
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    //移除首尾的'[]'
                    line = line.Remove(0, 1);
                    line = line.Remove(line.Length - 1, 1);
                    //去SectionList缓存中找是否存在这个Section
                    section = GetSection(line);
                    //如果没有找到就直接new一个
                    if (null == section)
                    {
                        section = new IniSection(line);
                        m_sectionList.Add(section);
                    }
                }
                else
                {
                    //就是在这个头下面的数据字段，key-value格式
                    equalSignPos = line.IndexOf('=');
                    if (equalSignPos != 0)
                    {
                        key = line.Substring(0, equalSignPos);
                        value = line.Substring(equalSignPos + 1, line.Length - equalSignPos - 1);
                        section.AddKeyValue(key, value);
                    }
                    else
                    {
                        Debug.LogWarning("value为空");
                    }
                }
            }
        }
    }
    /// <summary>
    /// 从缓存中找Section
    /// </summary>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public IniSection GetSection(string sectionName)
    {
        foreach (var section in m_sectionList)
        {
            if (section.SectionName.ToLower() == sectionName.ToLower())
            {
                return section;
            }
        }
        return null;
    }
}



/// <summary>
/// ini头部+数据
/// </summary>
public class IniSection
{
    private string sectionName;
    private Dictionary<string, string> m_dicKeyValue;

    public Dictionary<string, string> Dic => m_dicKeyValue;

    public string SectionName
    {
        get { return this.sectionName; }
        set { this.sectionName = value; }
    }

    public IniSection(string name)
    {
        this.sectionName = name;
        this.m_dicKeyValue = new Dictionary<string, string>();
    }
    /// <summary>
    /// 添加key-value的值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddKeyValue(string _key, string _value)
    {
        string value = null;
        if (m_dicKeyValue.TryGetValue(_key, out value))
        {
            if (value != null)
            {
                m_dicKeyValue[_key] = _value;
            }
        }
        else
        {
            m_dicKeyValue.Add(_key, _value);
        }
    }
    /// <summary>
    /// 根据key取得value，如果没有取到就返回默认的值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public string GetValue(string key, string defaultValue)
    {
        string value = null;
        m_dicKeyValue.TryGetValue(key, out value);
        if (m_dicKeyValue.TryGetValue(key, out value))
        {
            return value;
        }
        return defaultValue;
    }
}