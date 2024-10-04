using System.Collections.Generic;
using System.IO;
using XiaoCao;

///<array版的 cref="IniFile"/>

public class InitArrayFile
{
    public Dictionary<string, List<string>> Dic;


    public bool IsHas(string key)
    {
        return Dic.ContainsKey(key);
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
            Dic = LoadFromStream(fs);
        }
    }


    public Dictionary<string, List<string>> LoadFromStream(FileStream fs)
    {
        var sections = new Dictionary<string, List<string>>();
        var currentSectionName = string.Empty;
        var currentSectionLines = new List<string>();

        using (StreamReader sr = new StreamReader(fs))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var trimmedLine = line.Trim();

                // Ignore empty lines and comments  
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // New section found  
                    currentSectionName = trimmedLine.TrimStart('[').TrimEnd(']');
                    if (sections.ContainsKey(currentSectionName))
                    {
                        // Clear previous content if section name is repeated  
                        // (or you can choose to append to the existing section)  
                        sections[currentSectionName].Clear();
                    }
                    else
                    {
                        // Add new section to dictionary  
                        sections[currentSectionName] = new List<string>();
                    }
                    currentSectionLines = sections[currentSectionName];
                }
                else if (!string.IsNullOrEmpty(currentSectionName))
                {
                    // Add line to current section  
                    currentSectionLines.Add(trimmedLine);
                }
                // Ignore lines that are not part of any section and are not comments  
            }
        }

        return sections;
    }
}