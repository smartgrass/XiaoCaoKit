﻿using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using XiaoCao;

//流程,代码分块
//暂无并行需求
public abstract class ProcedureBase
{
    public abstract void Start();

    public bool IsFinish;


    public virtual bool LoadOnlyOnce => false;

    public bool IsReload
    {
        get {
            return ProcedureMgr.Inst.LoadedDic.Contains(GetType());
        }
    }

}

public class ProcedureMgr : Singleton<ProcedureMgr>
{

    protected override void Init()
    {
        base.Init();
    }

    public bool hasLoad => GameDataCommon.Current.IsHasLoad;

    public List<ProcedureBase> Procedures = new List<ProcedureBase>();

    public HashSet<Type> LoadedDic = new HashSet<Type>();

    public void AddTask(ProcedureBase p)
    {
        Procedures.Add(p);
    }

    public async UniTask Run()
    {
        foreach (var item in Procedures)
        {
            if (item.LoadOnlyOnce && LoadedDic.Contains(item.GetType()))
            {
                continue;
            }

            item.Start();
            while (!item.IsFinish)
            {
                await UniTask.Yield();
            }
            LoadedDic.Add(item.GetType());
        }
        Procedures.Clear();
        GameDataCommon.Current.IsHasLoad = true;
    }

}