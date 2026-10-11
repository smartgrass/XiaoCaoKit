using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 角色列表格子: 显示单个可选主控角色的图标与名称, 并响应选中状态和点击。
    /// </summary>
    public class RoleCell : MonoBehaviour
    {
        //UIStateChangeColor 约定: 0 选中, 1 未选中
        private const int SelectState = 0;
        private const int UnSelectState = 1;

        //角色 key 规则: Role_ + 下标, 与角色图标和本地化表保持一致
        private const string RoleKeyPrefix = "Role_";

        public Image roleIcon;
        public TMP_Text nameText;
        public Button btn;
        public UIStateChangeColor stateColor;

        private int _roleIndex;
        private Action<int> _onClick;

        public int RoleIndex => _roleIndex;

        private void Awake()
        {
            btn.onClick.AddListener(OnBtnClick);
        }

        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnBtnClick);
        }

        /// <summary>
        /// 绑定角色下标, 刷新角色图标、名称和选中状态。
        /// </summary>
        public void SetRole(int roleIndex, bool isSelected, Action<int> onClick)
        {
            _roleIndex = roleIndex;
            _onClick = onClick;

            string roleKey = $"{RoleKeyPrefix}{roleIndex}";
            roleIcon.sprite = SpriteResHelper.LoadRoleIcon(roleKey);
            nameText.text = roleKey.ToLocalizeStr();
            stateColor.SetState(isSelected ? SelectState : UnSelectState);
        }

        private void OnBtnClick()
        {
            _onClick?.Invoke(_roleIndex);
        }
    }
}
