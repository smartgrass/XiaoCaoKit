using System;
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

        public BehaviorLifeState lifeState;

        public bool IsExist => lifeState >= BehaviorLifeState.BehaviorInited;


        private bool enable;
        //是否有效
        public bool IsRuning => IsExist && enable && gameObject;

        public bool Enable
        {
            get { return enable; }
            set
            {
                bool isChange = enable != value;
                enable = value;

                if (isChange && lifeState > BehaviorLifeState.None)
                {
                    OnEnable(value);
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
            lifeState = BehaviorLifeState.BehaviorInited;
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
            lifeState = BehaviorLifeState.Destroy;
        }

        public void DestroySelf()
        {
            GameObject.Destroy(gameObject);
        }

        

        //权限范围
        public enum BehaviorLifeState
        {
            None = 0,
            Destroy = 1,
            WillDestroy = 2,
            BehaviorInited = 3,


        }

    }

}