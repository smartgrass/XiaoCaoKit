using Flux;
using GG.Extensions;
using ProtoBuf.Meta;
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

        public TriggerTargetType triggerTargetType = TriggerTargetType.Enemy;

        private TriggerTargetType SafeTriggerTargetType =>
            triggerTargetType == TriggerTargetType.None ? TriggerTargetType.Enemy : triggerTargetType;

        public int subSkillId = 0;

        public MeshType MeshType => meshInfo.meshType;

        public ITrigger Trigger { get; set; }

        public BaseAtker AtkCommponent { get; set; }

        private bool isTriggeredEnd;
        private bool hasCollision;


        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            isTriggeredEnd = false;
            hasCollision = false;

            AtkCommponent = TriggerCache.GetTrigger(MeshType);
            GetTrigger();

            var tf = AtkCommponent.transform;
            tf.SetParent(Tran);

            Trigger.SetMeshInfo(meshInfo);
            Trigger.InitListener(OnReceiveTriggerEnter, XCSetting.GetTriggerLayerMask(Info.role.team, SafeTriggerTargetType));
            SetAtkInfo();
            Trigger.Switch(true);
        }


        void GetTrigger()
        {
            if (UseRayCast())
            {
                Trigger = AtkCommponent.GetOrAddComponent<RayCasterTrigger>();
            }
            else
            {
                Trigger = AtkCommponent.GetOrAddComponent<ColliderTrigger>();
            }
        }


        private bool UseRayCast()
        {
            if (MeshType == MeshType.Sector)
            {
                return false;
            }

            return true;
        }


        public override void OnFinish()
        {
            bool autoTriggerOnFinish = maxTriggerTime == 1 && !hasCollision && !isTriggeredEnd;

            base.OnFinish();

            if (autoTriggerOnFinish)
            {
                OnMaxTrigger();
            }

            Trigger?.OnFinish();

            TriggerCache.Release(MeshType, AtkCommponent.gameObject);
            //回收处理?
        }

        private void OnReceiveTriggerEnter(Collider other)
        {
            if (isTriggeredEnd || other == null)
            {
                return;
            }

            if (XCSetting.HasTriggerTarget(SafeTriggerTargetType, TriggerTargetType.Enemy) && other.isTrigger)
            {
                hasCollision = true;
                AtkCommponent.ReceiveTriggerEnter(other);
                return;
            }

            if (other.isTrigger || !IsTargetLayer(other.gameObject.layer))
            {
                return;
            }

            hasCollision = true;
            AtkCommponent.InitHitInfo(other);
            OnMaxTrigger();
        }

        private bool IsTargetLayer(int layer)
        {
            int layerMask = XCSetting.GetEnvironmentTriggerLayerMask(SafeTriggerTargetType);
            return (layerMask & (1 << layer)) != 0;
        }

        private void SetAtkInfo()
        {
            PlayerAttr attr = Info.role.PlayerAttr;
            int baseAtk = attr.Atk;
            bool isCrit = MathTool.IsInRandom(attr.Crit / 100f);
            AtkCommponent.maxTriggerTime = maxTriggerTime;
            AtkCommponent.curTriggerTime = 0;

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

            AtkCommponent.InitAtkInfo(info);
        }

        void OnMaxTrigger()
        {
            if (isTriggeredEnd)
            {
                return;
            }

            isTriggeredEnd = true;
            //击中后停止运行
            Debug.Log($"--- OnMaxTrigger");
            TriggerEnd();
            task.ObjectData?.OnEnd();
            task.SetBreak();
        }

        private void TriggerEnd()
        {
            Transform triggerTran = task.ObjectData?.Tran;
            if (triggerTran == null)
            {
                return;
            }

            var behaviours = triggerTran.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ITriggerEnd triggerEnd)
                {
                    triggerEnd.OnTriggerEnd(AtkCommponent);
                }
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

        public void PreWarm()
        {
            CreateAtkerPool(MeshType.Sector);
        }


        public Dictionary<MeshType, AssetPool> dicPool = new Dictionary<MeshType, AssetPool>();


        public static Atker GetTrigger(MeshType meshType)
        {
            if (!Inst.dicPool.TryGetValue(meshType, out AssetPool assetPool))
            {
                assetPool = CreateAtkerPool(meshType);
            }

            return assetPool.Get().GetComponent<Atker>();
        }

        private static AssetPool CreateAtkerPool(MeshType meshType)
        {
            AssetPool assetPool;
            var newObject = new GameObject(XCTriggerEvent.TriggerNames[(int)meshType]);
            newObject.AddComponent<Atker>();
            assetPool = new AssetPool(newObject);
            newObject.SetActive(false);
            Inst.dicPool[meshType] = assetPool;
            return assetPool;
        }

        public static void Release(MeshType meshType, GameObject gameObject)
        {
            Inst.dicPool[meshType].Release(gameObject);
        }
    }
}
