using UnityEngine;
using XiaoCao;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 属性祝福主界面，负责创建并刷新各属性祝福条目。
    /// </summary>
    public class BlessingView : EnableShowUI
    {
        public Transform blessingParent;

        private void Awake()
        {
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.BlessingChange, UpdateUI);
        }

        private void OnDestroy()
        {
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.BlessingChange, UpdateUI);
        }

        /// <summary>
        /// 刷新所有祝福条目的等级、效果和升级状态。
        /// </summary>
        public override void UpdateUI()
        {
            if (blessingParent == null)
            {
                Debug.LogError(LocalizeKey.BlessingListParentMissing.ToLocalizeStr());
                return;
            }

            PlayerSaveData playerSaveData = GameAllData.playerSaveData;
            if (playerSaveData == null)
            {
                return;
            }

            playerSaveData.CheckBlessingNull();

            EBlessing[] blessings = BlessingRule.AllBlessings;
            UITool.SetCellListCount(blessingParent, blessings.Length);
            AttributeBlessingUI[] subUIList = blessingParent.GetComponentsInChildren<AttributeBlessingUI>(false);
            if (subUIList.Length < blessings.Length)
            {
                Debug.LogError(LocalizeKey.FormatWithArgs(
                    LocalizeKey.BlessingCellCountNotEnough,
                    subUIList.Length,
                    blessings.Length));
                return;
            }

            for (int i = 0; i < blessings.Length; i++)
            {
                subUIList[i].SetValue(blessings[i], OnBlessingUpgrade);
            }
        }

        /// <summary>
        /// 刷新祝福页显示，供新手引导主动重绘。
        /// </summary>
        public void RefreshGuideUI()
        {
            UpdateUI();
        }

        private void OnBlessingUpgrade(EBlessing blessing)
        {
            UICanvasMgr.Inst.EventSystem.SendEvent(UIEventNames.BlessingChange);
        }
    }
}
