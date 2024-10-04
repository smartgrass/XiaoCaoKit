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
        public static string[] TriggerNames = new[] 
        { "BoxTrigger", "SphereTrigger", "SectorTrigger", "OtherTrigger" };

        public int maxTriggerTime = 0;

        public MeshInfo meshInfo;

        public int subSkillId = 0;

        public MeshType MeshType => meshInfo.meshType;

        public Collider CurCol { get; set; }
        public AtkTrigger CurTrigger { get; set; }


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
            SetCurAtkTrigger();

        }

        public void OnSector()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            CurTrigger = triggerGo.GetComponent<AtkTrigger>();

            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = Vector3.one;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);

            var col = CurTrigger.GetComponent<MeshCollider>();
            col.convex = true;
            col.isTrigger = true;
            //TODO 缩放要特殊处理下,暂无
            col.sharedMesh = MathLayoutTool.GetSectorMesh(meshInfo.GetRadian, meshInfo.GetRadius, meshInfo.GetHight, 20);

            CurCol = col;
        }

        private void OnSphere()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            CurTrigger = triggerGo.GetComponent<AtkTrigger>();
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.GetSize;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);

            var col = CurTrigger.GetComponent<SphereCollider>();
            col.radius = 1;

            CurCol = col;
            CurCol.isTrigger = true;
        }

        private void OnBox()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            CurTrigger = triggerGo.GetComponent<AtkTrigger>();
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.GetSize;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);

            var col = CurTrigger.GetComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = Vector3.one;
            CurCol = col;
            CurCol.isTrigger = true;
        }

        private void SetCurAtkTrigger()
        {
            PlayerAttr attr = Info.role.roleData.playerAttr;
            int atk = attr.GetAtk();
            bool isCrit = MathTool.IsInRandom(attr.crit / 100f);
            CurTrigger.maxTriggerTime = maxTriggerTime;
            CurTrigger.curTriggerTime = 0;

            CurTrigger.info = new AtkInfo()
            {
                objectData = task.ObjectData,
                team = Info.role.team,
                skillId = Info.skillId,
                subSkillId = task.ObjectData.index,
                atk = atk,
                isCrit = isCrit,
            };
        }


        public override void OnFinish()
        {
            base.OnFinish();
            if (CurCol)
            {
                TriggerCache.Release(MeshType, CurCol.gameObject);
                //Debug.Log($"--- Release col {MeshType}");
            }
            else
            {
                Debug.LogError($"--- no col {MeshType}");
            }
        }

    }


    public class TriggerCache : Singleton<TriggerCache>, IClearCache
    {

        public static Dictionary<MeshType, AssetPool> dicPool = new Dictionary<MeshType, AssetPool>();
        public static GameObject GetTrigger(MeshType meshType)
        {
            if (dicPool.TryGetValue(meshType, out AssetPool assetPool))
            {
                return assetPool.Get();
            }

            var newObject = new GameObject(XCTriggerEvent.TriggerNames[(int)meshType]);

            newObject.AddComponent<AtkTrigger>();

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
            return assetPool.Get();
        }

        public static void Release(MeshType meshType, GameObject gameObject)
        {
            dicPool[meshType].Release(gameObject);
        }

    }
}
