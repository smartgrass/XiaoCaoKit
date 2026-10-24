using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

namespace XiaoCao.Render
{
    public class BodyPhantom : MonoBehaviour
    {
        /// <summary>间隔值的下限,避免技能配置成 0 时单帧狂刷幻影</summary>
        private const float MinInterval = 0.01f;

        /// <summary>未使用的那一路间隔统一设成这个大值,等价于关闭该判定</summary>
        private const float UnusedInterval = 999f;

        //按时间间隔触发
        public float createByTime = 0.08f;
        //按距离间隔触发
        public float createByDistance = 4;

        //单帧最多生成的幻影数量,避免掉帧时一帧内瞬间生成过多导致卡顿
        public int maxCreatePerFrame = 4;

        public float fadeTime = 1.2f;
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

        public void StartAnim(BaseMsg msg, float time = 0)
        {
            if (!_isInited)
            {
                Init();
            }

            if (msg.strMsg == "Distance")
            {
                createByDistance = Mathf.Max(MinInterval, msg.numMsg);
                createByTime = UnusedInterval;
            }
            else if (msg.strMsg == "Time")
            {
                createByTime = Mathf.Max(MinInterval, msg.numMsg);
                createByDistance = UnusedInterval;
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
            float timer = 0;
            Vector3 lastCreatePos = transform.position;
            //第一遍先触发
            CreateOnPos(lastCreatePos);

            while (_maxActiveTime > 0)
            {
                float deltaTime = XCTime.deltaTime;
                _maxActiveTime -= deltaTime;
                timer += deltaTime;

                int createdCount = 0;

                //按时间间隔补齐:帧率低时一帧内补出多个,保证幻影密度与帧率无关
                while (timer >= createByTime && createdCount < maxCreatePerFrame)
                {
                    timer -= createByTime;
                    CreateOnPos(transform.position);
                    createdCount++;
                }

                //按距离间隔补齐:把这一帧的位移按间隔等分,掉帧或瞬移时拖影不会断节
                Vector3 currentPos = transform.position;
                Vector3 delta = currentPos - lastCreatePos;
                float distance = delta.magnitude;
                while (distance >= createByDistance && createdCount < maxCreatePerFrame)
                {
                    lastCreatePos += delta * (createByDistance / distance);
                    CreateOnPos(lastCreatePos);
                    createdCount++;
                    delta = currentPos - lastCreatePos;
                    distance = delta.magnitude;
                }

                yield return null;
            }
        }

        private void CreateOnPos(Vector3 nextPos)
        {
            GameObject copy = Instantiate(gameObject);
            copy.GetComponent<Animator>().enabled = false;
            var renderers = copy.GetComponentsInChildren<Renderer>();
            copy.transform.SetPositionAndRotation(nextPos, transform.rotation);
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
