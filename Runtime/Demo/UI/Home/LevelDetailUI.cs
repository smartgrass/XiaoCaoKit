using cfg;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCao.UI
{
    public class LevelDetailUI : MonoBehaviour
    {
        public TMP_Text descText;
        public Button hideBtn;
        public Button enterBtn;
        public RectTransform rootRt;
        private string _levelKey;

        public void Awake()
        {
            hideBtn.onClick.AddListener(Hide);
            enterBtn.onClick.AddListener(OnEnter);
        }

        private void OnEnter()
        {
            if (string.IsNullOrEmpty(_levelKey))
            {
                return;
            }

            GameMgr.Inst.LoadLevelScene(_levelKey);
        }

        public void Show(int chapter, int level)
        {
            _levelKey = MapNames.GetLevelKey(chapter, level);
            gameObject.SetActive(true);
            //获取select Level
            LevelInfo levelInfo = MapNames.GetLevelInfoByName(_levelKey);
            descText.text = levelInfo.GetLevelName();
            // descText.text += "\n" + LocalizeKey.EnemyLevel.ToLocalizeStr() + " : " +
            //                  LubanTables.GetLevelSetting(_levelKey).EnemyBaseLevel.ToString();

            rootRt.localScale = Vector3.one * 0.2f;
            rootRt.transform.DOScale(Vector3.one, 0.3f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}