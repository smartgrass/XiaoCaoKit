
using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillSlot : MonoBehaviour
    {
        public Image image;

        public Image fieldImg;

        public SimpleImageTween effectTween;

        public bool isColdLastFrame;

        public void OnUpdate(float fill)
        {
            fieldImg.fillAmount = fill;
        }

        public void Clear()
        {

        }

        internal void PlayEffect()
        {
            effectTween.Play();
            image.color = Color.white;
        }

        internal void EnterCD()
        {
            image.color = new Color(0.8f, 0.8f, 0.8f);
        }
    }
}
