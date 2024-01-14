using Flux;
using System;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace XiaoCao
{
    [Serializable]
    public class XCTriggerEvent : XCEvent
    {
        public const string TriggerName = "Trigger";

        public MeshInfo meshInfo;

        public MeshType MeshType => meshInfo.meshType;

        public Collider CurCol { get; set; }

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);

            switch (MeshType)
            {
                case MeshType.Box:
                    OnBox();
                    break;
                case MeshType.Sphere:
                    OnSphere();
                    break;
                case MeshType.Sector:
                    OnSector();
                    break;
                case MeshType.Other:
                    break;
                default:
                    break;
            }

        }

        public void OnSector()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = Vector3.one;
            tf.localPosition = meshInfo.center;
            tf.localRotation = Quaternion.Euler(meshInfo.eulerAngles);

            var col = this.Go.GetComponent<MeshCollider>();
            col.sharedMesh = MathLayoutTool.GetSectorCylinderMesh(meshInfo.angle, meshInfo.radius, meshInfo.hight, 20);
            //TODO 拉伸以后做
            CurCol = col;
        }

        private void OnSphere()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.size;
            tf.localPosition = meshInfo.center;
            tf.localRotation = Quaternion.Euler(meshInfo.eulerAngles);

            var col = this.Go.GetComponent<SphereCollider>();
            col.radius = 1;

            CurCol = col;
        }

        private void OnBox()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.size;
            tf.localPosition = meshInfo.center;
            tf.localRotation = Quaternion.Euler(meshInfo.eulerAngles);

            var col = tf.GetComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = Vector3.one;
            CurCol = col;
        }



        public override void OnFinish()
        {
            base.OnFinish();
            if (CurCol)
            {
                TriggerCache.Release(MeshType, CurCol.gameObject);
            }
        }

    }


    public class TriggerCache : Singleton<TriggerCache>, IClearCache
    {

        public static Dictionary<MeshType, AssetPool> dicPool = new Dictionary<MeshType, AssetPool>();
        public static GameObject GetTrigger(MeshType meshType)
        {
            AssetPool assetPool = dicPool[meshType];

            if (assetPool != null)
            {
                return assetPool.pool.Get();
            }

            var newObject = new GameObject(XCTriggerEvent.TriggerName);

            //拼接得到一个key
            switch (meshType)
            {
                case MeshType.Box:
                    newObject.AddComponent<BoxCollider>();
                    break;
                case MeshType.Sphere:
                    newObject.AddComponent<SphereCollider>();
                    break;
                case MeshType.Sector:
                    newObject.AddComponent<MeshCollider>();
                    break;
                case MeshType.Other:
                    newObject.AddComponent<MeshCollider>();
                    break;
                default:
                    break;
            }

            assetPool = new AssetPool(newObject);
            dicPool[meshType] = assetPool;
            return assetPool.pool.Get();
        }

        public static void Release(MeshType meshType, GameObject gameObject)
        {
            dicPool[meshType].pool.Release(gameObject);
        }

    }
}
