using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace XiaoCao{

	//宝箱掉落
	public class DropItem{
		//实现,可随机,可配置
		public string getFromSetting = "";
		
		public  ItemType ItemType;
		public EEquipmentSub EquipmentSub;
		public EQuality itemQuality;
		public int level = 10;
		
		public Item GetItem(){
			if (!string.IsNullOrEmpty( getFromSetting)){
				
			}
			
			
			string id = $"{EquipmentSub.ToString()}_{level}";
			return new Item( ItemType.Equipment,id,1, itemQuality);
		}
	}
	
	

	public class SkillCell : MonoBehaviour{
		public TMP_Text levelText;
		public Image icon;
		public Image unlockMask;
		public int skillIndex;

		public void SetUnlock(bool isUnlock){
			unlockMask.enabled = isUnlock;
		}
	}

	public class SkillDisplay: MonoBehaviour{
		public TMP_Text nameText;
		public TMP_Text desText;
		public void Show(int skillIndex){
			gameObject.SetActive(true);
			//TODO 本地化
		}
	}
	
	public class SkillPanel:PanelBase{
		public GameObject cellPrefab;
		public GameObject linkPrefab;
		public SkillDisplay skillDisplay;
		public Transform tf;
		
		
		public SkillTreeSetting setting;
		
		private List<SkillCell> cells = new List<SkillCell>();
		

		public override void Init()
		{
			base.Init();
			setting =  Resources.Load<SkillTreeSetting>("SkillTreeSetting");
			
			foreach (var data in setting.datas){
				var cell =  GameObject.Instantiate(cellPrefab,tf);
				(cell.transform as RectTransform).localPosition = data.pos;
				SkillCell skillCell = cell.GetComponent<SkillCell>();
				cells.Add(skillCell);
			}
			//点亮和熄灭
			//玩家解锁数据保存
			UpdateUnlock();
			IsInited = true;
		}

		void UpdateUnlock(){
			var dic = PlayerSaveData.Current.skillUnlockDic;
			foreach (var cell in cells){
				int level = 0;
				dic.TryGetValue(cell.skillIndex,out level);
				
				cell.SetUnlock(level>0);
				
			}
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