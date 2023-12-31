﻿using System;
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

        public bool hasAwake;

        private bool enable;
        public bool IsRuning => hasAwake && enable && gameObject;

        public bool Enable
        {
            get { return enable; }
            set
            {
                if (hasAwake && enable != value)
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
            Debug.Log($"--- OnCreat {id}");
            UpdateEvent += OnUpdate;
            FixedUpdateEvent += OnFixedUpdate;
            LaterUpdateEvent += OnLaterUpdate;
            DestroyEvent += OnDestroy;
            Awake();
        }

        protected virtual void Awake()
        {
            hasAwake = true;
            Debug.Log($"--- Awake");
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