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
    /// 炮台交互界面: 由玩家靠近炮台按攻击键(J)打开。
    /// 列表第一项固定是玩家自己, 后面是在场支援角色;
    /// 点击角色头像 / 按 J 都是"选中该角色并立刻让他接管炮台"; 也可以终止当前操控
    /// </summary>
    public class TurretInteractionPanel : PanelBase
    {
        /// <summary>确定键: 与打开界面用的是同一个普攻键</summary>
        private const KeyCode ConfirmKey = KeyCode.J;

        public override UIPanelType PanelType => UIPanelType.TurretPanel;

        public override bool NeedUIData => true;

        [Header("标题")]
        public TMP_Text titleText;

        [Header("操作")]
        public Button stopControlBtn;

        public TMP_Text stopControlText;

        public Button closeBtn;

        [Header("角色列表")]
        [Tooltip("角色格子容器, 第一个子节点作为模板")]
        public Transform friendRoot;

        [Tooltip("没有可用角色时显示的提示")]
        public GameObject noFriendTip;

        private Turret turret;
        private bool isInited;

        /// <summary>可指派列表: 第 0 项固定是玩家自己, 之后是在场且存活的支援角色</summary>
        private readonly List<Role> candidates = new List<Role>();

        private int selectedIndex;

        /// <summary>界面通常就是按 J 打开的, 开屏那一下按键还没松开, 要先等它抬起再接收确定</summary>
        private bool waitConfirmKeyRelease;

        public override void Show(IUIData uiData = null)
        {
            if (!isInited)
            {
                Init();
            }

            turret = ((TurretPanelData)uiData).turret;
            selectedIndex = 0;
            waitConfirmKeyRelease = Input.GetKey(ConfirmKey);
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

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            if (waitConfirmKeyRelease)
            {
                if (!Input.GetKey(ConfirmKey))
                {
                    waitConfirmKeyRelease = false;
                }

                return;
            }

            if (Input.GetKeyDown(ConfirmKey))
            {
                ConfirmSelected();
            }
        }

        /// <summary>按钮与文案只在第一次显示时初始化</summary>
        private void Init()
        {
            isInited = true;
            titleText.text = "TurretTitle".ToLocalizeStr();
            stopControlText.text = "TurretStopControl".ToLocalizeStr();
            stopControlBtn.onClick.AddListener(OnStopControlClick);
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        /// <summary>按当前炮台状态刷新按钮与角色列表</summary>
        private void Refresh()
        {
            stopControlBtn.gameObject.SetActive(turret.HasOperator);
            RefreshCandidates();
        }

        /// <summary>
        /// 刷新可指派列表: 自己排第一, 后面是能选的支援角色(在场、存活、离玩家不超过炮台配置的距离)。
        /// 自己的显示 key 用本地设置里选的角色(Role_x), 不受皮肤 bodyName 影响
        /// </summary>
        private void RefreshCandidates()
        {
            candidates.Clear();
            candidates.Add(GameDataCommon.LocalPlayer);

            List<int> friendIds = GameDataCommon.LocalPlayer.playerData.friends;
            for (int i = 0; i < friendIds.Count; i++)
            {
                Role role = friendIds[i].GetRoleById();
                if (turret.IsFriendSelectable(role))
                {
                    candidates.Add(role);
                }
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, candidates.Count - 1);

            UITool.SetCellListCount(friendRoot, candidates.Count);

            for (int i = 0; i < candidates.Count; i++)
            {
                TurretFriendCell cell = friendRoot.GetChild(i).GetComponent<TurretFriendCell>();
                //第一项是自己: 名字与头像都按本地设置里选的角色走
                string displayKey = i == 0 ? GetSelfRoleKey() : null;
                cell.Init(candidates[i], turret.OperatorRole == candidates[i], i == selectedIndex,
                    OnCellClick, displayKey);
            }

            noFriendTip.SetActive(candidates.Count == 0);
        }

        /// <summary>自己这一格显示用的 key: 与 Player0 生成 bodyName 的规则一致</summary>
        private static string GetSelfRoleKey()
        {
            return $"Role_{ConfigMgr.Inst.LocalRoleSetting.selectRole}";
        }

        /// <summary>点击格子: 选中该角色, 并立刻让他接管炮台(与按确定键同一套流程)</summary>
        private void OnCellClick(Role role)
        {
            int index = candidates.IndexOf(role);
            if (index < 0)
            {
                return;
            }

            selectedIndex = index;
            ConfirmSelected();
        }

        /// <summary>确定: 让当前选中的角色接管炮台, 成功后关闭界面</summary>
        private void ConfirmSelected()
        {
            Role role = candidates[selectedIndex];
            bool ok = role.IsPlayer
                ? turret.TakeControlByPlayer((Player0)role)
                : turret.TakeControlByAiRole(role);

            if (ok)
            {
                Hide();
                return;
            }

            //接管失败(比如炮台正被别人占着): 界面留着, 顺手同步一下高亮
            RefreshCandidates();
        }

        /// <summary>终止当前操控, 界面保持打开以便重新指派</summary>
        private void OnStopControlClick()
        {
            //玩家主动终止 = 不想让这个角色继续守着炮台, 顺手摘掉"一直守着"的 Tag,
            //否则支援角色被揪下来 1 秒后又会自己跑回去
            Role role = turret.Operator;
            if (role != null)
            {
                role.RemoveTag(RoleTagCommon.KeepOperateFunction);
            }

            turret.ExitControl();
            Refresh();
        }
    }
}
