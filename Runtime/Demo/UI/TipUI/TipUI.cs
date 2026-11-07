using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 按键说明提示条：点击 btn 切换 TextPanel 的显隐，显示与隐藏都播放缩放动画。
    /// btn / textPanel 在 prefab 上绑定，不在代码里查找节点。
    /// </summary>
    public class TipUI : MonoBehaviour
    {
        [Label("按钮")]
        public Button btn;

        [Label("文字面板")]
        public RectTransform textPanel;

        [Label("动画时长")]
        [Tooltip("缩放动画单程时长(秒)")]
        public float duration = 0.2f;

        [Label("隐藏缩放")]
        [Tooltip("隐藏时缩到原始缩放的多少倍")]
        public float hideScale = 0.8f;

        [Label("时停中也能播放")]
        [Tooltip("勾选后动画走非缩放时间, Time.timeScale = 0(时停、暂停界面)时展开/收起动画照常播完")]
        public bool ignoreTimeScale = true;

        /// <summary>当前是否处于显示状态</summary>
        private bool _isShow = true;

        /// <summary>TextPanel 在 prefab 上的原始缩放，作为动画基准</summary>
        private Vector3 _originScale;

        private void Awake()
        {
            _originScale = textPanel.localScale;
            btn.onClick.AddListener(OnClickBtn);
        }

        private void OnEnable()
        {
            // 面板被外部关掉再打开时同步状态，避免第一次点击看起来没反应
            _isShow = textPanel.gameObject.activeSelf;
        }

        private void OnDestroy()
        {
            //补间已 SetLink 到 textPanel, 节点销毁时 DOTween 会自动回收, 这里不再手动 Kill
            btn.onClick.RemoveListener(OnClickBtn);
        }

        /// <summary>点击按钮：切换 TextPanel 的显示状态。</summary>
        private void OnClickBtn()
        {
            SetShow(!_isShow);
        }

        /// <summary>显示 / 隐藏 TextPanel，两个方向都播缩放动画。</summary>
        public void SetShow(bool isShow)
        {
            _isShow = isShow;
            textPanel.DOKill();

            if (isShow)
            {
                textPanel.gameObject.SetActive(true);
                textPanel.localScale = _originScale * hideScale;
                PlayScaleTween(_originScale, Ease.OutBack);
            }
            else
            {
                // 缩完再隐藏；SetLink 保证节点销毁时补间自动回收
                PlayScaleTween(_originScale * hideScale, Ease.InQuad)
                    .OnComplete(() => textPanel.gameObject.SetActive(false));
            }
        }

        /// <summary>
        /// 播放一次展开动画(从隐藏缩放弹到原始大小)并置为显示状态。
        /// 提示条刚被实例化挂到 Mid 层时调用，避免它"凭空出现"没有弹出效果。
        /// </summary>
        public void PlayPop()
        {
            SetShow(true);
        }

        /// <summary>
        /// 创建作用于 textPanel 的缩放补间；ignoreTimeScale 打开时走非缩放时间，
        /// 保证时停(Time.timeScale = 0)期间动画依然能播完。
        /// </summary>
        private Tween PlayScaleTween(Vector3 endScale, Ease ease)
        {
            return textPanel.DOScale(endScale, duration)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(textPanel.gameObject);
        }
    }
}
