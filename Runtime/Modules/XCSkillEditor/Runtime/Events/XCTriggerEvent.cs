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

        public int subSkillId = 0;

        public MeshType MeshType => meshInfo.meshType;

        public ITrigger Trigger { get; set; }

        public BaseAtker AtkCommponent { get; set; }


        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);

            AtkCommponent = TriggerCache.GetTrigger(MeshType);
            GetTrigger();

            var tf = AtkCommponent.transform;
            tf.SetParent(Tran);

            Trigger.SetMeshInfo(meshInfo);
            Trigger.InitListener(AtkCommponent.ReceiveTriggerEnter);
            Trigger.Switch(true);
            SetAtkInfo();
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
            base.OnFinish();

            Trigger.OnFinish();

            TriggerCache.Release(MeshType, AtkCommponent.gameObject);
            //回收处理?
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
            //击中后停止运行
            Debug.Log($"--- OnMaxTrigger");
            task.ObjectData.OnEnd();
            task.SetBreak();
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


        public static Atker GetTrigger(MeshType meshType)
        {
            if (Inst.dicPool.TryGetValue(meshType, out AssetPool assetPool))
            {
                return assetPool.Get().GetComponent<Atker>();
            }

            var newObject = new GameObject(XCTriggerEvent.TriggerNames[(int)meshType]);
            var trigger = newObject.AddComponent<Atker>();
            assetPool = new AssetPool(newObject);
            Inst.dicPool[meshType] = assetPool;
            return assetPool.Get().GetComponent<Atker>(); ;
        }

        public static void Release(MeshType meshType, GameObject gameObject)
        {
            Inst.dicPool[meshType].Release(gameObject);
        }

    }
}
