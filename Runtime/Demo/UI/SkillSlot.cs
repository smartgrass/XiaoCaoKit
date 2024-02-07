
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillSlot : MonoBehaviour
    {

        public Image image;

        public Image fieldImg;


        public bool isCold;

        public void OnUpdate(float fill)
        {
            fieldImg.fillAmount = fill;
        }

        public void Clear()
        {

        }

    }
}
