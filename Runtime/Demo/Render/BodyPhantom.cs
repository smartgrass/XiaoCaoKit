using DG.Tweening;
using Fantasy.Pool;
using System.Collections;
using UnityEngine;
using XiaoCao;

namespace XiaoCao.Render
{
    public class BodyPhantom : MonoBehaviour
    {
        public float meshRefreshRate = 0.1f;
        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        public Material mat;

        private AssetPool _pool;
        private Coroutine _coroutine;
        private float _maxActiveTime = 10;

        private void Awake()
        {
            GameObject prefab = new GameObject();
            MeshRenderer mr = prefab.AddComponent<MeshRenderer>();
            MeshFilter mf = prefab.AddComponent<MeshFilter>();
            _pool = new AssetPool(prefab);
        }

        public void StartAnim(float time)
        {
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
            WaitForSeconds wait = new WaitForSeconds(meshRefreshRate);
            if (skinnedMeshRenderers == null)
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            while (_maxActiveTime > 0)
            {
                _maxActiveTime -= meshRefreshRate;
                for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                {
                    var sr = skinnedMeshRenderers[i];
                    GameObject gobj = _pool.Get();
                    gobj.transform.SetPositionAndRotation(sr.transform.position, sr.transform.rotation);

                    MeshRenderer mr = gobj.GetComponent<MeshRenderer>();
                    MeshFilter mf = gobj.GetComponent<MeshFilter>();
                    Mesh mesh = new Mesh();
                    skinnedMeshRenderers[i].BakeMesh(mesh);
                    mf.mesh = mesh;
                    mr.material = mat;
                    var tween = mr.material.DOFade(0, 3);
                    tween.OnComplete(() =>
                    {
                        _pool.Release(gobj);
                    });
                }
                yield return wait;
            }
        }


        private void OnDestroy()
        {
            foreach (var item in _pool.pool.m_List)
            {
                if (item)
                {
                    Destroy(item);
                }
            }
            _pool.pool.Clear();
        }


    }
}