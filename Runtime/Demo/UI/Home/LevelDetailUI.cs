using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit
{
    public class LevelDetailUI : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public TMP_Text titleText;
        public Button hideBtn;
        public RectTransform rootRt;
        public Vector2 moveX;

        
        public void Awake()
        {
            hideBtn.onClick.AddListener(Hide);
        }
        public void Show()
        {
            //获取select Level
            LevelInfo levelInfo = MapNames.GetLevelInfoByName(GameDataCommon.Current.selectLevel);
            titleText.text = $"{levelInfo.GetIndexStr()}\n{GameDataCommon.Current.selectLevel.ToLocalizeStr()}";
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            rootRt.DOUIMoveX(moveX.x, 0.2f);
        }

        public void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            rootRt.DOUIMoveX(moveX.y, 0.2f);
        }
    }
}