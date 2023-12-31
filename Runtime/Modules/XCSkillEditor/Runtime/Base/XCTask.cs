﻿using OdinSerializer;
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

        public ObjectData ObjectData => data.objectData;

        public List<XCEvent> _events => data._events;

        //加入
        public List<XCTask> subTasks = new List<XCTask>();

        public XCState State { get; set; }

        public TaskInfo Info;

        public int _endFrame = 0;

        private float _curTime = 0;

        private int _curFrame = 0;

        private int _startEventIndex = 0; //记录队头

        private int _startSubTrackIndex = 0;

        private int _finshiCout = 0;

        public static XCTask CreatTask(XCTaskData data)
        {
            XCTask task = new XCTask();
            return task;
        }

        /// <summary>
        /// 任务创建时执行, 子任务在触发帧才执行
        /// </summary>
        public void TaskStart()
        {
            Debug.Log($"yns TaskStart");
            data.HasTrigger = true;
            data.objectData.OnTrigger(Info);
            State = XCState.Running;
            _startSubTrackIndex = 0;
            _startEventIndex = 0;
            _curTime = 0;
            _finshiCout = 0;
            //筛选数据
            _endFrame = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                _endFrame = Math.Max(_events[i].End, _endFrame);
            }
        }

        void UpdateSubTask()
        {
            int subLen = data.subDatas.Count;
            for (int i = _startSubTrackIndex; i < subLen; i++)
            {
                var subData = data.subDatas[i];
                if (!subData.HasTrigger)
                {
                    if (subData.start >= _curFrame)
                    {
                        var subTask = CreatTask(subData);
                        subTasks.Add(subTask);
                        subTask.TaskStart();
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

            if (subFinish)
            {
                Runner.AllFinsh();
            }
        }

        public void OnEventUpdate()
        {

            float deltaTime = FluxTime.deltaTime;

            _curTime += deltaTime;//Mathf.Clamp( time, 0, LengthTime );
            //帧数是用时间累加计算出来的
            //delta不是稳定的
            //当前的1帧,指的的是动画帧,即1/30s,而不是update的一帧
            _curFrame = Mathf.FloorToInt(_curTime * XCSetting.FrameRate);
            //Debug.Log("yns  _curFrame " + _currentFrame + "_curTime "+ _currentTime + " curEvent" + _currentEvent);

            ObjectData.OnFrameUpdate(_curFrame);

            UpdateEvent();

            UpdateSubTask();
        }

        private void UpdateEvent()
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
                        e.OnStart();
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
                        e.UpdateEvent(_curFrame, _events[i].EndTime);
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
                Runner.onFinishEvent?.Invoke();
                State = XCState.Stopped;
            }
        }

        internal void Show(bool show)
        {

        }
    }

}