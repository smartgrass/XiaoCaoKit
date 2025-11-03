using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace EasyUI.Helpers
{
    public class ToastUI : MonoBehaviour
    {
        [Header("UI References :")] [SerializeField]
        private CanvasGroup uiCanvasGroup;

        [SerializeField] private VerticalLayoutGroup uiContentVerticalLayoutGroup;

        [SerializeField] private TMP_Text uiText;

        [Header("Toast Fade In/Out Duration :")] [Range(.1f, .8f)] [SerializeField]
        private float fadeDuration = .3f;

        public Color[] textColors;


        private int maxTextLength = 300;


        void Awake()
        {
            uiCanvasGroup.alpha = 0f;
        }


        public void Show(string text, float duration, TextAnchor alignment, EToastType type)
        {
            uiText.text = (text.Length > maxTextLength) ? text.Substring(0, maxTextLength) + "..." : text;
            uiText.color = textColors[(int)type];
            uiContentVerticalLayoutGroup.childAlignment = (TextAnchor)((int)alignment);

            Dismiss();
            StartCoroutine(FadeInOut(duration, fadeDuration));
        }

        private IEnumerator FadeInOut(float toastDuration, float fadeDuration)
        {
            yield return null;
            uiContentVerticalLayoutGroup.CalculateLayoutInputHorizontal();
            uiContentVerticalLayoutGroup.CalculateLayoutInputVertical();
            uiContentVerticalLayoutGroup.SetLayoutHorizontal();
            uiContentVerticalLayoutGroup.SetLayoutVertical();
            yield return null;
            // Anim start
            yield return Fade(uiCanvasGroup, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(toastDuration);
            yield return Fade(uiCanvasGroup, 1f, 0f, fadeDuration);
            // Anim end
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

        public void Dismiss()
        {
            StopAllCoroutines();
            uiCanvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            EasyUI.Toast.Toast.IsLoaded = false;
        }
    }

    public enum EToastType
    {
        Normal, //Yellow
    }
}