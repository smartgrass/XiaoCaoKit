using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace XiaoCao.UI
{
    ///<see cref="BuffView"/>
    public class BuffItemCell : MonoBehaviour
    {
        public Image bg;

        public Image icon;

        public Button btn;
        private ColorSettingSo ColorSetting => UIPrefabSo.Inst.buffColorSettingSo;

        public SpriteSettingSo spriteSetting;

        [XCHeader("DebugView")] public BuffItem buffItem;

        public BuffItemCell TempItemCell { get; set; }

        public Action onBuffChangeAct;

        public int Index { get; set; }

        public Action<BuffItem> onButtonClick;

        public BuffView view;

        private void OnEnable()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonClick);
                btn.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonClick);
            }
        }

        public void SetValue(BuffItem item)
        {
            this.buffItem = item;
            RefreshView();
        }


        //点击监听
        void OnButtonClick()
        {
            onButtonClick?.Invoke(buffItem);
        }

        public void RefreshView()
        {
            SetIcon();
            SetBgColor();
        }

        public void SetIcon()
        {
            int index = (int)buffItem.GetBuffType;
            if (index < 0)
            {
                icon.enabled = false;
                return;
            }

            icon.sprite = spriteSetting.values[index];
            icon.enabled = true;
        }

        public void SetBgColor()
        {
            // bg.color = buffItem.GetColor();
        }

        [Button()]
        void TestColor()
        {
            SetBgColor();
        }
    }
}