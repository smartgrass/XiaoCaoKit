using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Flux;
using Object = UnityEngine.Object;
using JetBrains.Annotations;
using OdinSerializer;
using System.Text;
using cfg;
using TEngine;
using cfg.Skill;

namespace XiaoCao
{
    /// <summary>
    /// 数据原型, 无生成数据
    /// </summary>
    [Serializable]
    public class XCTaskData
    {
        public ObjectData objectData;

        public List<XCEvent> _events = new List<XCEvent>();

        public List<XCTaskData> subDatas;

        //NoSerializa
        public bool HasTrigger { get; set; }

        public bool IsMainTask { get; set; }

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

        public override string ToString()
        {
            var buffer = SerializationUtility.SerializeValue<XCTaskData>(this, DataFormat.JSON);
            string res = Encoding.UTF8.GetString(buffer);
            return res;
        }

    }

    class EventSort : IComparer<XCEvent>
    {
        public int Compare(XCEvent e1, XCEvent e2)
        {
            if (e1.Start > e2.Start)
            {
                return 1;
            }
            else if (e1.Start < e2.Start)
            {
                return -1;
            }
            return 0;
        }
    }

    [Serializable]
    public class ObjectData
    {
        public string ObjectPath = "";
        public int index;//subSkillId
        public bool isPs;

        public int endFrame;
        public int startFrame;
        public TransfromType transfromType;
        public Vector3 position;
        public Vector3 eulerAngle;
        public Vector3 scale;
        public string otherPointName;

        public XCState State { get; set; }

        public TaskInfo Info { get; set; }
        public Transform Tran { get; set; }
        public Transform PlayerTF { get; set; }
        public ParticleSystem Ps { get; set; }

        public IXCCommand command { get; set; }

        public bool HasStart { get; set; }

        public void OnTrigger(TaskInfo info)
        {
            this.Info = info;
            var instance = PoolMgr.Inst.Get(ObjectPath);
            Tran = instance.transform;
            PlayerTF = info.playerTF;

            if (isPs && instance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                //控制特效播放速度
                InitPs(ps);
            }
            SetPos();

            CheckAtkMsg(info);

            State = XCState.Running;
            HasStart = true;
        }

        private void CheckAtkMsg(TaskInfo info)
        {
            if (info.role.IsPlayer)
            {
                var setting = LubanTables.GetSkillSetting(info.skillId, index);
                if (setting.Tags.Contains(TaskInfoTags.Slash))
                {
                    GameEvent.Send<ObjectData>(EGameEvent.PlayerCreatNorAtk.Int(), this);
                }
            }
        }

        private void InitPs(ParticleSystem ps)
        {
            Ps = ps;
            if (!Info.GetAnimSpeed.IsFEqual(1))
            {
                SetPsSpeed(ps, Info.GetAnimSpeed);
            }
            Ps.Play();
        }


        public void SetPsSpeed(ParticleSystem ps, float speed)
        {
            foreach (var p in ps.GetComponentsInChildren<ParticleSystem>())
            {
                var main = p.main;
                main.simulationSpeed = Info.GetAnimSpeed;
            }
        }
        public void StopTimeSpeed(bool isOn)
        {
            if (!Ps)
            {
                return;
            }
            if (isOn)
            {
                Ps.Pause(true);
            }
            else
            {
                Ps.Play(true);
            }
        }

        //设置起始位置
        private void SetPos()
        {
            if (transfromType == TransfromType.StartPos)
            {
                var anglePos = GetRePos(Info.castEuler, Info.castPos, eulerAngle, position);
                Tran.eulerAngles = anglePos.Item1;

                Tran.position = anglePos.Item2;
            }


            if (transfromType is TransfromType.OtherTransfrom)
            {
                if (Info.role.GetPonitCache(otherPointName, out Transform tf))
                {
                    //修改起点位置为tf.position
                    if (tf == null)
                    {
                        Debug.LogError($"--- otherPointName {otherPointName} = null");
                    }

                    var anglePos = GetRePos(Info.castEuler, tf.position, eulerAngle, position);
                    Tran.eulerAngles = anglePos.Item1;
                    Tran.position = anglePos.Item2;
                    Debug.Log($"--- reset Start pos {tf.name} {tf.position} add {position} = {anglePos.Item2}");
                }
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
            else
            {
                Tran.SetParent(Info.taskRunner.transform, true);
            }
            Tran.localScale = scale;
        }

        public static Vector3 GetOffset(Transform tf, Vector3 castPos)
        {
            var retAngle = tf.eulerAngles;
            var retPos = tf.position - castPos;
            return retPos;
        }

        public static (Vector3, Vector3) GetRePos(Vector3 castEuler, Vector3 castPos, Vector3 addAngle, Vector3 addPos)
        {
            var retAngle = castEuler + addAngle;
            //castPos + 偏移量 = castPos + Quaternion(方向) * localPos
            var retPos = castPos + Quaternion.Euler(castEuler) * addPos;
            return (castEuler, retPos);
        }

        //TODO 回收错误
        //回收时机: 特效帧结束就回收
        public void OnEnd()
        {
            if (State != XCState.Running)
            {
                return;
            }
            if (Ps)
            {
                Ps.Stop();
            }

            Tran.gameObject.SetActive(false);
            PoolMgr.Inst.Release(ObjectPath, Tran.gameObject);
            State = XCState.Stopped;
        }

        public void ForceClear()
        {
            OnEnd();
        }

    }

}