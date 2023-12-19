
using OdinSerializer;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace XiaoCao
{
    /// <summary>
    /// 相当于System 无数据,RunTime时构建
    /// </summary>
    public class XCTaskRunner : MonoBehaviour
    {
        public XCTask Task { get; set; }

        public UnityEvent onFinishEvent = new UnityEvent();

        public static AssetPool runnerPool;


        public void Example()
        {
            //XCTaskData data = ResMgr.Inst.
        }

        /// <summary>
        /// 执行一个Task
        /// </summary>
        public static XCTaskRunner CreatNewRunner(XCTaskData data, TaskInfo info)
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
            runner.Init(data,info);
            return runner;
        }
        public void Init(XCTaskData data, TaskInfo info)
        {
            Task = XCTask.CreatTask(data);
            Task.Runner = this;
            Task.Info = info;
            //深复制
            var newData = SerializationUtility.CreateCopy(data) as XCTaskData;
            Task.data = newData;
            Task.TaskStart();
        }

        private void Update()
        {
            if (Task == null)
                return;
            Task.OnEventUpdate();
        }

        public void AllFinsh()
        {
            Task = null;
            gameObject.SetActive(false);
        }
    }

    public class TaskInfo
    {
        public Transform playerTF;

        public Transform transform;
        public GameObject gameObject;

        public int skillID;  //如100

        //释放技能时记录角度 和 位置
        public Vector3 castEuler;

        public Vector3 castPos;

        public float speed = 1;
    }

    public class SkillDataMgr: Singleton<SkillDataMgr>, IClearCache
    {
        //cache
        public Dictionary<string, XCTaskData> dataCache;

    }

}