
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
        public XCTask Task { get; set; }

        private bool IsSuccees { get; set; } //是正常执行

        public UnityEvent<XCTaskRunner> onEndEvent = new UnityEvent<XCTaskRunner>(); //无论退出还是完成都会执行

        public UnityEvent<XCTaskRunner> onFinishEvent = new UnityEvent<XCTaskRunner>(); //正常完成时触发

        public static AssetPool runnerPool;

        public static XCTaskRunner CreatNew(int skillId, RoleType roleType, TaskInfo info)
        {
            XCTaskData data = SkillDataMgr.Get(skillId, RoleType.Player);
            info.speed = data.speed;
            return CreatNewByData(data, info);
        }
        /// <summary>
        /// 执行一个Task
        /// </summary>
        public static XCTaskRunner CreatNewByData(XCTaskData data, TaskInfo info)
        {
            if (runnerPool == null)
            {
                GameObject go = new GameObject();
                go.AddComponent<XCTaskRunner>();
                runnerPool = new AssetPool(go);
            }
            //使用对象池
            GameObject gameObject = runnerPool.pool.Get();
            XCTaskRunner runner = gameObject.GetComponent<XCTaskRunner>();
            runner.Init(data, info);
            return runner;
        }
        public void Init(XCTaskData data, TaskInfo info)
        {
            Debug.Log($"--- skill {info.skillId} {data}");
            //深复制
            var newData = SerializationUtility.CreateCopy(data) as XCTaskData;

            Task = XCTask.CreatTask(newData,info);
            newData.IsMainTask = true;
            Task.Runner = this;
            Task.StartRun();
        }

        private void Update()
        {
            if (Task == null)
                return;
            Task.OnEventUpdate();
        }

        public void AllFinsh()
        {
            if (IsSuccees)
            {
                onFinishEvent?.Invoke(this);
                onFinishEvent.RemoveAllListeners();
            }
            onEndEvent.Invoke(this);
            onEndEvent.RemoveAllListeners();
            Task = null;
            gameObject.SetActive(false);
            Debug.Log($"--- Release {gameObject} ");
            runnerPool.pool.Release(gameObject);
        }
    }

    public class TaskInfo
    {
        public Transform playerTF;
        internal Animator playerAnimator;
        //针对当前对象, 可以是玩家或技能物体
        public GameObject curGO;
        public Transform curTF;

        public int skillId;  //如100

        //释放技能时记录角度 和 位置
        public Vector3 castEuler;

        public Vector3 castPos;

        public float speed = 1;

    }

    public class SkillDataMgr : Singleton<SkillDataMgr>, IClearCache
    {
        public Dictionary<int, XCTaskData> dataCache = new Dictionary<int, XCTaskData>();

        public static XCTaskData Get(int skillId, RoleType roleType)
        {
            int idKey = skillId + (int)roleType * 1000;
            if (Inst.dataCache.TryGetValue(idKey, out XCTaskData data))
            {
                return data;
            }

            //需要表做什么事?  技能类型, 技能图标 ,cd
            byte[] bytes = ResMgr.LoadRawByte(XCPathConfig.GetSkillDataPath(roleType, skillId));
            XCTaskData task = OdinSerializer.SerializationUtility.DeserializeValue<XCTaskData>(bytes, DataFormat.Binary);
            Inst.dataCache.Add(idKey, task);
            return task;
        }
    }

}