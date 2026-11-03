using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 炮台界面里的角色格子: 显示头像/名字, 并标记该角色是否正在操控本炮台、是否被选中。
    /// 列表第一项是玩家自己, 其余是在场支援角色
    /// </summary>
    public class TurretFriendCell : MonoBehaviour
    {
        public Button btn;

        public Image icon;

        public TMP_Text nameText;

        [Tooltip("正在操控本炮台的标记")]
        public GameObject inUseMark;

        /// <summary>选中项保持原色, 未选中项整体压暗</summary>
        private static readonly Color SelectedColor = Color.white;

        // private static readonly Color UnselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private Role friend;
        private Action<Role> onClick;
        private bool hasListener;

        /// <summary>
        /// 绑定角色并刷新显示。
        /// displayKey 用来指定显示用的 key(自己那一行传 Role_x),
        /// 不传时取 role 自己的 bodyName: 支援角色的 bodyName 就是 Role_x(召唤时由 GameAllData 同步),
        /// 与角色图标和本地化表一致
        /// </summary>
        public void Init(Role role, bool isOperating, bool isSelected, Action<Role> onClickAction,
            string displayKey = null)
        {
            friend = role;
            onClick = onClickAction;

            if (!hasListener)
            {
                hasListener = true;
                btn.onClick.AddListener(OnBtnClick);
            }

            string roleKey = string.IsNullOrEmpty(displayKey) ? role.idRole.bodyName : displayKey;
            Sprite roleSprite = string.IsNullOrEmpty(roleKey) ? null : SpriteResHelper.LoadRoleIcon(roleKey);

            if (roleSprite == null && string.IsNullOrEmpty(displayKey))
            {
                //老存档里的支援角色 bodyName 还没同步, 退回本地设置里选的支援角色
                roleKey = BattleExtraItemHelper.GetSupportRoleKey();
                roleSprite = SpriteResHelper.LoadRoleIcon(roleKey);
            }

            icon.sprite = roleSprite;
            icon.enabled = roleSprite != null;
            nameText.text = roleKey.ToLocalizeStr();
            inUseMark.SetActive(isOperating);
            // SetSelected(isSelected);
        }


        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnBtnClick);
        }

        private void OnBtnClick()
        {
            onClick?.Invoke(friend);
        }
    }
}
