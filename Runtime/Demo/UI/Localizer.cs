using System;
using TMPro;
using UnityEngine;

namespace XiaoCao.UI{
	public class Localizer : MonoBehaviour{
		public TextMeshProUGUI Text => GetComponent<TextMeshProUGUI>();

		public string key;
		
		private void Awake(){
			LocalizeMgr.OnLanguageChanged += OnLanguageChanged;
		}

		void OnLanguageChanged(){
			SetLocalize(key);
		}
		
		public void SetLocalize(string key){
			this.key = key;
			Text.text = new LocalizeStr(key);
		}
		
		public void ReFresh(){
			SetLocalize(key);
		}
		
		private void OnDestroy(){
			LocalizeMgr.OnLanguageChanged -= OnLanguageChanged;
		}
	}
	

	public struct LocalizeStr{
		public LocalizeStr(string key){
			this.Key = key;
		}
		public string Key;
		public string Value => LocalizeMgr.Localize(Key);
		
		
		public static implicit operator string(LocalizeStr l){
			return l.Value;
		}
	}
	
}