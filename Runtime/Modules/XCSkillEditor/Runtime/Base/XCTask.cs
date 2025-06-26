using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{

    public class XCTask
    {
        public XCTaskRunner Runner { get; set; }

        public XCTaskData data;

        public bool IsMainTask => data.IsMainTask;

        public ObjectData ObjectData => data.objectData;

        public List<XCEvent> _events => data._events;

        //加入
        public List<XCTask> subTasks = new List<XCTask>();

        public XCState State { get; set; }

        //标记为不占用, 但可以继续Running
        public bool IsNoBusyFlag { get; set; }

        public bool IsPlayer => Info.role.IsPlayer;

        public bool IsBusy => State == XCState.Running && !IsNoBusyFlag;

        public Transform GetBindTranfrom()
        {
            if (IsMainTask)
            {
                return Info.playerTF;
            }
            else
            {
                return ObjectData.Tran;
            }
        }


        public TaskInfo Info;

        public int _endFrame = 0;

        private float _curTime = 0;

        private int _curFrame = 0;

        private int _startEventIndex = 0; //记录队头

        private int _startSubTrackIndex = 0;

        private int _finshiCout = 0;

        public int GetCurFrame => _curFrame;

        public static XCTask CreatTask(XCTaskData data, TaskInfo info)
        {
            XCTask task = new XCTask();
            task.data = data;
            task.Info = info;
            return task;
        }

        /// <summary>
        /// 任务创建时执行, 子任务在触发帧才执行
        /// </summary>
        public void StartRun(float startTime = 0)
        {
            data.HasTrigger = true;
            Info.taskRunner = Runner;
            data.objectData?.OnTrigger(Info);
            State = XCState.Running;
            _startSubTrackIndex = 0;
            _startEventIndex = 0;
            _curTime = startTime;
            _finshiCout = 0;
            //筛选数据
            _endFrame = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                _endFrame = Math.Max(_events[i].End, _endFrame);
                _events[i].task = this;
            }
        }

        public void OnEventUpdate()
        {

            float deltaTime = XCTime.deltaTime * Info.GetTimeSpeed;

            _curTime += deltaTime;//Mathf.Clamp( time, 0, LengthTime );
            //帧数是用时间累加计算出来的
            //delta不是稳定的
            //当前的1帧,指的的是动画帧,即1/30s,而不是update的一帧
            _curFrame = Mathf.FloorToInt(_curTime * XCSetting.FrameRate);
            //Debug.Log("  _curFrame " + _currentFrame + "_curTime "+ _currentTime + " curEvent" + _currentEvent);

            UpdateMainEvent();

            UpdateSubTask();
        }

        private void UpdateMainEvent()
        {

            if (State != XCState.Running)
                return;

            int len = _events.Count;

            for (int i = _startEventIndex; i < len; i++)
            {
                var e = _events[i];
                if (e.State == XCState.Sleep)
                {
                    if (_curFrame >= e.Start)
                    {
                        e.OnTrigger(_curTime - _events[i].StartTime);
                    }
                    else
                    {
                        break;
                    }
                }

                //Trigger和Update同时执行
                if (e.State == XCState.Running)
                {
                    if (_curFrame < e.range.End)
                    {
                        e.UpdateEvent(_curFrame, _curTime - _events[i].StartTime);
                    }
                    else
                    {
                        //结束帧/超出帧
                        e.UpdateEvent(_curFrame, _events[i].LengthTime);
                        e.OnFinish();
                        _finshiCout++;

                        //裁剪
                        if (_events[_startEventIndex].End < _curFrame)
                        {
                            _startEventIndex = Math.Min(_startEventIndex + 1, len - 1);
                        }
                    }
                }
            }

            if (_curFrame >= _endFrame)
            {
                StopMain();
            }
        }

        private void StopMain()
        {
            //主Task结束
            if (IsMainTask)
            {
                State = XCState.Stopped;
                SetNoBusy();
                return;
            }

            if (ObjectData != null)
            {
                if (_curFrame >= ObjectData.endFrame && ObjectData.HasStart)
                {
                    State = XCState.Stopped;
                    ObjectData.OnEnd();
                }
            }
            else
            {
                State = XCState.Stopped;
                Debug.LogError("--- no ObjectDataEnd ?");
            }
        }

        void UpdateSubTask()
        {
            int subLen = data.subDatas == null ? 0 : data.subDatas.Count;

            for (int i = _startSubTrackIndex; i < subLen; i++)
            {
                var subData = data.subDatas[i];
                if (!subData.HasTrigger)
                {
                    if (subData.objectData.startFrame <= _curFrame)
                    {
                        var subTask = CreatTask(subData, Info);
                        subTask.Runner = Runner;
                        subTasks.Add(subTask);
                        subTask.StartRun(_curTime);
                        _startSubTrackIndex = i + 1;
                    }
                    else
                    {
                        //subTask 进行过排序, 所以后面不需要检测
                        break;
                    }
                }
            }

            bool subFinish = true;
            //代替Runner执行
            int len = subTasks.Count;
            for (int i = 0; i < len; i++)
            {
                subTasks[i].OnEventUpdate();
                if (subTasks[i].State == XCState.Running)
                {
                    subFinish = false;
                }
            }

            if (IsMainTask && subFinish && State == XCState.Stopped)
            {
                Runner.AllEnd();
            }
        }


        public void SetNoBusy()
        {
            //不占用角色任务
            Runner.OnNoBusy();
        }

        public void SetBreak()
        {
            Debug.Log($"--- SetBreak {Info.skillId}");
            //主技能中断
            StopMain();
            foreach (var e in _events)
            {
                if (e.State == XCState.Running)
                {
                    e.OnFinish();
                }
            }
            //子技能如果已经生成, 则继续执行
            foreach (var t in subTasks)
            {
                //为开始的停止
                if (t.State == XCState.Sleep)
                {
                    t.State = XCState.Stopped;
                }
                //已经执行的任务啥都不做
            }

        }

        internal void Show(bool show)
        {

        }

        public void StopTimeSpeed(bool isOn)
        {
            if (State != XCState.Running)
            {
                return;
            }
            ObjectData?.StopTimeSpeed(isOn);

            foreach (var e in subTasks)
            {
                e.StopTimeSpeed(isOn);
            }
        }
    }

}