using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{

    protected static T _instance = null;

    public static T Inst
    {
        get
        {
            if (!Application.isPlaying)
                return null;

            if (_instance == null)
            {
                GameObject DDOLGO = GameObject.Find("DontDestroyOnLoadGO");
                if (DDOLGO == null)
                {
                    DDOLGO = new GameObject("DontDestroyOnLoadGO");
                    DontDestroyOnLoad(DDOLGO);
                }

                GameObject go = new GameObject(typeof(T).ToString());
                go.transform.SetParent(DDOLGO.transform, false);
                _instance= go.AddComponent<T>();
                _instance.Init();
            }
            return _instance;
        }
    }

   public virtual void Init()
    {

    }

    public static void ClearSelf()
    {
        if (_instance != null)
        {
            GameObject.Destroy(_instance.gameObject);
            _instance = null;  
        }
    }

}


public abstract class MonoSingletonPrefab<T> :MonoBehaviour where T : MonoSingletonPrefab<T>
{
    protected static T _instance = null;

    public static T Inst
    {
        get
        {
            if (!Application.isPlaying)
                return null;

            if (_instance == null)
            {
                GameObject DDOLGO = GameObject.Find("DontDestroyOnLoadGO");
                if (DDOLGO == null)
                {
                    DDOLGO = new GameObject("DontDestroyOnLoadGO");
                    DontDestroyOnLoad(DDOLGO);
                }
                _instance = LoadMonoSingleton<T>();
                GameObject go = _instance.gameObject;
                go.transform.SetParent(DDOLGO.transform, false);
                _instance.Init();
            }
            return _instance;
        }
    }


    public static T LoadMonoSingleton<T>()
    {
        string prefabPath = $"MonoSingleton/{typeof(T).Name}";
        Debug.Log(prefabPath);
        var go = Resources.Load<GameObject>(prefabPath);
        var _instance = go.GetComponent<T>();
        return _instance;
    }

    public virtual void Init()
    {

    }

    public static void ClearSelf()
    {
        if (_instance != null)
        {
            GameObject.Destroy(_instance.gameObject);
            _instance = null;
        }
    }


}