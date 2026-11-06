using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 全屏黑幕: 切场景前用 ShowBlack 盖住画面, 场景就绪后再 FadeOutAnim 淡出。
    /// 以 DontDestroyOnLoad 常驻, 跨场景保留, 保证新场景首帧就被遮住。
    /// </summary>
    public class BlackScreenUI : MonoBehaviour
    {
        public CanvasGroup canvasGroup;

        public float fadeTime = 0.8f;

        //低于该透明度肉眼已经看不出, 视作"没有黑幕"
        private const float AlphaEpsilon = 0.001f;

        private Coroutine fadeCoroutine;

        /// <summary>
        /// 立刻盖满黑屏(不淡入), 用于切场景前遮挡画面
        /// </summary>
        public void ShowBlack()
        {
            StopFade();
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 从当前透明度淡出到全透明; 本来就是透明的则直接返回,
        /// 避免先强制置黑再淡出(表现为"先闪一下黑")
        /// </summary>
        public void FadeOutAnim()
        {
            StopFade();

            float startAlpha = canvasGroup.alpha;
            if (startAlpha <= AlphaEpsilon)
            {
                return;
            }

            fadeCoroutine = StartCoroutine(IEFadeOutAnim(startAlpha));
        }

        //中断尚未结束的淡出, 防止多次切换叠加出多个协程
        private void StopFade()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private IEnumerator IEFadeOutAnim(float startAlpha)
        {
            yield return Fade(canvasGroup, startAlpha, 0f, fadeTime);
            fadeCoroutine = null;
        }

        private IEnumerator Fade(CanvasGroup cGroup, float startAlpha, float endAlpha, float fadeDuration)
        {
            if (fadeDuration <= 0f)
            {
                cGroup.alpha = endAlpha;
                yield break;
            }

            //用非缩放时间, 避免顿帧/暂停改 timeScale 时淡出被拖慢或卡住
            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                cGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(time / fadeDuration));
                yield return null;
            }

            cGroup.alpha = endAlpha;
        }
    }

    public static class BlackScreen
    {
        private static bool _isLoaded = false;

        private static BlackScreenUI _blackScreenUI;

        private static void Prepare()
        {
            if (!_isLoaded)
            {
                GameObject instance = MonoBehaviour.Instantiate(Resources.Load<GameObject>("BlackScreenUI"));
                instance.name = "[ BlackScreenUI ]";
                _blackScreenUI = instance.GetComponent<BlackScreenUI>();
                Object.DontDestroyOnLoad(instance);
                _isLoaded = true;
            }
        }

        public static void ShowBlack()
        {
            Prepare();
            _blackScreenUI.ShowBlack();
        }

        public static void FadeOutAnim()
        {
            Prepare();
            _blackScreenUI.FadeOutAnim();
        }
    }
}
