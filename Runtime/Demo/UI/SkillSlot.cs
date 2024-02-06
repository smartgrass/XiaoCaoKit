
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillSlot : MonoBehaviour
    {

        public Image image;

        public Image maskImg;


        public bool isCold;

        public void OnUpdate(float fill)
        {
            maskImg.fillAmount = fill;
        }

        public void Clear()
        {

        }

    }
}
