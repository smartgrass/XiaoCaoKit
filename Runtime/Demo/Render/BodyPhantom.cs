using DG.Tweening;
using Fantasy.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

namespace XiaoCao.Render
{
    public class BodyPhantom : MonoBehaviour
    {
        public float meshRefreshRate = 0.08f;
        public float fadeTime = 1.2f;
        public float createPreDistance = 4;
        public Material mat;

        private Coroutine _coroutine;
        private float _maxActiveTime = 10;
        private bool _isInited;
        private string MatPath = "Assets/_Res/Render/BodyGlow.mat";

        void Init()
        {
            _isInited = true;
            mat = ResMgr.LoadAseet<Material>(MatPath);
            if (mat == null)
            {
                Debug.LogError("--- no find Mat!");
            }
        }

        public void StartAnim(float time)
        {
            if (!_isInited)
            {
                Init();
            }

            _maxActiveTime = time <= 0 ? 10 : time;
            _coroutine = StartCoroutine(IEActivateTrail());
        }

        public void StopAnim()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }

        IEnumerator IEActivateTrail()
        {
            float timer = meshRefreshRate / 2f;
            Vector3 lastCreatePos = transform.position;

            while (_maxActiveTime > 0)
            {
                timer += XCTime.deltaTime;
                Vector3 currentPos = transform.position;

                // 检查是否达到创建幻影的时间或距离条件
                if (timer >= meshRefreshRate || Vector3.Distance(currentPos, lastCreatePos) >= createPreDistance)
                {
                    CreateOnPos(currentPos);
                    timer = 0f; // 重置计时器
                    lastCreatePos = currentPos; // 更新上次创建位置
                }

                _maxActiveTime -= XCTime.deltaTime;
                yield return null;
            }
        }

        private void CreateOnPos(Vector3 nextPos)
        {
            GameObject copy = Instantiate(gameObject);
            copy.GetComponent<Animator>().enabled = false;
            var renderers = copy.GetComponentsInChildren<Renderer>();
            copy.transform.SetPositionAndRotation(nextPos, transform.rotation);
            _maxActiveTime -= meshRefreshRate;
            for (int i = 0; i < renderers.Length; i++)
            {
                var render = renderers[i];
                int len = render.materials.Length;
                Material[] materials = new Material[len];
                Array.Fill(materials, mat);
                render.materials = materials;
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var newMats = render.materials;
                for (int i1 = 0; i1 < newMats.Length; i1++)
                {
                    Material mat = newMats[i1];
                    var tween = mat.DOFade(0, fadeTime);
                }
            }

            Destroy(copy, fadeTime + 0.1f);
        }

        private void OnDestroy()
        {
        }
    }
}