using System;
using Unity.VisualScripting;
using UnityEngine;

namespace XiaoCao
{
    public class BehaviorEntity : Entity
    {
        public Action UpdateEvent;
        public Action FixedUpdateEvent;
        public Action LaterUpdateEvent;
        public Action DestroyEvent;

        public override EntityBaseType BindType => EntityBaseType.BehaviorEntity;
        public float DeltaTime => Time.deltaTime;
        public float FixedDeltaTime => Time.fixedDeltaTime;

        public bool isCreated;

        private bool enable;
        //是否有效
        public bool IsRuning => isCreated && enable && gameObject;

        public bool Enable
        {
            get { return enable; }
            set
            {
                if (isCreated && enable != value)
                {
                    enable = value;
                    OnEnable(value);
                }
                else
                {
                    enable = value;
                }
            }
        }

        public void OnCreat()
        {
            Debuger.Log($"--- OnCreat {id}");
            UpdateEvent += OnUpdate;
            FixedUpdateEvent += OnFixedUpdate;
            LaterUpdateEvent += OnLaterUpdate;
            DestroyEvent += OnDestroy;
            isCreated = true;
            Awake();
        }

        /// <summary>
        /// 创建后就执行, 即使没有GameObject
        /// </summary>
        protected virtual void Awake()
        {
            Debuger.Log($"--- Awake");
        }
        protected virtual void OnEnable(bool isEnable)
        {

        }

        protected virtual void OnLaterUpdate()
        {
        }

        protected virtual void OnFixedUpdate()
        {

        }

        protected virtual void OnUpdate()
        {

        }

        protected virtual void OnDestroy()
        {

        }

    }

}