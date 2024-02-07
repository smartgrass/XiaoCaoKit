using UnityEngine;

public abstract class MonoSingletonPrefab<T> : MonoBehaviour where T : MonoSingletonPrefab<T>
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
                _instance = LoadMonoSingleton();
                GameObject go = _instance.gameObject;
                go.transform.SetParent(DDOLGO.transform, false);
                _instance.Init();
            }
            return _instance;
        }
    }


    public static T LoadMonoSingleton()
    {
        string prefabPath = $"MonoSingleton/{typeof(T).Name}";
        Debug.Log(prefabPath);
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"no prefab {prefabPath}");
            return new GameObject(typeof(T).Name).AddComponent<T>();
        }

        GameObject go = Instantiate(prefab);
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