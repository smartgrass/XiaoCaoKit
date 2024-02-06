using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class LevelSelectionView : StandardView
    {
        public int num = 5;

        public GameObject prefab;

        public CachePool<ImgBtn> cachePool;

        public override void Init()
        {
            base.Init();

            cachePool = new CachePool<ImgBtn>(prefab);
            cachePool.UpdateCachedAmount(num);

            for (int i = 0; i < num; i++)
            {
                int tmp = i;
                cachePool.cacheList[i].index = tmp;

                cachePool.cacheList[i].button.onClick.AddListener(() => { OnSelectButton(i); });
            }
        }

        public override void OnCloseBtnClick()
        {
            Hide();
        }

        public override void OnSureBtnClick()
        {
            Show();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            gameObject.SetActive(true);
        }

        public void OnSelectButton(int index)
        {
            Debug.Log($"--- {index}");
        }
    }


}


