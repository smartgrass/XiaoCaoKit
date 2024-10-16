using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillCell : MonoBehaviour,IPointerClickHandler
	{

		public Color lockColor = Color.white;
		public Color unlockColor = Color.white;
		public Color unlockRingColor = Color.yellow;

		public TMP_Text levelText;
		public Image img;
		public Image ringImg;
		public Button button;
		public int skillIndex;

		public bool isUnlock;
		public Action clickAct;

        public void OnPointerClick(PointerEventData eventData)
        {
			clickAct?.Invoke();
        }

        public void SetUnlock(bool isUnlock){
			if (isUnlock)
			{
				ringImg.color = unlockRingColor;
                img.color = unlockColor;		
            }
			else
			{
				ringImg.color = lockColor;
                img.color = lockColor;
            }
			this.isUnlock = isUnlock;

        }

		[Button]
		void SetColor()
		{
			SetUnlock(isUnlock);
		}
	}
}