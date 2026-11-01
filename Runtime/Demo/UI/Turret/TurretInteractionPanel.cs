using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>炮台交互界面的数据</summary>
    public class TurretPanelData : IUIData
    {
        public Turret turret;

        public TurretPanelData(Turret turret)
        {
            this.turret = turret;
        }
    }

    /// <summary>
    /// 炮台交互界面: 由玩家靠近炮台按攻击键打开,
    /// 可选择自己操控、指派在场支援角色操控, 或终止当前操控。
    /// </summary>
    public class TurretInteractionPanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.TurretPanel;

        public override bool NeedUIData => true;

        [Header("标题")]
        public TMP_Text titleText;

        [Header("操作")]
        public Button selfControlBtn;

        public TMP_Text selfControlText;

        public Button stopControlBtn;

        public TMP_Text stopControlText;

        public Button closeBtn;

        [Header("支援角色列表")]
        [Tooltip("支援角色格子容器, 第一个子节点作为模板")]
        public Transform friendRoot;

        [Tooltip("没有可用支援角色时显示的提示")]
        public GameObject noFriendTip;

        private Turret turret;
        private bool isInited;

        private readonly List<Role> friends = new List<Role>();

        public override void Show(IUIData uiData = null)
        {
            if (!isInited)
            {
                Init();
            }

            turret = ((TurretPanelData)uiData).turret;
            gameObject.SetActive(true);
            Refresh();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
            UIMgr.Inst.HideView(PanelType);
        }

        public void OnCloseBtnClick()
        {
            Hide();
        }

        /// <summary>按钮与文案只在第一次显示时初始化</summary>
        private void Init()
        {
            isInited = true;
            titleText.text = "TurretTitle".ToLocalizeStr();
            stopControlText.text = "TurretStopControl".ToLocalizeStr();
            selfControlBtn.onClick.AddListener(OnSelfControlClick);
            stopControlBtn.onClick.AddListener(OnStopControlClick);
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        /// <summary>按当前炮台状态刷新按钮与支援角色列表</summary>
        private void Refresh()
        {
            bool isPlayerOperating = turret.OperatorRole == GameDataCommon.LocalPlayer;
            selfControlText.text = (isPlayerOperating ? "TurretSelfControlling" : "TurretSelfControl").ToLocalizeStr();
            selfControlBtn.interactable = !isPlayerOperating;

            stopControlBtn.gameObject.SetActive(turret.HasOperator);

            RefreshFriends();
        }

        /// <summary>列出在场且存活的支援角色, 正在操控炮台的格子会高亮</summary>
        private void RefreshFriends()
        {
            friends.Clear();

            List<int> friendIds = GameDataCommon.LocalPlayer.playerData.friends;
            for (int i = 0; i < friendIds.Count; i++)
            {
                Role role = friendIds[i].GetRoleById();
                if (role != null && !role.IsDie)
                {
                    friends.Add(role);
                }
            }

            UITool.SetCellListCount(friendRoot, friends.Count);

            for (int i = 0; i < friends.Count; i++)
            {
                TurretFriendCell cell = friendRoot.GetChild(i).GetComponent<TurretFriendCell>();
                cell.Init(friends[i], turret.OperatorRole == friends[i], OnFriendCellClick);
            }

            noFriendTip.SetActive(friends.Count == 0);
        }

        /// <summary>自己操控: 关掉界面直接接管炮台</summary>
        private void OnSelfControlClick()
        {
            if (turret.TakeControlByPlayer(GameDataCommon.LocalPlayer))
            {
                Hide();
            }
        }

        /// <summary>指派支援角色操控</summary>
        private void OnFriendCellClick(Role friend)
        {
            if (turret.TakeControlByAiRole(friend))
            {
                Hide();
            }
        }

        /// <summary>终止当前操控, 界面保持打开以便重新指派</summary>
        private void OnStopControlClick()
        {
            turret.ExitControl();
            Refresh();
        }
    }
}
