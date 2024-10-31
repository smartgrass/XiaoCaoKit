using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using EzySlice;
using Plane = EzySlice.Plane;
using OdinSerializer.Utilities;
using UnityEditor;

namespace XiaoCao
{
    public class Test_SliceObject : MonoBehaviour
    {
        public GameObject game;

        public Material crossSectionMaterial;

        public int cutCount = 2;
        public float tempMass = 1;

        public string savaPath = "Assets/_ResArt/Item/SliceObject";

        public string tempName = "mesh";

        private List<GameObject> tempList;
        private Vector3 tempCenter;
        private float tempTotalVolume = 1;


        [Button]
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
                var ret = DoSliceObjects(list, planes[i], mat);
                if (i != 0)
                {
                    Debug.Log($"--- destory {i}");
                    list.ForEach(o =>
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(o);
                        }
                        else
                        {
                            DestroyImmediate(o);
                        }
                    });
                }
                list.Clear();
                list.AddRange(ret);
            }

            list.ForEach(o =>
            {
                o.transform.SetParent(game.transform);
                var rb = o.GetOrAddComponent<Rigidbody>();
                var mesh = o.GetComponent<MeshFilter>().sharedMesh;
                SetRigiBodyMass(rb, GetVolume(mesh), tempTotalVolume);
                var col = o.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
                col.convex = true;
            });
            tempList = list;
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
            var planes = GetShatterPlanes(center, cutCount);
            return planes;
        }

        private List<GameObject> DoSliceObjects(List<GameObject> objs, Plane plane, Material material)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (var obj in objs)
            {
                var gameObjects = obj.SliceInstantiate(plane, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), material);
                list.AddRange(gameObjects);
            }
            return list;
        }


        public Plane[] GetShatterPlanes(Vector3 point, int cuts = 1)
        {
            Plane[] planes = new Plane[cuts];

            for (int i = 0; i < planes.Length; i++)
            {
                planes[i] = new Plane(point, Random.onUnitSphere);
            }
            return planes;
        }



        [SerializeField] private float _triggerForce = 0.5f;
        [SerializeField] private float _explosionRadius = 5;
        [SerializeField] private float _explosionForce = 500;
        [SerializeField] private GameObject _particles;

        private void DoExploded(Vector3 centerPos)
        {
            var surroundingObjects = Physics.OverlapSphere(centerPos, _explosionRadius);
            foreach (var obj in surroundingObjects)
            {
                Debug.Log($"--- DoExploded");
                var rb = obj.GetComponent<Rigidbody>();
                if (rb == null) continue;

                rb.AddExplosionForce(_explosionForce, centerPos, _explosionRadius, 1);
            }
            //Instantiate(_particles, transform.position, Quaternion.identity);
        }





        [Button("保存mesh")]
        void SavaAllMesh()
        {
            int i = 1;
            foreach (var item in tempList)
            {
                item.gameObject.name = tempName + "_" + i;
                i++;
                SavaMesh(item);
            }
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
    }
}
