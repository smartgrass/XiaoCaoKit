using System;
using System.Collections.Generic;
using UnityEngine.UI;
using XiaoCao;

public class BaseSettingPanel: SubPanel
{
	public override void Init(){
		Hide();

		//Language
		AddLanguageDropDown();
	}

	private void AddLanguageDropDown(){
		List<string> enumList = new List<string>();
		foreach (ELanguage item in Enum.GetValues(typeof(ELanguage))){
			enumList.Add(LangSetting.ShowNames[(int)item]);
		}
		Dropdown langDropDown = AddDropdown(LocalizeKey.Language, OnLangChange, enumList);
		langDropDown.SetValueWithoutNotify((int)LocalizeMgr.CurLanguage);
	}

	private void OnLangChange(int index){
		LocalizeMgr.ChangeCurLang((ELanguage)index);
	}
}