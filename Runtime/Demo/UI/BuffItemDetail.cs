using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using UnityEngine.EventSystems;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.UI
{
    public class BuffItemDetail : MonoBehaviour, IPointerClickHandler
    {
        public TMP_Text itemName;
        public TMP_Text itemDesc;
        public Image itemIcon;
        public Image itemBg;

        public GameObject select;

        public Action<BuffItemDetail> onSelect;

        private BuffItem _buffItem;

        private void Awake()
        {
            SetSelect(false);
        }

        // 实现点击事件接口
        public void OnPointerClick(PointerEventData eventData)
        {
            onSelect?.Invoke(this);
        }

        public void SetSelect(bool isSelect)
        {
            select.gameObject.SetActive(isSelect);
        }

        public void SetValue(BuffItem buffItem)
        {
            _buffItem = buffItem;
            itemName.text = LocalizeKey.GetBuffNameKey(buffItem.GetFirstEBuff).ToLocalizeStr();
            itemBg.color = buffItem.GetColor();
            itemDesc.text = LocalizeKey.GetBuffInfoDesc(buffItem.buffs[0]);
            itemIcon.sprite = buffItem.GetBuffSprite();
        }
    }
}