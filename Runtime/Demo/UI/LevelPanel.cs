using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class LevelPanel : StandardPanel<UIData0>
    {
        public int num = 5;

        public GameObject levelPrefab;

        public CachePool<ImgBtn> cachePool;

        public Transform content;

        public Transform levelsParent;

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();
            cachePool = new CachePool<ImgBtn>(levelPrefab, num);

            for (int i = 0; i < num; i++)
            {
                int tmp = i;
                cachePool.cacheList[i].index = tmp;

                cachePool.cacheList[i].transform.SetParent(levelsParent);

                cachePool.cacheList[i].button.onClick.AddListener(() => { OnSelectButton(i); });
            }
            IsInited = true;
        }


        public void LoadData()
        {
            //图标,标题,难度,目标关卡->So文件

            //LevelSettingHelper.

            //LubanTables.Inst.
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


