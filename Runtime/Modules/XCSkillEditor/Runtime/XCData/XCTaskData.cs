﻿using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Flux;
using Object = UnityEngine.Object;
using JetBrains.Annotations;

namespace XiaoCao
{
    /// <summary>
    /// 数据原型, 无生成数据
    /// </summary>
    [Serializable]
    public class XCTaskData
    {
        public int start;

        public int end;

        public ObjectData objectData;

        public List<XCEvent> _events = new List<XCEvent>();

        public List<XCTaskData> subDatas;

        //NoSerializa
        public bool HasTrigger { get; set; }

        public float speed = 1;

        public void AddSubData(XCTaskData subData)
        {
            if (subDatas == null) subDatas = new List<XCTaskData>();
            subDatas.Add(subData);
        }

        public void SortEvents()
        {
            _events.Sort(new EventSort());
        }

    }

    class EventSort : IComparer<XCEvent>
    {
        public int Compare(XCEvent e1, XCEvent e2)
        {
            if (e1.Start < e2.Start)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    [Serializable]
    public class ObjectData
    {
        public string ObjectPath = "";
        public bool isPs;

        public int endFrame;
        public TransfromType transfromType;
        public Vector3 position;
        public Vector3 eulerAngle;
        public Vector3 scale;

        public XCState State { get; set; }
        public TaskInfo Info { get; set; }
        public Transform Tran { get; set; }
        public Transform PlayerTF { get; set; }
        public ParticleSystem Ps { get; set; }

        public void OnTrigger(TaskInfo info)
        {
            this.Info = info;
            var instance = PoolMgr.Inst.Get(ObjectPath);
            Tran = instance.transform;
            PlayerTF = info.playerTF;
            info.curTF = Tran;
            info.curGO = instance;
            if (isPs && instance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                InitPs(ps);
            }
            SetPos();
            State = XCState.Running;
        }

        private void InitPs(ParticleSystem ps)
        {
            Ps = ps;
            if (!Info.speed.IsFEqual(1))
            {
                var main = Ps.main;
                main.simulationSpeed = Info.speed;
            }
            Ps.Play();
        }

        public void OnFrameUpdate(int frame)
        {
            if (frame>= endFrame && State == XCState.Running)
            {
                State = XCState.Stopped;
                Hide();
            }
        }

        //设置起始位置
        private void SetPos()
        {
            if (transfromType == TransfromType.StartPos)
            {
                Tran.eulerAngles = Info.castEuler + eulerAngle;
                //Quaternion(方向) * localPos = 世界向量
                Tran.position = Info.castPos + Quaternion.Euler(Info.castEuler) * position;
            }
            if (transfromType is TransfromType.PlayerTransfrom or TransfromType.PlyerPos)
            {
                //实时计算,误差似乎不可避免
                Tran.eulerAngles = PlayerTF.eulerAngles + eulerAngle;
                Tran.position = PlayerTF.TransformPoint(position);
            }
            if (transfromType == TransfromType.PlayerTransfrom)
            {
                Tran.SetParent(PlayerTF, true);
            }
            Tran.localScale = scale;
        }


        public void Hide()
        {
            if (Ps)
            {
                Ps.Stop();
            }
            else
            {
                Tran.gameObject.SetActive(false);
            }

            PoolMgr.Inst.Release(ObjectPath, Tran.gameObject);
        }

    }

}