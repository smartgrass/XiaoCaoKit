using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
        /// <summary>
        /// 反向获取key: 先取当前TMP的文本, 在Cn.ini中反查到key就填入;
        /// 查不到时用在线百度翻译得到译文, 生成新key写入Cn.ini和En.ini
        /// </summary>
        [Button("反向获取Key")]
        public void ReverseGetKey()
        {
            string text = LocalizeKeyReverser.GetPlainText(Text.text);
            if (!LocalizeKeyReverser.TryGetOrCreateKey(text, out string curKey))
            {
                return;
            }

            SetLocalize(curKey);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(Text);
        }
#endif

        void OnLanguageChanged(){
			ReFresh();
        }
		
		public  virtual void SetLocalize(string key){
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

		public static string ToLocalizeStr(this string key){
			return LocalizeMgr.Localize(key);
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
