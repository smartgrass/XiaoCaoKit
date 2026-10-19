using TEngine;
using TMPro;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.UI
{
    /// <summary>
    /// 金币条显示:监听金币变化刷新数量,并按数字宽度自动拉伸文字背景
    /// </summary>
    public class CoinBarUI : MonoBehaviour
    {
        public ItemCell coinCell;

        [Tooltip("数字背景,随位数向右拉伸")]
        public RectTransform textBg;

        [Header("布局间距")]
        [Tooltip("数字文本右边缘到背景右边缘的距离")]
        public float rightPadding = 36.5f;

        [Tooltip("金币图标与数字之间的最小间距")]
        public float iconTextGap = 2.5f;

        //背景左边缘(相对金币条左上角),固定不动,只向右扩展
        private float _bgLeft;

        //背景原始宽度,数字较少时保持这个宽度,避免数字变短时背景抖动
        private float _minBgWidth;

        private void Awake()
        {
            _bgLeft = textBg.anchoredPosition.x - textBg.sizeDelta.x * textBg.pivot.x;
            _minBgWidth = textBg.sizeDelta.x;
        }

        private void OnEnable()
        {
            GameEvent.AddEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);
            OnCoinChange(0);
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);
        }

        /// <summary>
        /// 金币数量变化:刷新数量文本并重新适配背景宽度
        /// </summary>
        private void OnCoinChange(int type)
        {
            coinCell.SetNum(PlayerSaveData.LocalSavaData.Coin);
            FitTextBg();
        }

        /// <summary>
        /// 按数字文本宽度拉伸背景:数字位数变多时背景向右变长,文本右边缘跟着背景内边距一起右移
        /// </summary>
        private void FitTextBg()
        {
            TextMeshProUGUI numText = coinCell.text;
            float textWidth = numText.GetPreferredValues(numText.text).x;

            //宽度 = 图标到背景左边缘的距离 + 图标文字间距 + 文本宽度 + 右侧内边距
            float bgWidth = Mathf.Max(_minBgWidth,
                GetIconRight() - _bgLeft + iconTextGap + textWidth + rightPadding);
            textBg.sizeDelta = new Vector2(bgWidth, textBg.sizeDelta.y);

            //文本保持右对齐,右边缘贴在背景右边缘的内侧
            Vector2 textPos = numText.rectTransform.anchoredPosition;
            textPos.x = _bgLeft + bgWidth - rightPadding;
            numText.rectTransform.anchoredPosition = textPos;
        }

        /// <summary>
        /// 金币图标右边缘在金币条内的横坐标
        /// </summary>
        private float GetIconRight()
        {
            RectTransform iconRt = coinCell.icon.rectTransform;
            return iconRt.anchoredPosition.x + iconRt.sizeDelta.x * (1f - iconRt.pivot.x);
        }
    }
}
