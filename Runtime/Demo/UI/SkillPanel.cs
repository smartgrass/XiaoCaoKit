using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao{
	
	public class SkillPanel:PanelBase{
		public GameObject cellPrefab;
		public GameObject linkPrefab;
		
		
		public SkillTreeSetting setting;
		


		public override void Init()
		{
			base.Init();
			setting =  Resources.Load<SkillTreeSetting>("SkillTreeSetting");


			foreach (var VARIABLE in data.){
				
			}
			
			IsInited = true;
		}

		public override void OnCloseBtnClick()
		{
			Hide();
		}

		public override void Hide()
		{
			gameObject.SetActive(false);
			IsShowing = false;
			UIMgr.Inst.HideView(panelType);
		}

		public override void Show()
		{
			if (!IsInited)
			{
				Init();
			}
			IsShowing = true;
			gameObject.SetActive(true);
		}
	}
}