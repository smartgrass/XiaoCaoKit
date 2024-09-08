
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace XiaoCao
{
    /// <summary>
    /// 相当于System 无数据,RunTime时构建
    /// </summary>
    public class XCTaskRunner : MonoBehaviour
    {

        public TaskInfo debugInfo;

        public XCTask Task { get; set; }

        public bool IsBusy => CheckIsBusy(); //主Task占用中

        public bool IsBreak { get; set; } //受到打断

        public bool IsAllStop { get; set; } //全部task结束

        ///<see cref="Role.OnSkillFinish"/>  主Task结束时执行
        public UnityEvent<XCTaskRunner> onMainEndEvent = new UnityEvent<XCTaskRunner>(); //正常完成时触发

        ///<see cref="Role.OnTaskEnd"/>
        public UnityEvent<XCTaskRunner> onAllTaskEndEvent = new UnityEvent<XCTaskRunner>(); //所有Task不执行时触发


        public static AssetPool runnerPool;

        public static XCTaskRunner CreatNew(int skillId, int roleId, TaskInfo info)
        {
            XCTaskData data = SkillDataMgr.Get(skillId, roleId);
            if (data == null)
            {
                return null;
            }
            info.speed = data.speed;
            info.skillId = skillId;
            return CreatNewByData(data, info);
        }
        /// <summary>
        /// 执行一个Task
        /// </summary>
        public static XCTaskRunner CreatNewByData(XCTaskData data, TaskInfo info)
        {
            if (runnerPool == null)
            {
                GameObject go = new GameObject($"Runner_{info.skillId}");
                go.AddComponent<XCTaskRunner>();
                runnerPool = new AssetPool(go);
            }
            //使用对象池
            GameObject gameObject = runnerPool.Get();
            XCTaskRunner runner = gameObject.GetComponent<XCTaskRunner>();
            runner.Init(data, info);
            return runner;
        }
        public void Init(XCTaskData data, TaskInfo info)
        {
            //Debug.Log($"--- skill {info.skillId} {data}");
            //深复制
            var newData = SerializationUtility.CreateCopy(data) as XCTaskData;

            Task = XCTask.CreatTask(newData, info);
            newData.IsMainTask = true;
            Task.Runner = this;
            IsAllStop = false;
            Task.StartRun();

            this.debugInfo = info;
        }

        public void OnUpdate()
        {
            if (Task == null)
                return;
            Debug.Log($"--- OnEventUpdate ");
            Task.OnEventUpdate();
        }

        public void AllEnd()
        {
            OnEnd();
            IsAllStop = true;
            Task = null;
            gameObject.SetActive(false);
            Debug.Log($"--- AllEnd {gameObject} ");
            runnerPool.Release(gameObject);
        }
        //角色恢复自由控制时触发
        public void OnNoBusy()
        {
            if (!Task.IsNoBusyFlag)
            {
                Task.IsNoBusyFlag = true;
                if (onMainEndEvent != null)
                {
                    onMainEndEvent.Invoke(this);
                    onMainEndEvent.RemoveAllListeners();
                }
            }
        }

        public void SetBreak()
        {
            if (IsBusy)
            {
                IsBreak = false;
                //主技能中断
                Task.SetBreak();

            }
        }
        private void OnEnd()
        {
            if (onAllTaskEndEvent != null)
            {
                onAllTaskEndEvent.Invoke(this);
                onAllTaskEndEvent.RemoveAllListeners();
            }
        }

        private bool CheckIsBusy()
        {
            if (IsAllStop)
            {
                return false;
            }
            if (IsBreak)
            {
                return false;
            }
            return Task.IsBusy;
        }
    }

    public class TaskInfo
    {
        public Role role;
        public Transform playerTF;
        internal Animator playerAnimator;
        //针对当前对象, 可以是玩家或技能物体
        public GameObject curGO;
        public Transform curTF;

        public int entityId;
        public int skillId;  //如100

        //释放技能时记录角度 和 位置
        public Vector3 castEuler;

        public Vector3 castPos;

        public float speed = 1;
    }

    public class SkillDataMgr : Singleton<SkillDataMgr>, IClearCache
    {
        public Dictionary<int, XCTaskData> dataCache = new Dictionary<int, XCTaskData>();

        public static XCTaskData Get(int skillId, int raceId)
        {
            int idKey = skillId + raceId * 1000;
            if (Inst.dataCache.TryGetValue(idKey, out XCTaskData data))
            {
                return data;
            }

            //需要表做什么事?  技能类型, 技能图标 ,cd
            byte[] bytes = ResMgr.LoadRawByte(XCPathConfig.GetSkillDataPath(raceId.ToString(), skillId));
            XCTaskData task = OdinSerializer.SerializationUtility.DeserializeValue<XCTaskData>(bytes, DataFormat.Binary);
            Inst.dataCache.Add(idKey, task);
            return task;
        }
    }

}