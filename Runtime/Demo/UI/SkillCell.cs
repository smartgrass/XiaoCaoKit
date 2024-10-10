using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillCell : MonoBehaviour{
		public TMP_Text levelText;
		public Image icon;
		public Image unlockMask;
		public Button button;
		public int skillIndex;

		public void SetUnlock(bool isUnlock){
			unlockMask.enabled = isUnlock;
		}
	}
}