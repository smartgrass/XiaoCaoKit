using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<see cref="IClearCache"/>
public abstract class Singleton<T> where T : Singleton<T>, new()
{

    protected static T _instance;

    public static T Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new T();
                _instance.Init();
            }
            return _instance;
        }
    }

    protected Singleton()
    {
        if (_instance != null)
        {
            _instance = null;
            Debug.LogError("This " + (typeof(T)).ToString() + " Singleton Instance is not null !!!");
            throw new System.Exception("This " + (typeof(T)).ToString() + " Singleton Instance is not null !!!");
        }
    }

    protected virtual void Init()
    {
        //Nothing
    }

    public static void ClearCache()
    {
        if (_instance != null)
        {
            _instance = null;
        }
    }

}


//标记为需要主动清除, 定时清除
public interface IClearCache { }

public interface IHasClear { }

public interface IInit
{
    public void Init();
}

//管理器抽象
public interface IMgr { }
public interface IHelper { }