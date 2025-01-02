using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using XiaoCao.Buff;

namespace XiaoCao
{
    public class BuffBinder : Singleton<BuffBinder>
    {
        private static readonly Dictionary<EBuff, Type> buffEffectDic = new Dictionary<EBuff, Type>();

        protected override void Init()
        {
            base.Init();
            Bind();
        }

        private void Bind()
        {
            Assembly assembly = typeof(IBuffEffect).Assembly;
            Type drawType = typeof(IBuffEffect);
            foreach (Type type in assembly.GetTypes())
            {
                if (drawType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                {
                    Debug.Log($"--- type {type}");
                    IBuffEffect instance = (IBuffEffect)Activator.CreateInstance(type);
                    buffEffectDic.Add(instance.Buff, type);
                    //gc
                }
            }
        }

        public IBuffEffect GetOrCreateInstance(EBuff eBuff)
        {
            if (!buffEffectDic.ContainsKey(eBuff))
            {
                return null;
            }
            else
            {
                var type = buffEffectDic[eBuff];
                IBuffEffect instance = (IBuffEffect)Activator.CreateInstance(type);
                return instance;
            }
        }
    }
}
