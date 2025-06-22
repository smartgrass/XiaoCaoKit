using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    //TODO 最好用gameobject激活控制
    public class SimpleImageTween : MonoBehaviour
    {
        public float time = 0.3f;

        public float scaleMult = 1;

        public Image image;

        public Gradient colorGradient;

        public bool isLoop;

        [CurveRange(0, 0, 1, 1)]
        public AnimationCurve scaleCurve;

        private Coroutine _coroutine;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public void Play()
        {
            if (isLoop && _coroutine == null)
            {
                _coroutine = StartCoroutine(TweenAnimation());
            }
            else
            {
                Stop();
                _coroutine = StartCoroutine(TweenAnimation());
            }
        }


        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public void Stop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
                image.color = image.color.SetAlpha(0);
            }
        }

        // 协程来执行动画  
        private IEnumerator TweenAnimation()
        {
            float _timer = 0;
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

            if (isLoop)
            {
                _coroutine = null;
                Play();
            }
        }

    }
}
