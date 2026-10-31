using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 经验条填充进度的表现方式。
    /// </summary>
    public enum ExpBarFillMode
    {
        /// <summary>改 Fill 节点的 RectTransform 宽度, Sliced/Simple 的九宫格图也能保住两侧圆角</summary>
        Width = 0,

        /// <summary>改 Image.fillAmount, 要求图片的 Image Type 是 Filled</summary>
        FillAmount = 1,
    }

    /// <summary>
    /// 经验条控件: 负责填充比例与"当前/需求"数值显示, 并提供同一等级内的增长动画。
    /// 跨级表现由调用方用 Sequence 串接多次 <see cref="PlayExp"/> 完成。
    /// </summary>
    public class ExpBar : MonoBehaviour
    {
        public Image fillImage;

        [Tooltip("进度表现方式: Width 改 RectTransform 宽度(默认); FillAmount 改 Image.fillAmount, 需要图是 Filled 类型")]
        public ExpBarFillMode fillMode = ExpBarFillMode.Width;

        [Tooltip("进度数值, 显示为 当前经验/升级所需经验")]
        public TMP_Text numText;

        [Tooltip("等级文本, 羁绊经验条这类没有单独等级文本的可以留空")]
        public TMP_Text levelText;

        private void Awake()
        {
            if (fillMode == ExpBarFillMode.Width)
            {
                UITool.NormalizeFillRect(fillImage);
            }
        }

        /// <summary>
        /// 立即刷新经验条; need 为 0 表示已满级, 条填满且只显示"已满级"。
        /// </summary>
        public void SetExp(int exp, int need)
        {
            bool hasNext = need > 0;
            SetProgress(hasNext ? exp / (float)need : 1f, hasNext ? $"{exp}/{need}" : LocalizeKey.MaxLevel.ToLocalizeStr());
        }

        /// <summary>
        /// 按比例与自定义文本刷新, 供任务进度这类非经验语义的进度条复用。
        /// </summary>
        public void SetProgress(float ratio, string numStr)
        {
            SetFill(Mathf.Clamp01(ratio));
            numText.text = numStr;
        }

        /// <summary>
        /// 刷新经验条上的等级文本, 文本格式取自本地化 key。
        /// </summary>
        public void SetLevel(int level, string levelKey)
        {
            if (levelText == null)
            {
                return;
            }

            levelText.text = LocalizeKey.FormatWithArgs(levelKey, level);
        }

        /// <summary>
        /// 生成"同一等级内经验从 fromExp 涨到 toExp"的补间, 供调用方 Append 到 Sequence 中播放。
        /// </summary>
        public Tween PlayExp(int fromExp, int toExp, int need, float duration)
        {
            int curExp = fromExp;
            return DOTween.To(
                    () => (float)curExp,
                    value =>
                    {
                        curExp = Mathf.RoundToInt(value);
                        SetExp(curExp, need);
                    },
                    toExp, duration)
                .SetEase(Ease.Linear)
                .SetTarget(this);
        }

        /// <summary>
        /// 按比例刷新填充量, 实际表现由 <see cref="fillMode"/> 决定。
        /// </summary>
        private void SetFill(float ratio)
        {
            if (fillMode == ExpBarFillMode.FillAmount)
            {
                fillImage.fillAmount = ratio;
                return;
            }

            //宽度模式: 左锚点固定, 用 anchorMax.x 表达进度, 宽度自动跟随父级
            UITool.SetFillWidth(fillImage, ratio);
        }
    }
}
