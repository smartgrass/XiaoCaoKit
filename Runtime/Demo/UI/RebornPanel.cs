using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 复活界面
    /// 当前关卡模式首次复活消耗100金币
    /// 同一局内重复付费复活费用翻倍
    /// 关卡模式付费复活上限为4次
    /// </summary>
    public class RebornPanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.RebornPanel;
        public override bool HideEsc => false;

        public Button rebornBtn; // 复活按钮
        public Button reloadBtn; // 重开本关
        public Button exitBtn; // 退出到大厅

        public TMPro.TMP_Text costText;
        public TMPro.TMP_Text hasCoinText;

        public void Init()
        {
            gameObject.SetActive(false);
            rebornBtn.onClick.AddListener(OnReviveBtnClicked);
            exitBtn.onClick.AddListener(OnExitBtnClicked);
            reloadBtn.onClick.AddListener(OnReload);

            GameEvent.AddEventListener<int>(EGameEvent.PlayerDead.ToInt(), OnPlayerDead);
        }

        public void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.PlayerDead.ToInt(), OnPlayerDead);
        }

        public override void Show(IUIData data = null)
        {
            gameObject.SetActive(true);
            UIMgr.Inst.PopUIEnable(true, name);
            RefreshRebornBtnState();
            costText.text = $"<sprite=0>{GameMgr.Inst.GetCurrentRebornCost()}";
            hasCoinText.text = $"<sprite=0>{PlayerSaveData.LocalSavaData.Coin}";
        }

        public override void Hide()
        {
            UIMgr.Inst.PopUIEnable(false, name);
            gameObject.SetActive(false);
        }

        private void OnPlayerDead(int playerId)
        {
            UIMgr.Inst.ShowView(PanelType);
        }

        private void RefreshRebornBtnState()
        {
            if (rebornBtn == null || GameMgr.Inst == null)
            {
                return;
            }

            rebornBtn.interactable = GameMgr.Inst.CanPaidReborn();
        }

        private void OnReviveBtnClicked()
        {
            Debug.Log(
                $"-- click reborn cost:{GameMgr.Inst.GetCurrentRebornCost()} count:{BattleData.Current.paidRebornCount}");
            if (!GameMgr.Inst.TryPaidReborn())
            {
                RefreshRebornBtnState();
                return;
            }

            UIMgr.Inst.HideView(PanelType);
        }

        private void OnExitBtnClicked()
        {
            Debug.Log("-- click exit reborn panel");
            UIMgr.Inst.HideView(PanelType);
            GameMgr.Inst.BackHome();
        }

        private void OnReload()
        {
            GameMgr.Inst.ReloadScene();
        }

        public override void InputKeyCode(KeyCode key)
        {
        }
    }
}