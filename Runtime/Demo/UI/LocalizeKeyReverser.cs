#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using XiaoCao;

namespace XiaoCao.UI
{
    /// <summary>
    /// 本地化key反查工具: 按文本在Cn.ini中反查key, 查不到时用在线百度翻译生成新key并写入本地化配置
    /// </summary>
    public static class LocalizeKeyReverser
    {
        //百度翻译开放平台的文本翻译接口
        private const string BaiduTranslateApi = "https://fanyi-api.baidu.com/api/trans/vip/translate";

        private const string CnFile = "Localize/Cn.ini";
        private const string EnFile = "Localize/En.ini";
        //配置放在Tools目录, 避免打包时被复制进ExtraRes
        private const string BaiduSettingFile = "Tools/BaiduTranslate.ini";
        private const string BaiduSettingSection = "Translate";

        private const string NewLine = "\r\n";
        private const int WebTimeoutMs = 10000;

        //本地化配置都是UTF-8(无BOM), 保持一致
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        //百度翻译配置带BOM, 方便编辑器和查看工具识别中文
        private static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);

        /// <summary>
        /// 反查文本对应的key, 查不到时用在线百度翻译生成新key并写入Cn.ini和En.ini
        /// </summary>
        public static bool TryGetOrCreateKey(string text, out string key)
        {
            key = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("--- 反向获取key失败: 文本为空");
                return false;
            }

            IniFile cnIni = LoadIni(CnFile);
            if (cnIni.TryGetKeyByValue(text, out key))
            {
                Debug.Log($"--- 反向获取key命中配置: {key} = {text}");
                return true;
            }

            //没有配置的文本会直接显示成key, 这种情况key已经存在
            IniFile enIni = LoadIni(EnFile);
            if (cnIni.HasKey(text) || enIni.HasKey(text))
            {
                key = text;
                Debug.Log($"--- 文本本身就是已配置的key: {key}");
                return true;
            }

            if (!TryTranslateToEn(text, out string enText, out string error))
            {
                Debug.LogError($"--- 在线百度翻译失败: {error}");
                return false;
            }

            key = BuildKeyName(enText, cnIni, enIni);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"--- 生成key失败, 译文无法作为key: {enText}");
                return false;
            }

            AppendKeyValue(CnFile, key, text);
            AppendKeyValue(EnFile, key, enText);
            LocalizeMgr.ReloadLangData();
            Debug.Log($"--- 新增本地化配置 {key}\n{CnFile}: {text}\n{EnFile}: {enText}");
            return true;
        }

        /// <summary>
        /// 去掉TMP富文本标签, 得到实际显示的文本
        /// </summary>
        public static string GetPlainText(string tmpText)
        {
            if (string.IsNullOrEmpty(tmpText))
            {
                return string.Empty;
            }
            return Regex.Replace(tmpText, "<[^<>]+>", string.Empty).Trim();
        }

        /// <summary>
        /// 用英文译文生成不重复的key名
        /// </summary>
        private static string BuildKeyName(string enText, IniFile cnIni, IniFile enIni)
        {
            string baseKey = ToCamelKey(enText);
            if (string.IsNullOrEmpty(baseKey))
            {
                return string.Empty;
            }

            string key = baseKey;
            for (int index = 2; cnIni.HasKey(key) || enIni.HasKey(key); index++)
            {
                key = $"{baseKey}{index}";
            }
            return key;
        }

        /// <summary>
        /// 把译文转成驼峰风格的key名, 如 "character attributes" -> "CharacterAttributes"
        /// </summary>
        private static string ToCamelKey(string text)
        {
            StringBuilder builder = new StringBuilder();
            bool upperNext = true;
            foreach (char c in text)
            {
                //撇号属于单词内部, 直接去掉
                if (c == '\'' || c == '’')
                {
                    continue;
                }

                //只保留英文和数字, 译文没翻译成功(还是中文)时会直接判断为无法作为key
                if (!IsAsciiLetterOrDigit(c))
                {
                    //单词分隔符, 下一个字母大写
                    upperNext = true;
                    continue;
                }

                builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }
            return builder.ToString();
        }

        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        /// <summary>
        /// 调用在线百度翻译, 中文翻译成英文
        /// </summary>
        private static bool TryTranslateToEn(string text, out string result, out string error)
        {
            result = string.Empty;
            if (!TryLoadBaiduSetting(out string appId, out string appKey, out error))
            {
                return false;
            }

            string salt = DateTime.Now.Ticks.ToString();
            string sign = Md5(appId + text + salt + appKey);
            string body = $"q={Uri.EscapeDataString(text)}&from=zh&to=en&appid={appId}&salt={salt}&sign={sign}";

            string json;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaiduTranslateApi);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = WebTimeoutMs;
                byte[] bodyData = Encoding.UTF8.GetBytes(body);
                request.ContentLength = bodyData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyData, 0, bodyData.Length);
                }

                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            BaiduTransResponse transResponse = JsonUtility.FromJson<BaiduTransResponse>(json);
            if (transResponse == null || transResponse.trans_result == null || transResponse.trans_result.Length == 0)
            {
                error = string.IsNullOrEmpty(transResponse?.error_msg) ? json : $"{transResponse.error_code} {transResponse.error_msg}";
                return false;
            }

            result = transResponse.trans_result[0].dst;
            return true;
        }

        /// <summary>
        /// 读取百度翻译开放平台的appid和密钥, 缺少配置时生成模板文件
        /// </summary>
        private static bool TryLoadBaiduSetting(out string appId, out string appKey, out string error)
        {
            appId = string.Empty;
            appKey = string.Empty;
            error = string.Empty;

            string path = $"{PathTool.GetProjectPath()}/{BaiduSettingFile}";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string template = $"[{BaiduSettingSection}]{NewLine}"
                    + $"//百度翻译开放平台申请的appid和密钥, 见 https://fanyi-api.baidu.com/manage/developer{NewLine}"
                    + $"AppId={NewLine}"
                    + $"AppKey={NewLine}";
                File.WriteAllText(path, template, Utf8WithBom);
                error = $"缺少百度翻译配置, 已生成模板文件, 请填写appid和密钥: {path}";
                return false;
            }

            IniFile ini = new IniFile();
            ini.LoadFromFullPath(path);
            appId = ini.GetValue(BaiduSettingSection, "AppId", string.Empty);
            appKey = ini.GetValue(BaiduSettingSection, "AppKey", string.Empty);
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appKey))
            {
                error = $"百度翻译配置没有填写appid或密钥: {path}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 把key=value追加到配置文件末尾, 值中的换行转义成\n
        /// </summary>
        private static void AppendKeyValue(string fileName, string key, string value)
        {
            string path = XCPathConfig.GetGameConfigFile(fileName);
            string content = File.ReadAllText(path, Utf8NoBom);
            string prefix = content.EndsWith("\n") ? string.Empty : NewLine;
            string valueLine = value.Replace("\r\n", "\\n").Replace("\n", "\\n");
            File.WriteAllText(path, $"{content}{prefix}{key}={valueLine}{NewLine}", Utf8NoBom);
        }

        private static IniFile LoadIni(string fileName)
        {
            IniFile ini = new IniFile();
            ini.LoadFromFile(fileName);
            return ini;
        }

        /// <summary>
        /// 百度翻译接口要求的md5签名
        /// </summary>
        private static string Md5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// 百度翻译接口的返回数据
        /// </summary>
        [Serializable]
        private class BaiduTransResponse
        {
            public string error_code;
            public string error_msg;
            public BaiduTransItem[] trans_result;
        }

        [Serializable]
        private class BaiduTransItem
        {
            public string src;
            public string dst;
        }
    }
}
#endif
