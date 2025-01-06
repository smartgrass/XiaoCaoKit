using Flux;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            var col = CurTrigger.GetComponent<MeshCollider>();
            CurCol = col;
            col.convex = true;
            col.isTrigger = true;
            col.sharedMesh = MathLayoutTool.GetSectorMesh(meshInfo.GetRadian, meshInfo.GetRadius, meshInfo.GetHight, 20);

            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = Vector3.one;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);

            //TODO 缩放要特殊处理下,暂无

        }

        private void OnSphere()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            CurTrigger = triggerGo.GetComponent<AtkTrigger>();
            var col = CurTrigger.GetComponent<SphereCollider>();
            CurCol = col;
            CurCol.isTrigger = true;
            col.radius = 1;

            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.GetSize;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);




        }

        private void OnBox()
        {
            var triggerGo = TriggerCache.GetTrigger(MeshType);
            CurTrigger = triggerGo.GetComponent<AtkTrigger>();

            var col = CurTrigger.GetComponent<BoxCollider>();
            CurCol = col;
            CurCol.isTrigger = true;
            var tf = triggerGo.transform;
            tf.SetParent(Tran);
            tf.localScale = meshInfo.GetSize;
            tf.localPosition = meshInfo.GetCenter;
            tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);

            col.center = Vector3.zero;
            col.size = Vector3.one;

        }

        private void SetCurAtkTrigger()
        {
            PlayerAttr attr = Info.role.PlayerAttr;
            int baseAtk = attr.Atk;
            bool isCrit = MathTool.IsInRandom(attr.Crit / 100f);
            CurTrigger.maxTriggerTime = maxTriggerTime;
            CurTrigger.curTriggerTime = 0;

            int subIndex = task.ObjectData != null ? task.ObjectData.index : 0;

            var info = new AtkInfo()
            {
                team = Info.role.team,
                skillId = Info.skillId,
                subSkillId = subIndex,
                baseAtk = baseAtk,
                atk = baseAtk,
                isCrit = isCrit,
                atker = Info.role.id
            };
            info.maxTriggerAct += OnMaxTrigger;

            info.atk = (int)(baseAtk * info.GetSkillSetting.AckRate);

            CurTrigger.InitAtkInfo(info);
        }

        void OnMaxTrigger()
        {
            task.ObjectData.OnEnd();
            Debug.Log($"--- OnMaxTrigger");
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


    public class TriggerCache : Singleton<TriggerCache>, IClearCache, IHasClear
    {
        protected override void Init()
        {
            base.Init();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            ClearCache();
        }

        public Dictionary<MeshType, AssetPool> dicPool = new Dictionary<MeshType, AssetPool>();


        public static GameObject GetTrigger(MeshType meshType)
        {
            if (Inst.dicPool.TryGetValue(meshType, out AssetPool assetPool))
            {
                return assetPool.Get();
            }

            var newObject = new GameObject(XCTriggerEvent.TriggerNames[(int)meshType]);

            newObject.AddComponent<AtkTrigger>();

            Collider collider = null;

            //拼接得到一个key
            switch (meshType)
            {
                case MeshType.Box:
                    collider = newObject.AddComponent<BoxCollider>();
                    break;
                case MeshType.Sphere:
                    collider = newObject.AddComponent<SphereCollider>();
                    break;
                case MeshType.Sector:
                    collider = newObject.AddComponent<MeshCollider>();
                    break;
                case MeshType.Other:
                    collider = newObject.AddComponent<MeshCollider>();
                    break;
                default:
                    Debug.LogError($"--- unknow {meshType}");
                    break;
            }
            collider.enabled = false;
            collider.isTrigger = true;
            collider.enabled = true;

            assetPool = new AssetPool(newObject);
            Inst.dicPool[meshType] = assetPool;
            return assetPool.Get();
        }

        public static void Release(MeshType meshType, GameObject gameObject)
        {
            Inst.dicPool[meshType].Release(gameObject);
        }

    }
}
