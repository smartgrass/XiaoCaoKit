using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

public class BasicSettingPanel: SubPanel
{
	public override void Init(){

		//Language
		AddLanguageDropDown();
	}

	private void AddLanguageDropDown(){
		List<string> enumList = new List<string>();
		foreach (ELanguage item in Enum.GetValues(typeof(ELanguage))){
			enumList.Add(LangSetting.ShowNames[(int)item]);
		}
        var langDropDown = AddDropdown(LocalizeKey.Language, OnLangChange, enumList);
        langDropDown.SetValueWithoutNotify((int)LocalizeMgr.CurLanguage);
	}

	private void OnLangChange(int index){
		LocalizeMgr.ChangeCurLang((ELanguage)index);
	}
}