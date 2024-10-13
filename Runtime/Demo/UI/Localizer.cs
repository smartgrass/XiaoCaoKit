using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;

namespace XiaoCao.UI{
	public class Localizer : MonoBehaviour{
		public TextMeshProUGUI Text => GetComponent<TextMeshProUGUI>();

		public string key;

		private bool _hasLocalized = false;
		
		private void Awake(){
			LocalizeMgr.Inst.OnLanguageChanged += OnLanguageChanged;
		}

		void Start()
		{
			if (!_hasLocalized)
			{
                ReFresh();
            }
        }

        [Button]
        public void ReFresh()
        {
            SetLocalize(key);
        }

        void OnLanguageChanged(){
			ReFresh();
        }
		
		public void SetLocalize(string key){
			this.key = key;
			Text.text = new LocalizeStr(key);
			_hasLocalized = true;

        }

		
		private void OnDestroy(){
			LocalizeMgr.Inst.OnLanguageChanged -= OnLanguageChanged;
		}
	}

	public static class LocalizerExtend
	{
		public static void BindLocalizer(this TextMeshProUGUI tmp, string key)
		{
            tmp.gameObject.GetOrAddComponent<Localizer>().SetLocalize(key);
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