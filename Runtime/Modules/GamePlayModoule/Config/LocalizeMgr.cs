﻿using System;
using UnityEngine;

namespace XiaoCao
{
    ///<see 配置 cref="LocalizeKey"/>
    public class LocalizeMgr : Singleton<LocalizeMgr>, IClearCache
    {
        private IniFile _localizeData;
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

        public static void Load()
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
#endif

            if (_instance._localizeData.TryGetFristValue(key, out string value))
            {
                return value;
            }
            return $"\"{key}\"";
        }


        private static void LoadLangData(ELanguage lang)
        {
            var ini = new IniFile();
            //默认语言
            ini.LoadFromFile($"{DirName}{lang}.ini", $"{DirName}{DefaultLang}.ini");
            _instance._localizeData = ini;
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
    }



    public enum ELanguage
    {
        Cn,
        En,
    }
}