using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class ItemCell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Item Item { get; private set; }

        public Image icon;

        public Image qualityBg;

        public Button btn;

        public TextMeshProUGUI text;

        public EItemClickMode  clickMode;
        
        public Action<ItemCell> clickAction;
        

        private void Awake()
        {
            if (btn!=null)
            {
                btn.onClick.AddListener(OnBtnClick);
            }
        }

        public ItemCell SetItem(Item item, UIItemTextType textType = UIItemTextType.Default)
        {
            this.Item = item;
            SetQualityBg(item.quality);
            if (textType == UIItemTextType.NeedNum)
            {
                int hasCount = GameAllData.playerSaveData.inventory.GetItemCount(item.ItemKey);
                if (hasCount < item.num)
                {
                    text.text = $"<color=red>{hasCount}</color>/{item.num}";
                }
                else
                {
                    text.text = $"{hasCount}/{item.num}";
                }
            }
            else if (textType == UIItemTextType.NoNum)
            {
                text.text = "";
            }
            else
            {
                text.text = item.num.ToString();
            }
    
            return this;
        }

        public void SetQualityBg(EQuality quality)
        {
            if (!qualityBg)
            {
                return;
            }
            ColorSettingSo colorSetting = UIPrefabSo.Inst.itemColorSettingSo;
            float a = 132f / 255;
            if (colorSetting.values.Length <= (int)quality)
            {
                Debug.LogError($"-- colorSetting len {colorSetting.values.Length} <= {quality} {(int)quality})");
            }
            
            var color = colorSetting.values[(int)quality];
            color.a = a;
            qualityBg.color = color;
        }

        public void SetMask(string mask)
        {
            var maskTf = transform.Find(mask);
            if (maskTf)
            {
                maskTf.gameObject.SetActive(true);
            }
        }

        public void SetNum(int num)
        {
            text.text = num.ToString();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnBtnClick()
        {
            //1.默认显示详情
            if (clickMode == EItemClickMode.ShowDetail)
            {
                UIMgr.Inst.PopItemDetail();
            }
            //2.定制化需求,不做反应,只给回调
            else if (clickMode == EItemClickMode.Custom)
            {
                clickAction?.Invoke(this);
            }
        }
    }

    public enum UIItemTextType
    {
        Default,
        NeedNum,
        NoNum
    }

    public enum EItemClickMode
    {
        None,
        ShowDetail, //显示详情
        Custom //定制
    }
}