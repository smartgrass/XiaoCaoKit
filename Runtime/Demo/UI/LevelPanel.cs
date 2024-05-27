using cfg;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using XiaoCao.FancyScrollRect;
using Image = UnityEngine.UI.Image;

namespace XiaoCao
{
    public class LevelPanel : StandardPanel<UIData0>
    {
        public int num = 5;

        [SerializeField] XCScrollView scrollView = default;

        //public GameObject levelPrefab;

        //public CachePool<ImgTextBtn> cachePool;

        public Transform content;

        public Transform levelsParent;

        public Image mainImage;

        private int CurLevel;

        private int CurChapter = 1;

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();

            //cachePool = new CachePool<ImgTextBtn>(levelPrefab, num);

            //for (int i = 0; i < num; i++)
            //{
            //    int tmp = i;
            //    cachePool.cacheList[i].index = tmp;

            //    cachePool.cacheList[i].transform.SetParent(levelsParent);

            //    cachePool.cacheList[i].button.onClick.AddListener(() => { OnSelectButton(i); });
            //}

            scrollView.OnCellClicked(OnSelectButton);

            var items = Enumerable.Range(0, num).Select(i => new ItemData($"Cell {i}")).ToArray();
            scrollView.UpdateData(items);
            IsInited = true;
        }

        //public void UpdateData(IList<ItemData> items)
        //{
        //    base.UpdateContents(items);
        //    scroller.SetTotalCount(items.Count);
        //}

        public void LoadData()
        {
            //图标,标题,难度,目标关卡->So文件
            ChapterSetting chapterSetting = LubanTables.GetChapterSetting(1);

            int count = chapterSetting.Levels.Count;

            //cachePool.UpdateCachedAmount(count);
            //for (int i = 0;i < count;i++)
            //{
            //    cachePool.cacheList[i].text.text = LevelSettingHelper.GetText(chapterSetting.Levels[i]);
            //}
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
            ChapterSetting chapterSetting = LubanTables.GetChapterSetting(1);
            CurLevel = chapterSetting.Levels[index];
            var levelSetting = LubanTables.GetLevelSetting(CurLevel);
            //TODO 加载图片
            string spritePath = XCPathConfig.GetLevelImage(CurLevel);
            mainImage.sprite = ResMgr.LoadAseet(spritePath) as Sprite;
            Debug.Log($"--- {index} {CurLevel}");
        }

    }


}


