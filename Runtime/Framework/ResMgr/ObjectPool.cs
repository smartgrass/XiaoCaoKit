using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace XiaoCao
{
    /// <summary>
    /// 模仿 UnityEngine.Pool.ObjectPool
    /// </summary>
    public class ObjectPool<T> : IDisposable where T : class
    {
        internal readonly List<T> m_List;

        private readonly Func<T> m_CreateFunc;

        private readonly Action<T> m_ActionOnGet;

        private readonly Action<T> m_ActionOnRelease;

        private readonly Func<T, int> m_ActtionId;

        public int CountAll { get; private set; }

        public ObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Func<T, int> ActtionId = null, int defaultCapacity = 10)
        {
            if (createFunc == null)
            {
                throw new ArgumentNullException("createFunc");
            }

            m_List = new List<T>(defaultCapacity);
            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_ActtionId = ActtionId;
        }

        public T Get()
        {
            T val;
            if (m_List.Count == 0)
            {
                val = m_CreateFunc();
                CountAll++;
            }
            else
            {
                int index = m_List.Count - 1;
                val = m_List[index];
                m_List.RemoveAt(index);
            }

            m_ActionOnGet?.Invoke(val);
            return val;
        }

        public void Release(T element)
        {
            if (!m_List.Contains(element))
            {
                m_List.Add(element);
                m_ActionOnRelease?.Invoke(element);
            }
        }

        public void Clear()
        {
            m_List.Clear();
            CountAll = 0;
        }

        public void Dispose()
        {
            Clear();
        }

        internal void ClearNull()
        {

        }
    }
}