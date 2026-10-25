using UnityEngine;

public abstract class MonoSingletonPrefab<T> : MonoBehaviour where T : MonoSingletonPrefab<T>
{
    public virtual bool NeedDontDestroy => true;

    protected static T _instance = null;

    //正在创建实例的标记: 预制体上的组件在 Awake/OnEnable 里反向访问 Inst 时, 用它阻断递归
    private static bool _isCreating;

    /// <summary>
    /// 实例是否已经创建。只读判断, 不会像 <see cref="Inst"/> 那样在未创建时自动加载预制体。
    /// 需要"有没有"时用它, 不要写 Inst != null。
    /// </summary>
    public static bool HasInst => _instance != null;

    /// <summary>
    /// 实例是否已经跑完 Init()。创建过程中(预制体内组件的 Awake/OnEnable 阶段)为 false,
    /// 这些组件要等它变 true 再初始化, 否则会读到还没配置好的单例状态。
    /// </summary>
    public static bool IsInited { get; private set; }

    public static T Inst
    {
        get
        {
            if (!Application.isPlaying)
                return null;

            if (_instance == null)
            {
                if (_isCreating)
                {
                    Debug.LogError($"[Singleton] {typeof(T).Name} 正在创建中, 不要在 Awake/OnEnable 里调用 Inst");
                    return null;
                }

                _isCreating = true;
                try
                {
                    CreateInst();
                }
                finally
                {
                    _isCreating = false;
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 创建实例: 先给 _instance 赋值, 再让对象进入激活层级触发子组件的 Awake/OnEnable,
    /// 最后调 Init()。这样预制体内的面板在 OnEnable 里访问 Inst 时拿到的是正在创建的这个实例,
    /// 不会因为 _instance 还是 null 而重新走一遍创建流程(以前会无限递归到栈溢出)。
    /// </summary>
    private static void CreateInst()
    {
        T instance = LoadMonoSingleton();
        if (instance == null)
        {
            return;
        }

        _instance = instance;
        GameObject go = instance.gameObject;

        if (instance.NeedDontDestroy)
        {
            GameObject DDOLGO = GameObject.Find("DontDestroyOnLoadGO");
            if (DDOLGO == null)
            {
                DDOLGO = new GameObject("DontDestroyOnLoadGO");
                DontDestroyOnLoad(DDOLGO);
            }
            go.transform.SetParent(DDOLGO.transform, false);
        }
        else
        {
            //脱离临时父节点, 进入激活层级
            go.transform.SetParent(null, false);
        }

        instance.Init();
        IsInited = true;
    }


    public static T LoadMonoSingleton()
    {
        string prefabPath = $"MonoSingleton/{typeof(T).Name}";
        //重要节点日志: 单例创建只走一次, 保留用于确认实例化时机
        Debug.Log(prefabPath);
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"no prefab {prefabPath}");
            return new GameObject(typeof(T).Name).AddComponent<T>();
        }

        //挂到未激活的临时节点下实例化: 预制体上组件的 Awake/OnEnable 会被推迟到激活时,
        //从而保证它们里面访问 Inst 时 _instance 已经赋值(直接在根节点实例化会立刻回调, 造成递归)
        GameObject holder = new GameObject($"__{typeof(T).Name}_Creating");
        holder.SetActive(false);

        GameObject go = Instantiate(prefab, holder.transform);
        T instance = go.GetComponent<T>();
        if (instance == null)
        {
            Debug.LogError($"{prefabPath} 上没有 {typeof(T).Name} 组件");
        }

        Destroy(holder);
        return instance;
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
            IsInited = false;
        }
    }


}
