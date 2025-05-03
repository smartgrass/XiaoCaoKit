using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Plane = EzySlice.Plane;
#if UNITY_EDITOR
using UnityEditor;
using EzySlice;
#endif

namespace XiaoCao
{
    public class Test_SliceObject : MonoBehaviour
    {
#if UNITY_EDITOR

        public GameObject game;

        public Material crossSectionMaterial;

        public int cutCount = 2;
        public float tempMass = 1;

        public string savaPath = "Assets/_ResArt/Item/SliceObject";

        public string tempName = "mesh";

        private List<GameObject> tempList;
        private Vector3 tempCenter;
        private float tempTotalVolume = 1;

        [Button("切割")]
        void DoSlice()
        {
            //获取随机切割面
            Plane[] planes = GetRandomPlanes(game);
            List<GameObject> list = new List<GameObject>();

            if (game.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                tempMass = rb.mass;
            }

            list.Add(game);
            Material mat = crossSectionMaterial ? crossSectionMaterial : GetMat(game);
            for (int i = 0; i < cutCount; i++)
            {
                var retList = DoSliceObjects(list, planes[i], mat);
                if (i != 0)
                {
                    Debug.Log($"--- destory {i}");
                    list.ForEach(o =>
                    {
                        if (!o.CompareTag(Tags.ITEM))
                        {
                            DestroyImmediate(o);
                        }
                        else
                        {
                            o.tag = Tags.UNTAGGED;
                        }
                    });
                }
                list.Clear();
                list.AddRange(retList);
            }

            list.ForEach(o =>
            {
                //Vector3 pos = o.transform.localPosition;
                o.transform.SetParent(game.transform);
                o.transform.localPosition = Vector3.zero;
                var rb = o.GetOrAddComponent<Rigidbody>();
                var mesh = o.GetComponent<MeshFilter>().sharedMesh;
                SetRigiBodyMass(rb, GetVolume(mesh), tempTotalVolume);
                var col = o.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
                col.convex = true;
            });
            tempList = list;
        }

        [Button("切割 & 保存mesh")]
        void SavaAllMesh()
        {
            DoSlice();
            int i = 1;
            foreach (var item in tempList)
            {
                item.gameObject.name = tempName + "_" + i;
                i++;
                SavaMesh(item);
            }
        }

        //获取体积
        private float GetVolume(Mesh mesh)
        {
            Vector3 size = mesh.bounds.size;
            float volume = size.x * size.y * size.z;
            return volume;
        }

        void SetRigiBodyMass(Rigidbody rigidbody, float volume, float totalVolume)
        {
            rigidbody.mass = tempMass * (volume / totalVolume);

            //if (!rigidbody.isKinematic)
            //{
            //    rigidbody.linearVelocity = rigidbody.GetPointVelocity(rigidbody.worldCenterOfMass);

            //    rigidbody.angularVelocity = rigidbody.angularVelocity;
            //}
        }


        [Button(enabledMode: EButtonEnableMode.Playmode)]
        void DoSliceAndExploded()
        {
            DoSlice();
            DoExploded(tempCenter);
        }

        private Material GetMat(GameObject game)
        {
            MeshRenderer mesh = game.GetComponent<MeshRenderer>();
            return mesh.sharedMaterial;
        }

        private Plane[] GetRandomPlanes(GameObject game)
        {
            var mesh = game.GetComponent<MeshFilter>().sharedMesh;
            Vector3 center = mesh.bounds.center;
            tempCenter = center;
            tempTotalVolume = GetVolume(mesh);
            tempName = mesh.name;
            var planes = GetShatterPlanes(mesh.bounds, cutCount);
            return planes;
        }


        private List<GameObject> DoSliceObjects(List<GameObject> objs, Plane plane, Material material)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (var obj in objs)
            {
                var gameObjects = obj.SliceInstantiate(plane, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), material);
                if (gameObjects != null)
                {
                    list.AddRange(gameObjects);
                    obj.tag = Tags.UNTAGGED;
                }
                else
                {
                    obj.tag = Tags.ITEM;
                    list.Add(obj);
                }
            }
            return list;
        }

        public bool randomCenter = true;

        public Plane[] GetShatterPlanes(Bounds bounds, int cuts = 1)
        {
            Plane[] planes = new Plane[cuts];

            Vector3 center = bounds.center;

            for (int i = 0; i < planes.Length; i++)
            {
                if (randomCenter)
                {
                    center = bounds.center;
                    float y = bounds.size.y * Random.Range(-0.4f, 0.4f);
                    float x = bounds.size.x * Random.Range(-0.4f, 0.4f);
                    center += new Vector3(x, y);
                }
                planes[i] = new Plane(center, Random.onUnitSphere);
            }
            return planes;
        }



        [SerializeField] private float _triggerForce = 0.5f;
        [SerializeField] private float _explosionForce = 500;
        [SerializeField] private GameObject _particles;

        [SerializeField] private float _testExplosionRadius = 5;
        private void DoExploded(Vector3 centerPos)
        {
            var surroundingObjects = Physics.OverlapSphere(centerPos, _testExplosionRadius);
            foreach (var obj in surroundingObjects)
            {
                Debug.Log($"--- DoExploded");
                var rb = obj.GetComponent<Rigidbody>();
                if (rb == null) continue;

                rb.AddExplosionForce(_explosionForce, centerPos, _testExplosionRadius, 1);
            }
            //Instantiate(_particles, transform.position, Quaternion.identity);
        }



        public void SavaMesh(GameObject game)
        {
            Debug.Log("mesh" + game.name);
            var meshFilter = game.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;
            string path = savaPath + "/" + game.name + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
#endif
    }
}
