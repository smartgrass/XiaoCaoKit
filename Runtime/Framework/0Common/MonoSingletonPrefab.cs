using UnityEngine;

public abstract class MonoSingletonPrefab<T> : MonoBehaviour where T : MonoSingletonPrefab<T>
{
    public virtual bool NeedDontDestroy => true;

    protected static T _instance = null;

    //正在创建实例的标记: 预制体上的组件在 Awake/OnEnable 里反向访问 Inst 时, 用它阻断递归
    private static bool _isCreating;

    /// <summary>
    /// 实例是否已经创建并且可用。只读判断, 不会像 <see cref="Inst"/> 那样在未创建时自动加载预制体。
    /// 需要"有没有"时用它, 不要写 Inst != null。
    /// </summary>
    public static bool HasInst => _instance != null;

    /// <summary>
    /// 实例是否已经跑完 Init()。面板初始化判断请用 <see cref="HasInst"/>:
    /// Init 执行期间本值仍为 false, 会把自己触发的子面板初始化一并挡掉。
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
    /// 创建实例: 预制体根节点预置为"未激活", 所以 Instantiate 不会触发 Awake/OnEnable;
    /// 先给 _instance 赋值, 再挂到目标父节点并 SetActive(true) 触发唤醒, 最后调 Init()。
    /// 这样预制体内组件在 Awake/OnEnable 里访问 Inst 拿到的就是本实例,
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

        //激活时才触发子组件的 Awake/OnEnable, 此时 _instance 已就位
        go.SetActive(true);
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
            //兜底实例也保持未激活, 由 CreateInst 统一激活, 保证 Awake 里能取到 Inst
            GameObject emptyGO = new GameObject(typeof(T).Name);
            emptyGO.SetActive(false);
            return emptyGO.AddComponent<T>();
        }

        GameObject go = Instantiate(prefab);
        T instance = go.GetComponent<T>();
        if (instance == null)
        {
            Debug.LogError($"{prefabPath} 上没有 {typeof(T).Name} 组件");
        }

        //预制体根节点必须是未激活状态, 否则 Awake/OnEnable 会在 _instance 赋值前就跑
        if (go.activeSelf)
        {
            Debug.LogWarning($"[Singleton] {prefabPath} 根节点是激活状态, Awake/OnEnable 早于 _instance 赋值; 请把预制体根节点取消激活");
        }

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
