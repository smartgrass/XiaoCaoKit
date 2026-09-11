using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 关卡选择页中的敌人展示卡片。
    /// </summary>
    public class LevelSelectEnemyCard : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text tagText;

        /// <summary>
        /// 刷新卡片显示内容。
        /// </summary>
        public void Show(string title, string roleKey, int enemyLevel, string tag)
        {
            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(title) ? "--" : title;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv.{Mathf.Max(1, enemyLevel)}";
            }

            if (tagText != null)
            {
                tagText.text = string.IsNullOrEmpty(tag) ? "" : tag;
                tagText.gameObject.SetActive(!string.IsNullOrEmpty(tag));
            }

            if (icon != null)
            {
                icon.sprite = string.IsNullOrEmpty(roleKey) ? null : SpriteResHelper.LoadRoleIcon(roleKey);
                icon.enabled = icon.sprite != null;
            }
        }
    }
}
