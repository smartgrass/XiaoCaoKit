using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 漂浮对话气泡: 普通屏幕空间 UI, 位置就按预制体摆好的来, 不做位移, 只做透明度淡入淡出。
    /// <para>实例常驻在 UIMgr 预制体里(UIMgr.floatTalkPanel), 不动态加载; 显示时只 SetActive + 改透明度。</para>
    /// <para>结构: 根(全屏拉伸 + CanvasGroup) > rect > speakerImg(头像) / dialoguePanel(底框, 带 ControlUISizeByTextWidth) > dialogueText。</para>
    /// </summary>
    public class FloatTalkPanel : MonoBehaviour
    {
        /// <summary>头像截图缓存用的标签, 和 TalkPanel 分开展, 避免互相覆盖</summary>
        private const string AvatarCacheTag = "FloatTalkPanel";

        [Header("UI 组件")]
        [Tooltip("说话人头像")]
        public RawImage speakerImg;

        [Tooltip("对话框底框, 宽度由 ControlUISizeByTextWidth 跟着文字撑开")]
        public GameObject dialoguePanel;

        public TextMeshProUGUI dialogueText;
        public CanvasGroup canvasGroup;

        [Header("头像")]
        [Tooltip("按角色名自动截一张头像; 关掉就沿用预制体上的贴图")]
        public bool useSpeakerAvatar = true;

        private Tween tween;

        #region 对外接口

        /// <summary>
        /// 显示一句对话气泡。位置固定(预制体的位置), 只做透明度动画。
        /// </summary>
        /// <param name="roleName">角色标识, 只用来取头像: 场景节点名(npc01) / 角色模型名(Role_1) / ShowAct 标记名</param>
        /// <param name="talkKey">本地化 key</param>
        /// <param name="duration">完全显示后的停留时间(秒), 不含淡入淡出</param>
        /// <param name="fadeIn">淡入时长(秒), 0 = 直接出现</param>
        /// <param name="fadeOut">淡出时长(秒), 0 = 到点直接消失</param>
        public static void ShowTalk(string roleName, string talkKey, float duration, float fadeIn = 0.2f,
            float fadeOut = 0.3f)
        {
            if (string.IsNullOrEmpty(talkKey))
            {
                Debug.LogWarning($"--- FloatTalkPanel 对话 key 为空, 角色 {roleName} 不显示气泡");
                return;
            }

            FloatTalkPanel panel = GetPanel();
            if (panel == null)
            {
                return;
            }

            panel.Play(roleName, talkKey.ToLocalizeStr(), Mathf.Max(0f, duration), Mathf.Max(0f, fadeIn),
                Mathf.Max(0f, fadeOut));
        }

        /// <summary>立刻收起气泡</summary>
        public static void Hide()
        {
            FloatTalkPanel panel = GetPanel();
            if (panel != null)
            {
                panel.HideSelf();
            }
        }

        #endregion

        /// <summary>取 UIMgr 上的常驻实例; UIMgr 没起来(编辑器工具场景)时退回场景里自己找</summary>
        private static FloatTalkPanel GetPanel()
        {
            if (UIMgr.HasInst && UIMgr.Inst.floatTalkPanel != null)
            {
                return UIMgr.Inst.floatTalkPanel;
            }

            return FindFirstObjectByType<FloatTalkPanel>(FindObjectsInactive.Include);
        }

        /// <summary>UIMgr 初始化时调: 开局先隐藏</summary>
        public void Init()
        {
            HideSelf();
        }

        #region 显示逻辑

        private void Play(string roleName, string content, float duration, float fadeIn, float fadeOut)
        {
            tween?.Kill();
            tween = null;

            gameObject.SetActive(true);
            //位置固定在预制体摆好的地方, 这里只保证盖在同类 UI 上面, 不碰任何坐标
            transform.SetAsLastSibling();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            UpdateContent(roleName, content);

            canvasGroup.alpha = fadeIn > 0f ? 0f : 1f;
            if (fadeIn <= 0f && duration <= 0f && fadeOut <= 0f)
            {
                //三个时长都是 0: 一直显示, 等 Hide 收
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (fadeIn > 0f)
            {
                sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1f, fadeIn));
            }

            if (duration > 0f)
            {
                sequence.AppendInterval(duration);
            }

            if (fadeOut > 0f)
            {
                sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0f, fadeOut));
            }

            sequence.OnComplete(HideSelf);
            tween = sequence;
        }

        private void UpdateContent(string roleName, string content)
        {
            dialogueText.text = content;

            //ControlUISizeByTextWidth 是每帧 LateUpdate 刷新的, 手动刷一次让第一帧就是正确宽度
            ControlUISizeByTextWidth sizer = dialoguePanel.GetComponent<ControlUISizeByTextWidth>();
            //文本节点的宽度就是气泡的排版宽度, 长句在这里换行;
            //不设上限时底框会按"不换行"的自然宽度撑开, 一句长台词能把气泡拉满屏
            float layoutWidth = dialogueText.rectTransform.rect.width;
            if (sizer.maxWidth <= 0f && layoutWidth > 0f)
            {
                sizer.maxWidth = layoutWidth + sizer.extraWidth;
            }

            sizer.Refresh();

            UpdateSpeakerAvatar(roleName);
        }

        /// <summary>
        /// 头像: 按角色名截一张, 取不到就沿用预制体上的贴图。
        /// CharacterCaptureManager 内部有缓存, 同一个角色第二次说话不会重新渲染。
        /// </summary>
        private void UpdateSpeakerAvatar(string roleName)
        {
            //IsOn 判断不触发创建: 没有截图管理器(比如编辑器工具场景)就沿用预制体贴图
            if (!useSpeakerAvatar || string.IsNullOrEmpty(roleName) || !CharacterCaptureManager.IsOn)
            {
                return;
            }

            Texture avatar = CharacterCaptureManager.Inst.GetSpeakerAvatar(roleName, AvatarCacheTag);
            if (avatar != null)
            {
                speakerImg.texture = avatar;
                speakerImg.enabled = true;
            }
        }

        /// <summary>收起: 停动画、透明度归零、关节点</summary>
        private void HideSelf()
        {
            tween?.Kill();
            tween = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        private void Awake()
        {
            //纯展示, 不参与点击; 挂在 CanvasGroup 上比逐个改 Graphic 省事
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void OnDestroy()
        {
            tween?.Kill();
            tween = null;
        }
    }
}
