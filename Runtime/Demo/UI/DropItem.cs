using System;
using System.Collections;
using UnityEngine;

namespace XiaoCao
{
    
    [Obsolete("用不上了")]
    public class DropItem : GameStartMono
    {
        /*
        public Item item;
        public float attractDistance = 2.5f; // 吸附距离
        public float flyDuration = 0.5f;     // 飞行时间
        public AnimationCurve flyCurve;      // 抛物线曲线，可在Inspector调整
        public MeshRenderer visualRenderer;

        private bool isFlying = false;

        public override void OnGameStart()
        {
            base.OnGameStart();
            if (flyCurve == null)
            {
                // 默认抛物线
                flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            Show3DItem();
        }

        void Show3DItem()
        {
            var sprite = item.GetItemSprite();
            visualRenderer.material.mainTexture = SpriteToTexture(sprite);
        }

        // Sprite转Texture2D
        Texture2D SpriteToTexture(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            if (sprite.rect.width != sprite.texture.width || sprite.rect.height != sprite.texture.height)
            {
                // 裁剪出Sprite区域
                Texture2D newTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
                Color[] pixels = sprite.texture.GetPixels(
                    (int)sprite.textureRect.x,
                    (int)sprite.textureRect.y,
                    (int)sprite.textureRect.width,
                    (int)sprite.textureRect.height);
                newTex.SetPixels(pixels);
                newTex.Apply();
                return newTex;
            }
            else
            {
                return sprite.texture;
            }
        }

        void Update()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
            {
                return;
            }

            // 只旋转Y轴可用 LookAt + Vector3.up, 若需完全朝向则直接LookAt
            visualRenderer.transform.LookAt(Camera.main.transform.position, Vector3.up);

            if (isFlying) return;

            Transform playerTf = GameDataCommon.Current.player0.transform;

            float dist = Vector3.Distance(transform.position, playerTf.position);
            if (dist < attractDistance)
            {
                StartCoroutine(FlyToPlayer());
            }
        }

        IEnumerator FlyToPlayer()
        {
            isFlying = true;
            Vector3 start = transform.position;
            Transform playerTf = GameDataCommon.Current.player0.transform;
            float height = 1.5f; // 抛物线高度

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / flyDuration;
                Vector3 pos = Vector3.Lerp(start, playerTf.position, t);
                // 抛物线高度
                float yOffset = flyCurve.Evaluate(t) * height;
                pos.y += yOffset;
                transform.position = pos;
                yield return null;
            }

            // 到达玩家，加入背包
            AddToPlayerInventory();
            Destroy(gameObject);
        }

        void AddToPlayerInventory()
        {
            PlayerSaveData.LocalSavaData.inventory.AddItem(item.type, item.id, item.num);
        }
        */
    }
}