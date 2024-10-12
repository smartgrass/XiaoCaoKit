using System;

namespace XiaoCao
{
	public class LocalizeMgr:Singleton<LocalizeMgr>,IClearCache
	{
		public static IniFile LocalizeData;
		private const string DirName = "Localize/";
		private const string CurLangKey = "CurLang";
		public static Language Language;

		protected override void Init(){
			base.Init();
			Load();
		}

		public static string Localize(string key){
			if (LocalizeData.TryGetFristValue(key,out string value)){
				return value;	
			}
			return CurLangKey+key;
		}

		public static void Load()
		{
			var langStr= CurLangKey.GetKeyString();
			Enum.TryParse(langStr, out Language lang);
			Language = lang;
            
			LoadLangData(lang);
		}

		private static void LoadLangData(Language lang){
			var ini = new IniFile();
			ini.LoadFromFile(DirName + lang.ToString()+".ini");
			LocalizeData = ini;
		}

		public static void ChangeCurLang(Language lang){
			if (Language == lang) return;
			
			CurLangKey.SetKeyString(lang.ToString());
			LoadLangData(lang);
			Language = lang;
		}



        
		public void TestMethod1()
		{

		}
		
		
	}

	//[TestClass]
	//public class UnitTest1
	//{
	//    private const string Expected = "Hello World!";
	//    [TestMethod]

	//}

	public enum Language{
		En,
		Cn
	}
}