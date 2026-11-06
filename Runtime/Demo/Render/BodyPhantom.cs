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

        /// <summary>幻影节点名,复用同一个名字避免每次生成都产生字符串垃圾</summary>
        private const string PhantomName = "BodyPhantomCopy";
        private const string PhantomMeshName = "PhantomMesh";

        /// <summary>烘焙网格池上限,同时存活的幻影约 fadeTime/createByTime 个,每个若干个网格</summary>
        private const int MaxPooledMesh = 64;

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

        //幻影只要"冻结的顶点",用网格池复用,避免每次生成都 new Mesh 造成 GC 与显存抖动
        private readonly Stack<Mesh> _meshPool = new Stack<Mesh>();

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

        /// <summary>
        /// 生成一个幻影: 只复制"当前姿势的网格", 不再 Instantiate 整个角色。
        /// 原实现会连 VRM 的 Vrm10Instance / 160+ 个 SpringBoneJoint 一起复制,
        /// 移动端每 0.08s 复制一次整条骨骼层级会直接掉帧, 骨架组件一旦被剥离还会刷屏报警。
        /// </summary>
        private void CreateOnPos(Vector3 nextPos)
        {
            GameObject copy = new GameObject(PhantomName);
            Transform root = copy.transform;
            root.SetPositionAndRotation(transform.position, transform.rotation);
            root.localScale = transform.lossyScale;

            Material phantomMat = mat != null ? new Material(mat) : null;
            List<Mesh> bakedMeshes = null;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Mesh mesh;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null)
                    {
                        continue;
                    }

                    mesh = RentMesh();
                    //useScale=true 才是"渲染器局部空间(不含缩放)"; 传 false 会把世界缩放烘进顶点,
                    //再乘节点 localScale 就成了双重缩放(实测 Unity 6000.4 与官方文档描述相反)
                    skinned.BakeMesh(mesh, true);
                    bakedMeshes ??= new List<Mesh>();
                    bakedMeshes.Add(mesh);
                }
                else if (renderer is MeshRenderer)
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    //静态网格是共享资源, 直接引用, 不能回收
                    mesh = filter.sharedMesh;
                }
                else
                {
                    //粒子/拖尾/LineRenderer 不属于"身体幻影", 跳过
                    continue;
                }

                CreateMeshNode(root, renderer, mesh, phantomMat);
            }

            root.position = nextPos;

            if (root.childCount == 0)
            {
                Destroy(copy);
                if (phantomMat != null)
                {
                    Destroy(phantomMat);
                }

                ReturnMeshes(bakedMeshes);
                return;
            }

            if (phantomMat != null)
            {
                //一个幻影共用一份材质实例: 原来逐段 .materials 会克隆出 19 份, 视觉一致但开销大
                phantomMat.DOFade(0, fadeTime);
                Destroy(phantomMat, fadeTime + 0.1f);
            }

            Destroy(copy, fadeTime + 0.1f);

            if (bakedMeshes != null && bakedMeshes.Count > 0)
            {
                //比游戏物体晚一点回收, 避免网格被重新烘焙时旧渲染节点还活着
                StartCoroutine(IERecycleMeshes(bakedMeshes, fadeTime + 0.3f));
            }
        }

        /// <summary>
        /// 用 网格 + MeshRenderer 拼出与本体同姿态的节点, 姿态按"角色根节点"的相对变换还原
        /// </summary>
        private void CreateMeshNode(Transform root, Renderer source, Mesh mesh, Material phantomMat)
        {
            Transform sourceTrans = source.transform;
            GameObject node = new GameObject(PhantomMeshName);
            Transform nodeTrans = node.transform;
            nodeTrans.SetParent(root, false);

            Vector3 rootScale = transform.lossyScale;
            Vector3 sourceScale = sourceTrans.lossyScale;
            nodeTrans.localPosition = transform.InverseTransformPoint(sourceTrans.position);
            nodeTrans.localRotation = Quaternion.Inverse(transform.rotation) * sourceTrans.rotation;
            nodeTrans.localScale = new Vector3(
                SafeDiv(sourceScale.x, rootScale.x),
                SafeDiv(sourceScale.y, rootScale.y),
                SafeDiv(sourceScale.z, rootScale.z));

            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = node.AddComponent<MeshRenderer>();

            int slotCount = source.sharedMaterials.Length;
            if (slotCount <= 0)
            {
                slotCount = 1;
            }

            Material[] materials = new Material[slotCount];
            Array.Fill(materials, phantomMat);
            meshRenderer.sharedMaterials = materials;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static float SafeDiv(float value, float div)
        {
            return Mathf.Approximately(div, 0f) ? value : value / div;
        }

        private Mesh RentMesh()
        {
            while (_meshPool.Count > 0)
            {
                Mesh mesh = _meshPool.Pop();
                if (mesh != null)
                {
                    return mesh;
                }
            }

            return new Mesh();
        }

        private void ReturnMeshes(List<Mesh> meshes)
        {
            if (meshes == null)
            {
                return;
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                if (mesh == null)
                {
                    continue;
                }

                if (_meshPool.Count >= MaxPooledMesh)
                {
                    Destroy(mesh);
                    continue;
                }

                _meshPool.Push(mesh);
            }

            meshes.Clear();
        }

        private IEnumerator IERecycleMeshes(List<Mesh> meshes, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnMeshes(meshes);
        }

        private void OnDestroy()
        {
            StopAnim();
            while (_meshPool.Count > 0)
            {
                Mesh mesh = _meshPool.Pop();
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }
        }
    }
}
