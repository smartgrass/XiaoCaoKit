
using OdinSerializer;
using OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Events;

namespace XiaoCao
{
    /// <summary>
    /// 相当于System 无数据,RunTime时构建
    /// </summary>
    public class XCTaskRunner : MonoBehaviour, IClearCache
    {

        public TaskInfo debugInfo;

        public XCTask Task { get; set; }

        public bool IsBusy => CheckIsBusy(); //主Task占用中

        public bool IsBreak { get; set; } //受到打断

        public bool IsAllStop { get; set; } //全部task结束

        public bool IsTimeStop { get; set; }

        ///<see cref="Role.OnSkillFinish"/>  主Task结束时执行
        public UnityEvent<XCTaskRunner> onMainEndEvent = new UnityEvent<XCTaskRunner>(); //正常完成时触发

        ///<see cref="Role.OnTaskEnd"/>
        public UnityEvent<XCTaskRunner> onAllTaskEndEvent = new UnityEvent<XCTaskRunner>(); //所有Task不执行时触发


        [StaticCache]
        public static AssetPool runnerPool;

        private void Awake()
        {
            GameEvent.AddEventListener<GameState, GameState>(EGameEvent.GameStateChange.Int(), GameStateChange);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<GameState, GameState>(EGameEvent.GameStateChange.Int(), GameStateChange);
        }

        public static XCTaskRunner CreatNew(string skillId, int roleId, TaskInfo info)
        {
            XCTaskData data = SkillDataMgr.Get(skillId, roleId);
            if (data == null)
            {
                return null;
            }
            info.baseSpeed = data.speed;
            info.skillId = skillId;
            return CreatNewByData(data, info);
        }
        /// <summary>
        /// 执行一个Task
        /// </summary>
        public static XCTaskRunner CreatNewByData(XCTaskData data, TaskInfo info)
        {
            PreInitPool();
            //使用对象池
            GameObject gameObject = runnerPool.Get();
            gameObject.name = $"Runner_{info.entityId}";
            XCTaskRunner runner = gameObject.GetComponent<XCTaskRunner>();
            runner.Init(data, info);
            return runner;
        }

        public static void PreInitPool()
        {
            if (runnerPool == null || !runnerPool.prefab)
            {
                GameObject go = new GameObject($"Runner_Pre");
                go.AddComponent<XCTaskRunner>();
                runnerPool = new AssetPool(go);
            }
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
            if (IsTimeStop)
                return;

            Task.OnEventUpdate();
        }

        public void AllEnd()
        {
            OnEnd();
            IsAllStop = true;
            Task = null;
            gameObject.SetActive(false);
            Debuger.Log($"--- AllEnd {gameObject} ");
        }

        public static void AllEnd2(XCTaskRunner runner)
        {
            runnerPool.Release(runner.gameObject);
        }


        //角色恢复自由控制时触发
        public void OnNoBusy()
        {
            if (Task.IsNoBusyFlag)
            {
                return;
            }

            Task.IsNoBusyFlag = true;
            if (onMainEndEvent != null)
            {
                onMainEndEvent.Invoke(this);
                onMainEndEvent.RemoveAllListeners();
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

        public void StopTimeSpeed(bool isOn)
        {
            if (!IsAllStop)
            {
                IsTimeStop = isOn;
                //停止特效
                Task.StopTimeSpeed(isOn);
            }
        }

        private void GameStateChange(GameState state1, GameState state2)
        {
            if (state2 == GameState.Exit)
            {
                if (Task != null && Task.State == XCState.Running)
                {
                    if (Task.ObjectData != null)
                    {
                        Debug.Log($"--- ForceClear");
                        Task.ObjectData.ForceClear();
                    }
                    GameObject.Destroy(gameObject);
                }
            }
        }
    }

    ///<see cref="AtkInfo"/>
    public class TaskInfo
    {
        public Role role;
        public Transform playerTF;
        internal Animator playerAnimator;

        public int entityId;
        public string skillId;  //如100

        //释放技能时记录角度 和 位置
        public Vector3 castEuler;

        public Vector3 castPos;

        public float GetSpeed => baseSpeed * speedMult;


        public float baseSpeed;

        public float speedMult = 1;


        public XCTaskRunner taskRunner;
    }

    public class TaskInfoTags
    {
        public static string Slash = "Slash";

    }

    public class SkillDataMgr : Singleton<SkillDataMgr>, IClearCache
    {
        public Dictionary<string, XCTaskData> dataCache = new Dictionary<string, XCTaskData>();

        public static XCTaskData Get(string skillId, int raceId)
        {

            string idKey = $"{raceId}_{skillId}";
            if (Inst.dataCache.TryGetValue(idKey, out XCTaskData data))
            {
                return data;
            }

            //需要表做什么事?  技能类型, 技能图标 ,cd
            byte[] bytes = ResMgr.LoadRawByte(XCPathConfig.GetSkillDataPath(raceId.ToString(), skillId).LogStr("--"));
            if (bytes == null)
            {
                return null;
            }
            XCTaskData task = OdinSerializer.SerializationUtility.DeserializeValue<XCTaskData>(bytes, XCPathConfig.RawFileFormat);
            Inst.dataCache.Add(idKey, task);
            return task;
        }
    }

}