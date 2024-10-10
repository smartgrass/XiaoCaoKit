using TMPro;
using UnityEngine;

namespace XiaoCao
{

    public class SkillDisplay: MonoBehaviour{
		public TMP_Text nameText;
		public TMP_Text desText;
		public void Show(int skillIndex){
			gameObject.SetActive(true);
			//TODO 本地化
		}
	}

}