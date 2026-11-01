using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 炮台界面里的支援角色格子: 显示头像/名字, 并标记该角色是否正在操控本炮台
    /// </summary>
    public class TurretFriendCell : MonoBehaviour
    {
        public Button btn;

        public Image icon;

        public TMP_Text nameText;

        [Tooltip("正在操控本炮台的标记")]
        public GameObject inUseMark;

        private Role friend;
        private Action<Role> onClick;
        private bool hasListener;

        /// <summary>绑定支援角色并刷新显示</summary>
        public void Init(Role role, bool isOperating, Action<Role> onClickAction)
        {
            friend = role;
            onClick = onClickAction;

            if (!hasListener)
            {
                hasListener = true;
                btn.onClick.AddListener(OnBtnClick);
            }

            //支援角色的 bodyName 就是 Role_x(召唤时由 GameAllData 同步), 与角色图标和本地化表一致
            string roleKey = role.idRole.bodyName;
            Sprite roleSprite = string.IsNullOrEmpty(roleKey) ? null : SpriteResHelper.LoadRoleIcon(roleKey);

            if (roleSprite == null)
            {
                //老存档里的支援角色 bodyName 还没同步, 退回本地设置里选的支援角色
                roleKey = BattleExtraItemHelper.GetSupportRoleKey();
                roleSprite = SpriteResHelper.LoadRoleIcon(roleKey);
            }

            icon.sprite = roleSprite;
            icon.enabled = roleSprite != null;
            nameText.text = roleKey.ToLocalizeStr();
            inUseMark.SetActive(isOperating);
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
