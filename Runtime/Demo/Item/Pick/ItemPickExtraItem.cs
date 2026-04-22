using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.Item.Pick
{
    public class ItemPickExtraItem : MonoBehaviour
    {
        [Dropdown(nameof(GetExtraItemIdDropdown))]
        [OnValueChanged(nameof(RefreshIconTexture))]
        public string itemId;

        private bool _isPicked;

        public ParticleSystem icon;

        private void Start()
        {
            RefreshIconTexture();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isPicked)
            {
                return;
            }

            if (PlayerHelper.IsLocalPlayerCollider(other, out var player))
            {
                OnPlayerEnter(player);
            }
        }

        private void OnPlayerEnter(Player0 player)
        {
            if (player != GameDataCommon.LocalPlayer)
            {
                return;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning($"--- empty extra item id {name}");
                return;
            }

            if (ConfigMgr.Inst.BattleExtraItemConfigSo?.GetConfig(itemId) == null)
            {
                Debug.LogWarning($"--- no extra item config {itemId}");
                return;
            }

            _isPicked = true;
            XiaoCao.Item item = CreateItem();
            item.RewardItem();
            Hide();
        }

        public void RefreshIconTexture()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (icon == null)
            {
                icon = FindIconParticle();
            }

            if (icon == null)
            {
                return;
            }
            
            Sprite sprite = LoadItemSprite();
            if (sprite == null)
            {
                return;
            }

            var textureSheetAnimation = icon.textureSheetAnimation;
            textureSheetAnimation.enabled = true;
            textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;

            if (textureSheetAnimation.spriteCount > 0)
            {
                textureSheetAnimation.SetSprite(0, sprite);
            }
            else
            {
                textureSheetAnimation.AddSprite(sprite);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(icon);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(icon);
            }
#endif
        }

        private XiaoCao.Item CreateItem()
        {
            return new XiaoCao.Item(XiaoCao.ItemType.Consumable, itemId);
        }

        private string[] GetExtraItemIdDropdown()
        {
            List<string> itemIds = new List<string>();
            AddItemId(itemIds, itemId);

            BattleExtraItemConfigSo config = LoadBattleExtraItemConfig();
            if (config != null && config.list != null)
            {
                foreach (BattleExtraItemSubConfig subConfig in config.list)
                {
                    if (subConfig != null)
                    {
                        AddItemId(itemIds, subConfig.id);
                    }
                }
            }

            if (itemIds.Count == 0)
            {
                itemIds.Add(string.Empty);
            }

            return itemIds.ToArray();
        }

        private void AddItemId(List<string> itemIds, string id)
        {
            if (!string.IsNullOrEmpty(id) && !itemIds.Contains(id))
            {
                itemIds.Add(id);
            }
        }

        private BattleExtraItemConfigSo LoadBattleExtraItemConfig()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<BattleExtraItemConfigSo>(
                    "Assets/XiaoCaoKit/Resources/BattleExtraItemConfigSo.asset");
            }
#endif

            BattleExtraItemConfigSo config = ConfigMgr.Inst.BattleExtraItemConfigSo;
            if (config != null)
            {
                return config;
            }

            return Resources.Load<BattleExtraItemConfigSo>("BattleExtraItemConfigSo");
        }

        private Sprite LoadItemSprite()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = $"Assets/_Res/Sprite/ItemIcon/{itemId}.png";
                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }

                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SpriteResHelper.DefaultItemSpritePath);
            }
#endif

            return CreateItem().GetItemSprite();
        }

        private ParticleSystem FindIconParticle()
        {
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "icon")
                {
                    return child.GetComponent<ParticleSystem>();
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                RefreshIconTexture();
            }
        }
#endif

        private void Hide()
        {
            transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => { Destroy(gameObject); });
            string path = "Assets/_Res/Audio/Effect/heal.mp3";
            AudioClip audioClip = ResMgr.LoadAseet(path) as AudioClip;
            SoundMgr.Inst.PlayClip(audioClip);
        }
    }
}
