using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

//流程: 与写静态方法区别不大,主要是可读性,代码分块,进度展示
public abstract class ProcedureBase
{
    public abstract void Start();

    public bool IsFinish;


    public virtual bool LoadOnlyOnce => false;

    public virtual float Cost => 10;

    public float waitCost;

    protected bool IsReload
    {
        get { return ProcedureMgr.Inst.LoadedDic.Contains(GetType()); }
    }

    public void AddFinishCost(float cost)
    {
        waitCost -= cost;
        ProcedureMgr.Inst.curFinishCost += cost;
    }

    public void End()
    {
        if (waitCost > 0)
        {
            AddFinishCost(waitCost);
        }
    }
}

public class ProcedureMgr : Singleton<ProcedureMgr>
{
    protected override void Init()
    {
        base.Init();
    }

    public List<ProcedureBase> Procedures = new List<ProcedureBase>();

    public HashSet<Type> LoadedDic = new HashSet<Type>();

    public float totalCost;

    public float curFinishCost;

    public bool isFinish;

    public void AddTask(ProcedureBase p)
    {
        Procedures.Add(p);
    }

    public async UniTask Run()
    {
        isFinish = false;
        foreach (var item in Procedures)
        {
            totalCost += item.Cost;
            item.waitCost = item.Cost;
        }

        foreach (var item in Procedures)
        {
            if (item.LoadOnlyOnce && LoadedDic.Contains(item.GetType()))
            {
                continue;
            }

            DebugCostTime.StartTime(2);
            item.Start();
            while (!item.IsFinish)
            {
                await UniTask.Yield();
            }

            item.End();

            DebugCostTime.StopTime(item.ToString(), 2);
            LoadedDic.Add(item.GetType());
        }

        Procedures.Clear();
        isFinish = true;
    }

    public float GetProcess()
    {
        if (totalCost == 0)
        {
            return 0;
        }

        return curFinishCost / totalCost;
    }
}


public static class ProcedureMgrExtend
{
    public static ProcedureMgr InitOnce(this ProcedureMgr procedureMgr)
    {
        procedureMgr.AddTask(new ConfigProcedure());
        procedureMgr.AddTask(new PlayerDataProcedure());
        procedureMgr.AddTask(new ResProcedure());
        return procedureMgr;
    }
}