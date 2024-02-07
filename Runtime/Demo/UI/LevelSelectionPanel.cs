using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class LevelSelectionPanel : StandardView
    {
        public int num = 5;

        public GameObject prefab;

        public CachePool<ImgBtn> cachePool;


        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();
            cachePool = new CachePool<ImgBtn>(prefab);
            cachePool.UpdateCachedAmount(num);

            for (int i = 0; i < num; i++)
            {
                int tmp = i;
                cachePool.cacheList[i].index = tmp;

                cachePool.cacheList[i].button.onClick.AddListener(() => { OnSelectButton(i); });
            }
            IsInited = true;
        }

        public override void OnCloseBtnClick()
        {
            Hide();
        }

        public override void OnSureBtnClick()
        {

        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            if (!IsInited)
            {
                Init();
            }

            gameObject.SetActive(true);
        }

        public void OnSelectButton(int index)
        {
            Debug.Log($"--- {index}");
        }
    }


}


