using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace XiaoCao
{
    ///<see cref="BuffPanelView"/>
    public class BuffItemCell : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Image bg;

        public Image icon;

        public ColorSettingSo colorSetting;

        public SpriteSettingSo spriteSetting;

        [XCHeader("DebugView")]
        public BuffItem buffItem;

        public BuffItemCell TempItemCell { get; set; }

        public bool IsEquiped { get; set; }

        public int Index { get; set; }
        public bool CanDrag { get; set; }

        public Action<Vector2> OnBeginDragAct;
        public Action OnBuffChangeAct;
        public Action<Vector2> OnDragAct;

        public Action<BuffItem> OnButtonClick;

        public BuffPanelView panelView;

        private void Start()
        {
            //transform.GetComponent<Button>().onClick.AddListener(OnButonClick);
        }

        public void SetValue(BuffItem BuffItem)
        {
            this.buffItem = BuffItem;
            RefreshView();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                //自动装备
                RightClick();
            }
            else
            {
                OnButonClick();
            }

        }

        void RightClick()
        {
            int toIndex = PlayerHelper.LocalPlayerBuffs.FindEmptyIndex(!IsEquiped);
            PlayerHelper.LocalPlayerBuffs.MoveBuff(IsEquiped, Index, !IsEquiped, toIndex);
            OnBuffChangeAct?.Invoke();
        }

        //点击监听
        void OnButonClick()
        {
            Debug.Log($"--- click");
            //显示单个->右侧显示
            OnButtonClick?.Invoke(buffItem);
        }

        //TODO代码分块可以稍微做下
        public void OnBeginDrag(PointerEventData eventData)
        {
            CanDrag = buffItem.GetBuffType != EBuffType.None;
            if (!CanDrag)
            {
                return;
            }


            //隐藏自身
            SetFade();
            OnBeginDragAct?.Invoke(eventData.position);
            TempItemCell.buffItem = buffItem;
            TempItemCell.RefreshView();
            TempItemCell.gameObject.SetActive(true);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!CanDrag)
            {
                return;
            }
            OnDragAct?.Invoke(eventData.position);
            TempItemCell.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!CanDrag)
            {
                return;
            }

            TempItemCell.gameObject.SetActive(false);

            //判断落点 获取位置的BuffItem
            BuffItemCell nextCell = GetPointBuffItemCell(eventData);

            bool isNull = nextCell == null;

            if (isNull || nextCell == this)
            {
                Debug.Log($"--- 撤销 isNull {isNull}");
                RefreshView();
                EnableRayCast(true);
                //撤销移动
                return;
            }

            bool canUpgrade = IsCanUgrade(nextCell, out bool isMaxLevel);
            if (canUpgrade)
            {
                //合成
                UpgradeBuff(nextCell);
            }
            else
            {
                //确保一个ExBuff只能装备一个
                if (isMaxLevel || !HasSameExBuff())
                {
                    ExChange(nextCell);
                }
            }
            RefreshView();
            EnableRayCast(true);
        }

        private bool IsCanUgrade(BuffItemCell nextCell, out bool isMaxLevel)
        {
            isMaxLevel = false;
            //升级设计: Ex, 同类型, 同等级
            EBuffType nextType = nextCell.buffItem.GetBuffType;
            if (nextType == EBuffType.Ex && buffItem.GetBuffType == EBuffType.Ex
                && buffItem.GetFirstEBuff == nextCell.buffItem.GetFirstEBuff)
            {
                if (buffItem.IsMaxLevel)
                {
                    isMaxLevel = true;
                    return false;
                }
                return true;
            }
            return false;
            //return nextCell.buffItem.CanUpGradeItem(buffItem);
        }

        private bool HasSameExBuff()
        {
            foreach (var item in PlayerHelper.LocalPlayerBuffs.EquippedExBuffs)
            {
                if (item.GetFirstEBuff == buffItem.GetFirstEBuff)
                {
                    return true;
                }
            }
            return false;
        }

        //需要拦截同类型ExBuff
        void ExChange(BuffItemCell nextCell)
        {
            PlayerHelper.LocalPlayerBuffs.MoveBuff(IsEquiped, Index, nextCell.IsEquiped, nextCell.Index);
            OnBuffChangeAct?.Invoke();
        }

        void UpgradeBuff(BuffItemCell nextCell)
        {
            PlayerHelper.LocalPlayerBuffs.CombineItem(IsEquiped, Index, nextCell.IsEquiped, nextCell.Index);
            OnBuffChangeAct?.Invoke();
        }

        public BuffItemCell GetPointBuffItemCell(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject)
            {
                Debug.Log($"--- {eventData.pointerCurrentRaycast.gameObject.name}");
                return eventData.pointerCurrentRaycast.gameObject.GetComponent<BuffItemCell>();
            }

            return null;

        }

        public void ReGetValue()
        {
            buffItem = PlayerHelper.LocalPlayerBuffs.GetValue(IsEquiped, Index);
            RefreshView();
            EnableRayCast(true);
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
            int len = colorSetting.values.Length;
            if (buffItem.GetBuffType == EBuffType.None)
            {
                bg.color = colorSetting.values[len - 1];
                return;
            }

            int index = buffItem.level;
            if (index < 0)
            {
                index = len - 1;
            }
            if (index >= colorSetting.values.Length)
            {
                index = colorSetting.values.Length - 1;
            }
            bg.color = colorSetting.values[index];
        }

        private void SetFade()
        {
            icon.enabled = false;
            int len = colorSetting.values.Length;
            bg.color = colorSetting.values[len - 1];
            EnableRayCast(false);
        }

        public void EnableRayCast(bool isOn)
        {
            bg.raycastTarget = isOn;
        }


        [Button()]
        void TestColor()
        {
            SetBgColor();
        }
    }

}