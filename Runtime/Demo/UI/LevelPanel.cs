using cfg;
using System.Linq;
using UnityEngine;
using XiaoCao.FancyScrollRect;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace XiaoCao
{
    public class LevelPanel : StandardPanel<UIData0>
    {
        public override UIPanelType panelType => UIPanelType.LevelPanel;

        public int num = 5;

        public Button sureBtn;

        [SerializeField] XCScrollView scrollView = default;

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

            sureBtn.onClick.AddListener(OnSureBtnClick);
            scrollView.OnCellClicked(OnSelectButton);

            var items = Enumerable.Range(0, num).Select(i => new ItemData($"Cell {i}")).ToArray();
            scrollView.UpdateData(items);
            
            IsInited = true;
        }


        public void LoadData()
        {
            //图标,标题,难度,目标关卡->So文件
            ChapterSetting chapterSetting = LubanTables.GetChapterSetting(1);

            int count = chapterSetting.Levels.Count;
        }


        public override void OnCloseBtnClick()
        {
            Hide();
        }

        public void OnSureBtnClick()
        {

        }

        public override void Hide()
        {
            gameObject.SetActive(false);
            UIMgr.Inst.HideView(panelType);
        }

        public override void Show()
        {
            if (!IsInited)
            {
                Init();
            }
            IsShowing = true;
            gameObject.SetActive(true);
        }

        public void OnSelectButton(int index)
        {
            ChapterSetting chapterSetting = LubanTables.GetChapterSetting(1);
            CurLevel = chapterSetting.Levels[index];
            string levelName = $"level{CurLevel}";
            var levelSetting = LubanTables.GetLevelSetting(levelName);
            //TODO 加载图片
            string spritePath = XCPathConfig.GetLevelImage(CurLevel);
            mainImage.sprite = ResMgr.LoadAseet(spritePath) as Sprite;
            Debug.Log($"--- {index} {CurLevel}");
        }

    }


}


