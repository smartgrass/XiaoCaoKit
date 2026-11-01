using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 炮台瞄准准星: 屏幕正中的四段短线, 开火时向外张开再收回。
    /// 只在玩家操控炮台期间显示, 由 <see cref="XiaoCao.Turret"/> 开关。
    /// </summary>
    public class TurretCrosshair : MonoBehaviour
    {
        [Header("节点绑定")]
        [Tooltip("准星上方的短线")]
        public RectTransform up;

        [Tooltip("准星下方的短线")]
        public RectTransform down;

        [Tooltip("准星左侧的短线")]
        public RectTransform left;

        [Tooltip("准星右侧的短线")]
        public RectTransform right;

        [Header("开火反馈")]
        [Tooltip("开火时向外张开的距离(像素)")]
        public float pulseDistance = 10f;

        [Tooltip("张开后的回收速度(每秒收回多少比例)")]
        public float pulseRecoverSpeed = 8f;

        private Vector2 upBase;
        private Vector2 downBase;
        private Vector2 leftBase;
        private Vector2 rightBase;
        private float pulse;

        private void Awake()
        {
            upBase = up.anchoredPosition;
            downBase = down.anchoredPosition;
            leftBase = left.anchoredPosition;
            rightBase = right.anchoredPosition;
        }

        /// <summary>显示准星</summary>
        public void Show()
        {
            gameObject.SetActive(true);

            pulse = 0f;
            ApplyOffset(0f);
        }

        /// <summary>隐藏准星</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>开火时张开一次, 随后的 Update 里自动收回</summary>
        public void PlayFirePulse()
        {
            pulse = 1f;
        }

        private void Update()
        {
            if (pulse <= 0f)
            {
                return;
            }

            pulse = Mathf.Max(0f, pulse - pulseRecoverSpeed * Time.unscaledDeltaTime);
            ApplyOffset(pulse * pulseDistance);
        }

        private void ApplyOffset(float offset)
        {
            up.anchoredPosition = upBase + Vector2.up * offset;
            down.anchoredPosition = downBase + Vector2.down * offset;
            left.anchoredPosition = leftBase + Vector2.left * offset;
            right.anchoredPosition = rightBase + Vector2.right * offset;
        }
    }
}
