using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCao.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        public Image img;
        public Button btn;
        public TMP_Text text;
        public Image imgLock;
        public Image imgNoPass;
        public Color[] colors;


        public void Show(int chapter, int index)
        {
            text.text = LocalizeKey.GetLevelName(chapter, index);

            // 根据关卡状态设置显示
            LevelPassState passState = GetPassState(chapter, index);
            text.color = colors[(int)(passState)];
            switch (passState)
            {
                case LevelPassState.Lock:
                    SetSprite(0); // 未解锁状态
                    btn.interactable = false;
                    break;
                case LevelPassState.Unlock:
                    SetSprite(1); // 未通关状态
                    btn.interactable = true;
                    break;
                case LevelPassState.Pass:
                    SetSprite(2); // 已通过状态
                    btn.interactable = true;
                    break;
            }
        }

        public void SetSprite(int index)
        {
            //3种 未解锁0,未通关1,已通过2
            bool showLock = index == 0;
            imgLock.enabled = showLock;
            bool showNoPass = index == 1;
            imgNoPass.enabled = showNoPass;
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }
    }
}