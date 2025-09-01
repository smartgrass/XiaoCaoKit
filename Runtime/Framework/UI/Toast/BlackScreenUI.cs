using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class BlackScreenUI : MonoBehaviour
    {
        public CanvasGroup canvasGroup;

        public float fadeTime = 0.8f;

        public void ShowBlack()
        {
            canvasGroup.alpha = 1f;
        }

        public void FadeOutAnim()
        {
            StartCoroutine(IEFadeOutAnim());
        }

        private IEnumerator IEFadeOutAnim()
        {
            yield return Fade(canvasGroup, 1f, 0f, fadeTime);
        }

        private IEnumerator Fade(CanvasGroup cGroup, float startAlpha, float endAlpha, float fadeDuration)
        {
            float startTime = Time.time;
            float alpha = startAlpha;

            if (fadeDuration > 0f)
            {
                //Anim start
                while (alpha != endAlpha)
                {
                    alpha = Mathf.Lerp(startAlpha, endAlpha, (Time.time - startTime) / fadeDuration);
                    cGroup.alpha = alpha;

                    yield return null;
                }
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