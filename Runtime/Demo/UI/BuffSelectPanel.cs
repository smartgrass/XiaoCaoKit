using System.Collections.Generic;
using UnityEngine;
using XiaoCaoKit.Runtime.Demo.UI;
using Button = UnityEngine.UI.Button;

namespace XiaoCao.UI
{
    /// <summary>
    /// <see cref="BuffItemDetail"/>
    /// <see cref="BuffSelectPanelData"/>
    /// </summary>
    public class BuffSelectPanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.BuffSelectPanel;
        public override bool NeedUIData => true;
        public override bool HideEsc => false;
        public Transform buffContainer;
        public Button sureBtn;

        public GameObject prefabRoot;
        public GameObject buffItemPrefab;

        private List<BuffItem> availableBuffs = new List<BuffItem>();
        private List<BuffItemDetail> buffItemDetails = new List<BuffItemDetail>();
        private BuffItem selectedBuff;
        private System.Action<BuffItem> onSelectCallback;

        // 记录当前选中的详情项
        private BuffItemDetail selectedDetail;

        public override void Show(IUIData data)
        {
            BuffSelectPanelData uiData = (BuffSelectPanelData)data;
            ShowWith(uiData.buffItems, uiData.onSelect);
        }

        
        public override void Hide()
        {
            // 清理引用
            onSelectCallback = null;
            selectedBuff = null;
            selectedDetail = null;

            // 清除现有选项
            foreach (var detail in buffItemDetails)
            {
                if (detail)
                {
                    Destroy(detail.gameObject);
                }
            }

            buffItemDetails.Clear();

            UIMgr.Inst.PopUIEnable(false, name);

            gameObject.SetActive(false);
        }

        public void Init()
        {
            sureBtn.onClick.AddListener(OnConfirmButtonClicked);
            sureBtn.interactable = false;
            gameObject.SetActive(false);
            prefabRoot.SetActive(false);
            buffItemPrefab.transform.SetParent(prefabRoot.transform);
        }


        private void ShowWith(List<BuffItem> buffs, System.Action<BuffItem> onSelect)
        {
            gameObject.SetActive(true);

            UIMgr.Inst.PopUIEnable(true, name);

            availableBuffs = buffs;
            onSelectCallback = onSelect;
            selectedBuff = null;
            selectedDetail = null;

            if (sureBtn != null)
                sureBtn.interactable = false;

            RefreshBuffDisplay();
        }

        private void RefreshBuffDisplay()
        {
            // 清除现有选项
            foreach (var detail in buffItemDetails)
            {
                if (detail != null)
                    Destroy(detail.gameObject);
            }

            buffItemDetails.Clear();

            // 创建新的选项
            foreach (var buff in availableBuffs)
            {
                if (buffItemPrefab != null && buffContainer != null)
                {
                    GameObject buffGO = Instantiate(buffItemPrefab, buffContainer);
                    BuffItemDetail buffDetail = buffGO.GetComponent<BuffItemDetail>();
                    if (buffDetail != null)
                    {
                        buffDetail.SetValue(buff);
                        // 注册选择回调
                        buffDetail.onSelect = OnBuffDetailSelected;
                        buffItemDetails.Add(buffDetail);
                    }
                }
            }


            //自动选中
            OnBuffDetailSelected(buffItemDetails[0]);
        }

        private void OnBuffDetailSelected(BuffItemDetail buffDetail)
        {
            // 取消之前选中的项
            if (selectedDetail != null)
            {
                selectedDetail.SetSelect(false);
            }

            // 设置新选中的项
            selectedDetail = buffDetail;
            selectedDetail.SetSelect(true);

            // 查找对应的 BuffItem
            int index = buffItemDetails.IndexOf(buffDetail);
            if (index >= 0 && index < availableBuffs.Count)
            {
                selectedBuff = availableBuffs[index];
            }

            if (sureBtn != null)
            {
                sureBtn.interactable = true;
            }
        }

        private void OnConfirmButtonClicked()
        {
            onSelectCallback?.Invoke(selectedBuff);
            UIMgr.Inst.HideView(this.PanelType);
        }

        public override void InputKeyCode(KeyCode key)
        {
            if (key == KeyCode.Space)
            {
                OnConfirmButtonClicked();
            }
        }
    }

    public struct BuffSelectPanelData : IUIData
    {
        public List<BuffItem> buffItems;
        public System.Action<BuffItem> onSelect;

        public BuffSelectPanelData(List<BuffItem> buffItems, System.Action<BuffItem> onSelect)
        {
            this.buffItems = buffItems;
            this.onSelect = onSelect;
        }
    }
}