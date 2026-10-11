using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class ItemCell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>经验值图标使用的特殊颜色 #FFED51</summary>
        private static readonly Color ExpIconColor = new Color32(0xFF, 0xED, 0x51, 0xFF);

        private const string ExpIconMaterialPath = "Assets/_Res/UI/UI_Shader/UIFlow_Exp.mat";

        private static Material _expIconMaterial;

        /// <summary>经验值图标的流光材质,首次使用时加载并缓存</summary>
        private static Material ExpIconMaterial
        {
            get
            {
                if (_expIconMaterial == null)
                {
                    _expIconMaterial = ResMgr.LoadAseet<Material>(ExpIconMaterialPath);
                }

                return _expIconMaterial;
            }
        }

        public Item Item { get; private set; }

        public Image icon;

        public Image qualityBg;

        public Button btn;

        public TextMeshProUGUI text;

        [Tooltip("圣器等级文本,只在物品是圣器时显示")]
        public TextMeshProUGUI levelText;

        public EItemClickMode clickMode;

        public Action<ItemCell> clickAction;


        private void Awake()
        {
            if (btn != null)
            {
                btn.onClick.AddListener(OnBtnClick);
            }
        }

        /// <summary>
        /// 写入格子显示的物品与数量文本,并同步刷新图标和品质底色。
        /// </summary>
        public ItemCell SetItem(Item item, UIItemTextType textType = UIItemTextType.Default)
        {
            this.Item = item;
            //图标只由物品决定,统一在格子内部刷新,调用方无需再单独设置
            icon.sprite = item.GetItemSprite();
            RefreshIconStyle(item);
            icon.enabled = icon.sprite != null;
            SetQualityBg(item.quality);
            RefreshLevelText(item);
            if (textType == UIItemTextType.NeedNum)
            {
                int hasCount = GameAllData.playerSaveData.inventory.GetItemCount(item.ItemKey);
                if (hasCount < item.num)
                {
                    text.text = $"<color=red>{hasCount}</color>/{item.num}";
                }
                else
                {
                    text.text = $"<color=#50FF99>{hasCount}</color>/{item.num}";
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

        /// <summary>
        /// 刷新圣器等级:圣器显示等级,其他物品隐藏等级文本
        /// 部分格子预制体(金币条、技能消耗格)没有等级文本节点,未绑定时跳过
        /// </summary>
        private void RefreshLevelText(Item item)
        {
            if (levelText == null)
            {
                return;
            }

            bool isRelic = item.type == ItemType.HolyRelic;
            levelText.gameObject.SetActive(isRelic);
            if (isRelic)
            {
                //圣器等级存在 typeId 的第 3 段,如 Relic/Weapon_1_10 的 10
                levelText.text = $"Lv{item.ToRelicItem().level}";
            }
        }

        /// <summary>
        /// 刷新图标颜色和材质:经验值使用金色流光材质,其他物品保持白色且不带材质
        /// </summary>
        private void RefreshIconStyle(Item item)
        {
            bool isExp = item.type == ItemType.OtherValue && item.typeId == OtherValueItemId.Exp;
            icon.material = isExp ? ExpIconMaterial : null;
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

        /// <summary>
        /// 常用参数  hasGet
        /// </summary>
        public void SetMask(string mask,bool maskActive  = true)
        {
            var maskTf = transform.Find(mask);
            if (maskTf)
            {
                maskTf.gameObject.SetActive(maskActive);
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
