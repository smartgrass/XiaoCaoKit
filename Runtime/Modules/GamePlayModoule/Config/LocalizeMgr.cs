using System;
using UnityEngine;

namespace XiaoCao
{
    ///<see 配置 cref="LocalizeKey"/>
    public class LocalizeMgr : Singleton<LocalizeMgr>, IClearCache
    {
        public IniFile localizeData;
        public Action OnLanguageChanged = null;

        private const string DirName = "Localize/";
        private const string CurLangKey = "CurLang";
        private static ELanguage DefaultLang = ELanguage.Cn; //枚举顺序第一个
        public static ELanguage CurLanguage;

        protected override void Init()
        {
            base.Init();
            Load();
        }

        private static void Load()
        {
            var langStr = CurLangKey.GetKeyString();
            Enum.TryParse(langStr, out ELanguage lang);
            CurLanguage = lang;

            LoadLangData(lang);
        }

        public static string Localize(string key)
        {
#if UNITY_EDITOR
            var inst = LocalizeMgr.Inst;

            if (key.EndsWith('\r'))
            {
                Debug.LogError($"-- key {key} ends with \\r");
            }
            
#endif

            if (_instance.localizeData.TryGetFristValue(key, out string value))
            {
                return value;
            }
            else
            {
                if (!Application.isPlaying)
                {
                    //自动重读取
                    ClearCache();
                    if (LocalizeMgr.Inst.localizeData.TryGetFristValue(key, out value))
                    {
                        return value;
                    }
                }
            }
            return $"<color=#FFE880>{key}</color>";
            //return $"\"{key}\"";
        }


        private static void LoadLangData(ELanguage lang)
        {
            var ini = new IniFile();
            //默认语言
            ini.LoadFromFile($"{DirName}{lang}.ini", $"{DirName}{DefaultLang}.ini");
            Inst.localizeData = ini;
        }

        public static void ChangeCurLang(ELanguage lang)
        {
            if (CurLanguage == lang) return;

            CurLangKey.SetKeyString(lang.ToString());
            LoadLangData(lang);
            CurLanguage = lang;
            Debug.Log($"--- ChangeCurLang {lang}");
            _instance.OnLanguageChanged?.Invoke();
        }

        public bool HasKey(string key)
        {
            if (localizeData.TryGetFristValue(key, out string value))
            {
                return true;
            }
            return false;
        }
    }



    public enum ELanguage
    {
        Cn,
        En,
    }
}