using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using XiaoCao;

public class IniFile
{
    private List<IniSection> m_sectionList = new();

    public List<IniSection> SectionList => m_sectionList;


    public void LoadFromFile(string fileName, string failBack = null)
    {
        string strFullPath = XCPathConfig.GetGameConfigFile(fileName);
        //string fileContent = FileTool.WWWAllTextsSync(strFullPath);
        //LoadFromText(fileContent);


        if (!File.Exists(strFullPath))
        {
            if (failBack != null)
            {
                Debug.LogError($"--- no file {strFullPath}");
                LoadFromFile(failBack, null);
            }
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

    public bool TryGetFristValue(string key, out string ret)
    {
        IniSection section = m_sectionList[0];
        if (section.Dic.TryGetValue(key, out string value))
        {
            ret = value;
            return true;
        }
        ret = null;
        return false;
    }

    public bool TryGetValue(string sectionName, string key, out string ret)
    {
        IniSection section = GetSection(sectionName);
        if (section != null)
        {
            if (section.Dic.TryGetValue(key, out string value))
            {
                ret = value;
                return true;
            }
        }
        ret = "";
        return false;
    }

    public void LoadFromText(string content)
    {
        //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        //using (FileStream ms = new FileStream(bytes))
        //{
        //    LoadFromStream(ms);
        //}
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
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == "") continue;

                // 跳过注释
                if (line.StartsWith("//")) continue;


                // 处理 Section
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string sectionName = line.Substring(1, line.Length - 2);
                    section = GetSection(sectionName) ?? new IniSection(sectionName);
                    if (!m_sectionList.Contains(section))
                    {
                        m_sectionList.Add(section);
                    }
                }
                else
                {
                    equalSignPos = line.IndexOf('=');
                    if (equalSignPos > 0)
                    {
                        key = line.Substring(0, equalSignPos).Trim();
                        value = line.Substring(equalSignPos + 1).Trim();

                        // 处理换行符（\n）
                        value = value.Replace("\\n", "\n");
                        section?.AddKeyValue(key, value);
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
