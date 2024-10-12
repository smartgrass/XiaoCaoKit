using System;

namespace XiaoCao
{
	public class LocalizeMgr:Singleton<LocalizeMgr>,IClearCache
	{
		private static IniFile _localizeData;
		private const string DirName = "Localize/";
		private const string CurLangKey = "CurLang";
		public static ELanguage CurLanguage;
		public static Action OnLanguageChanged = null;

		protected override void Init(){
			base.Init();
			Load();
		}

		public static string Localize(string key){
			if (_localizeData.TryGetFristValue(key,out string value)){
				return value;	
			}
			return CurLangKey+key;
		}

		public static void Load()
		{
			var langStr= CurLangKey.GetKeyString();
			Enum.TryParse(langStr, out ELanguage lang);
			CurLanguage = lang;
            
			LoadLangData(lang);
		}

		private static void LoadLangData(ELanguage lang){
			var ini = new IniFile();
			ini.LoadFromFile(DirName + lang.ToString()+".ini");
			_localizeData = ini;
		}

		public static void ChangeCurLang(ELanguage lang){
			if (CurLanguage == lang) return;
			
			CurLangKey.SetKeyString(lang.ToString());
			LoadLangData(lang);
			CurLanguage = lang;
			OnLanguageChanged?.Invoke();
		}
		
		
	}

	public class LangSetting{
		//与枚举1,1对应
		public static string[] ShowNames =  { "English", "中文" };
	}
	

	public enum ELanguage{
		En,
		Cn
	}
}