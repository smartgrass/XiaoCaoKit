using System;
using UnityEngine;
namespace XiaoCao
{


    [Serializable]
    public class XCEvent
    {
        public const int FrameRate = 30;

        public const float FramePerSec = 1f / FrameRate;
        public XCEvent() { }
        public XCEvent(XCRange range)
        {
            this.range = range;
        }
        [SerializeField]
        public XCRange range = new XCRange();

        public string eName;


        public bool isLocalOnly = false;
        public bool isPlayerOnly = false;

        public XCTask task;
        public XCState State { get; set; }
        public TaskInfo Info => task.Info;
        public Transform Tran => task.GetBindTranfrom();

        #region TODO
        [NonSerialized]
        private bool _hasTriggered = false;
        public bool HasTrigger { get { return State != XCState.Sleep; } }

        [NonSerialized]
        protected bool _hasFinished = false;
        public bool HasFinished { get { return _hasFinished; } }
        public void SetFinished()
        {
            _hasFinished = true;
            State = XCState.Stopped;
        }
        #endregion

        public int Start
        {
            get { return range.Start; }
            set { range.Start = value; }
        }
        public int End
        {
            get { return range.End; }
            set { range.End = value; }
        }

        public float StartTime
        {
            get { return range.Start * XCSetting.FramePerSec; }
        }

        /// @brief What this the event ends.
        /// @note This value isn't cached.
        public float EndTime
        {
            get { return range.End * XCSetting.FramePerSec; }
        }
        public float LengthTime
        {
            get { return range.Length * XCSetting.FramePerSec; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startOffsetTime">超出触发时间的偏移量,一般用不上</param>
        public virtual void OnTrigger(float startOffsetTime)
        {
            State = XCState.Running;
        }
        public virtual void OnFinish() 
        {
            State = XCState.Stopped;        
        }
        public virtual void OnUpdateEvent(int frame, float timeSinceTrigger) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="timeSinceTrigger">事件触发后的相对时间,从事件的strat帧作为0开始计时</param>
        public void UpdateEvent(int frame, float timeSinceTrigger)
        {
            OnUpdateEvent(frame, timeSinceTrigger);
        }
    }

    public enum XCState
    {
        Sleep, //未激活
        Running, //已激活,执行中
        Stopped  //结束/完成
    }

    [Serializable]
    public struct XCRange
    {
        // start frame
        [SerializeField]
        private int _start;

        // end frame
        [SerializeField]
        private int _end;

        /// @brief Returns the start frame.
        public int Start
        {
            get { return _start; }
            set
            {
                _start = value;
            }
        }

        /// @brief Returns the end frame.
        public int End
        {
            get { return _end; }
            set
            {
                _end = value;
            }
        }

        public int Length { set { End = _start + value; } get { return _end - _start; } }

        public XCRange(int start, int end)
        {
            this._start = start;
            this._end = end;
        }
        public int Cull(int i)
        {
            return Mathf.Clamp(i, _start, _end);
        }

        /// @brief Returns if \e i is inside [start, end], i.e. including borders
        public bool Contains(int i)
        {
            return i >= _start && i <= _end;
        }

        /// @brief Returns if \e i is inside ]start, end[, i.e. excluding borders
        public bool ContainsExclusive(int i)
        {
            return i > _start && i < _end;
        }

        /// @brief Returns if the ranges intersect, i.e. touching returns false
        /// @note Assumes They are both valid
        public bool Collides(XCRange range)
        {
            return _start < range._end && _end > range._start;
            //			return (range.start > start && range.start < end) || (range.end > start && range.end < end );
        }

        /// @brief Returns if the ranges overlap, i.e. touching return true
        /// @note Assumes They are both valid
        public bool Overlaps(XCRange range)
        {
            return range.End >= _start && range.Start <= _end;
        }
        public override string ToString()
        {
            return string.Format("[{0}; {1}]", _start, _end);
        }
    }

}