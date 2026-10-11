using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 单个 buff 的倒计时显示: fieldImg1/fieldImg2 填充进度为剩余进度, maskImg 填充进度为已消耗进度。
    /// </summary>
    public class BuffStateUI : MonoBehaviour
    {
        public Image img;

        public Image fieldImg1;

        public Image fieldImg2;

        public Image maskImg;

        /// <summary>
        /// 按 buff 名加载并刷新图标。
        /// </summary>
        public void SetBuffIcon(string buffName)
        {
            img.sprite = SpriteResHelper.LoadBuffStateUIIcon(buffName);
        }

        /// <summary>
        /// 刷新填充进度, remainRatio 为 buff 剩余进度(0~1)。
        /// </summary>
        public void SetRemainRatio(float remainRatio)
        {
            remainRatio = Mathf.Clamp01(remainRatio);
            fieldImg1.fillAmount = remainRatio;
            fieldImg2.fillAmount = remainRatio;
            maskImg.fillAmount = 1 - remainRatio;
        }
    }
}
