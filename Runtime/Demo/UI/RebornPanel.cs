using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 复活界面 - 通过监听PlayerDead事件触发, 选项重来,退出,复活
    /// 复活费用:50
    /// </summary>
    public class RebornPanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.RebornPanel;
        public override bool HideEsc => false;

        public Button rebornBtn; // 复活按钮
        public Button reloadBtn; // 重载
        public Button exitBtn; // 退出按钮


        public void Init()
        {
            rebornBtn.onClick.AddListener(OnReviveBtnClicked);
            exitBtn.onClick.AddListener(OnExitBtnClicked);
            reloadBtn.onClick.AddListener(OnReload);


            // 订阅玩家死亡事件
            GameEvent.AddEventListener<int>(EGameEvent.PlayerDead.ToInt(), OnPlayerDead);
        }


        public void OnDestroy()
        {
            // 取消订阅事件
            GameEvent.RemoveEventListener<int>(EGameEvent.PlayerDead.ToInt(), OnPlayerDead);
        }

        public override void Show(IUIData data = null)
        {
            gameObject.SetActive(true);
            UIMgr.Inst.PopUIEnable(true, name);
        }

        public override void Hide()
        {
            UIMgr.Inst.PopUIEnable(false, name);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 玩家死亡事件回调
        /// </summary>
        private void OnPlayerDead(int playerId)
        {
            UIMgr.Inst.ShowView(this.PanelType);
        }


        /// <summary>
        /// 复活按钮点击
        /// </summary>
        private void OnReviveBtnClicked()
        {
            Debug.Log("点击复活按钮");
            Debug.Log("执行复活逻辑");

            // 这里应该调用玩家复活的逻辑
            // 例如：重置玩家状态、恢复生命值等
            var localPlayer = GameDataCommon.LocalPlayer;
            if (localPlayer != null)
            {
                // 调用玩家的复活逻辑
                localPlayer.OnReborn();
            }

            // 隐藏复活界面
            UIMgr.Inst.HideView(this.PanelType);
        }

        /// <summary>
        /// 退出按钮点击
        /// </summary>
        private void OnExitBtnClicked()
        {
            Debug.Log("点击退出按钮");
            UIMgr.Inst.HideView(this.PanelType);
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