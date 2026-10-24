using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    /// <summary>负责 Home 触发器名字牌的查找、创建和文本缓存。</summary>
    public sealed class HomeTriggerNameText
    {
        private const string RootName = "Canvas_Talk";
        private readonly Transform owner;
        private GameObject root;
        private TMP_Text text;
        private Image bgImage;
        public string uiKey;

        const float ShowAlpha = 170 / 255f;
        const float HideAlpha = 45 / 255f;

        /// <summary>创建名字牌管理器。</summary>
        public HomeTriggerNameText(Transform owner, string uiKey)
        {
            this.owner = owner;
            this.uiKey = uiKey;
        }

        /// <summary>名字牌根节点。</summary>
        public GameObject Root => root;

        /// <summary>名字牌文本组件。</summary>
        public TMP_Text Text => text;

        /// <summary>确保名字牌节点存在并缓存文本组件。</summary>
        public void Ensure(float offsetY)
        {
            if (root && text)
            {
                return;
            }

            if (!root)
            {
                root = FindRoot();
                if (!root)
                {
                    var prefab = Resources.Load<GameObject>(RootName);
                    if (!prefab)
                    {
                        return;
                    }

                    root = Object.Instantiate(prefab, owner);
                    root.name = RootName;
                    root.transform.localPosition = Vector3.up * offsetY;
                }
            }

            if (root)
            {
                text = root.GetComponentInChildren<TMP_Text>();
                bgImage = root.GetComponentInChildren<Image>();
                HideUI();
                text.text = uiKey.ToLocalizeStr();
            }
        }

        public void ShowUI()
        {
            bgImage.color = bgImage.color.SetAlpha(ShowAlpha);
        }

        public void HideUI()
        {
            bgImage.color = bgImage.color.SetAlpha(HideAlpha);
        }

        /// <summary>
        /// 查找名字牌节点: 同名的 Canvas_Talk 可能被复制出多份, 优先取已激活的那份,
        /// 否则文本会写到隐藏节点上, 显示中的那份仍是模板默认文案
        /// </summary>
        private GameObject FindRoot()
        {
            GameObject first = null;
            for (int i = 0; i < owner.childCount; i++)
            {
                Transform child = owner.GetChild(i);
                if (child.name != RootName)
                {
                    continue;
                }

                if (child.gameObject.activeSelf)
                {
                    return child.gameObject;
                }

                first ??= child.gameObject;
            }

            return first;
        }
    }
}