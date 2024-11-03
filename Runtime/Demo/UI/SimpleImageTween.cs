using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SimpleImageTween : MonoBehaviour
    {
        public float time = 0.3f;

        public float scaleMult = 1;

        public Image image;

        public Gradient colorGradient;

        [CurveRange(0,0,1,1)]
        public AnimationCurve scaleCurve;

        private float _timer = 0f;

        private Coroutine _coroutine;

        [Button(enabledMode:EButtonEnableMode.Playmode)]
        public void Play()
        {
            if (_timer == 0)
            {
                StartCoroutine(TweenAnimation());
            }
        }
        // 协程来执行动画  
        private IEnumerator TweenAnimation()
        {
            _timer = 0;
            while (_timer < time)
            {
                float t = _timer / time;
                // 计算当前颜色和缩放  
                image.color = colorGradient.Evaluate(t);
                image.rectTransform.localScale = scaleCurve.Evaluate(t) * scaleMult * Vector2.one;
                yield return null;
                _timer += Time.deltaTime;
            }
            image.color = colorGradient.Evaluate(1);
            image.rectTransform.localScale = scaleCurve.Evaluate(1) * scaleMult * Vector2.one;
            _timer = 0;
        }

    }
}
