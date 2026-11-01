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
            if (image == null)
            {
                return;
            }

            //节点未激活(技能栏槽位被隐藏)时 StartCoroutine 会抛
            //"Coroutine couldn't be started because the game object xxx is inactive",
            //这里直接跳过, 等下次激活后再播, 不影响冷却逻辑本身
            if (!isActiveAndEnabled)
            {
                _coroutine = null;
                return;
            }

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
            if (_coroutine == null)
            {
                return;
            }

            StopCoroutine(_coroutine);
            _coroutine = null;

            if (image != null)
            {
                image.color = image.color.SetAlpha(0);
            }
        }

        /// <summary>
        /// 节点被隐藏时 Unity 会停掉协程但不清引用, 这里同步清掉,
        /// 否则 isLoop 模式会误以为动画还在跑而不再启动
        /// </summary>
        private void OnDisable()
        {
            _coroutine = null;
        }

        // 协程来执行动画  
        private IEnumerator TweenAnimation()
        {
            //拖动/重建 UI 时可能已经丢了引用, 直接结束协程而不是每帧空引用
            if (image == null)
            {
                _coroutine = null;
                yield break;
            }

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
