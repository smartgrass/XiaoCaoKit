using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> where T : class, new()
{

    protected static T _instance = null;

    public static T Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new T();
            }
            return _instance;
        }
    }

    protected Singleton()
    {
        if(_instance!=null)
        {
            throw new System.Exception("This " + (typeof(T)).ToString() + " Singleton Instance is not null !!!");
        }

        Init();
    }

    protected virtual void Init()
    {

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
