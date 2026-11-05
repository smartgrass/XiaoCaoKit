using System.Collections;
using System.Threading.Tasks;
using GG.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace XiaoCao
{
    //与CameraController类似是在Scene里面预配置的
    public class MapMgr : MonoBehaviour, IMgr
    {
        //地图预制体目录
        public const string MapDir = "Assets/_Res/Map";

        //关卡敌人配置预制体目录, 与地图分开加载
        public const string LevelActionDir = "Assets/_Res/Map/LevelAction";

        //地图上的敌人配置挂载节点
        public const string LevelActionNodeName = "LevelAction";

        //敌人配置子物体名, 地图上可预置多个 LevelAction_xxx, 按配置只激活一个
        public const string LevelActionPrefix = "LevelAction";

        private static MapMgr _instance = null;

        public static MapMgr Inst => _instance;

        //地图已预置在场景里, 不从配置加载关卡地图(主城Home场景)
        public bool dontLoadMap;

        //仅编辑器直启时有效: 用这个关卡替换当前关卡
        public string testLevelName = "";

        public static string CurLevelName
        {
            get => GameDataCommon.Current.levelName;
            set => GameDataCommon.Current.levelName = value;
        }

        public LevelData LevelData
        {
            get => BattleData.Current.levelData;
        }

        public LevelControl LevelControl { get; set; }

        private void Awake()
        {
            _instance = this;
        }

        public void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public IEnumerator LoadLevelObject()
        {
            GetSetting();

            //换地图: 通道寻路图要按新地图的 PassPointCollector 重建
            PassPathFinder.Invalidate();

            //主城等场景的地图直接预置在场景里(dontLoadMap), 不需要从配置加载关卡地图
            if (dontLoadMap || GameDataCommon.Current.gameStage == EGameStage.Home)
            {
                Debug.Log($"--- skip load level map, stage {GameDataCommon.Current.gameStage} level {CurLevelName}");
                yield break;
            }

            if (string.IsNullOrEmpty(CurLevelName))
            {
                yield break;
            }

            //开始加载新关卡前清空上一关还没执行的敌人生成请求
            EnemySpawnQueue.Clear();

            HideOtherLevel();

            Transform tf = transform.Find(CurLevelName);
            if (tf != null)
            {
                LevelControl = tf.GetOrAddComponent<LevelControl>();

                //预置关卡同样按配置只激活一个LevelAction_xxx, 先处理再激活根节点
                Transform actionTf = tf.Find(LevelActionNodeName);
                if (actionTf)
                {
                    ActivateLevelAction(actionTf, LevelSettingHelper.GetLevelActionName(CurLevelName));
                }

                tf.gameObject.SetActive(true);
                yield break;
            }

            //第一步: 加载地图, mapPrefab未配置时用关卡名, 多个关卡可共用同一套地图
            string mapName = LevelSettingHelper.GetMapPrefabName(CurLevelName);
            Debug.Log($"--- load map {mapName} for level {CurLevelName}");
            string path = $"{MapDir}/{mapName}.prefab";
            AssetHandle handle = ResMgr.LoadPrefabAsyncHandle(path);
            yield return handle;
            GameObject obj = handle.InstantiateSync(new InstantiateOptions(true, transform, false));
            obj.name = CurLevelName;
            LevelControl = obj.GetOrAddComponent<LevelControl>();

            //第二步: 加载该关卡专属的敌人配置
            yield return LoadLevelAction(obj.transform);
        }

        /// <summary>
        /// 加载关卡敌人配置 LevelAction_0_2 等预制体, 挂到地图的LevelAction节点下
        /// 地图LevelAction节点下可预置多个LevelAction_xxx, 只激活配置指定的那个
        /// 资源不存在时表示地图预制体自身已带敌人配置, 不做处理
        /// </summary>
        private IEnumerator LoadLevelAction(Transform levelTf)
        {
            string actionName = LevelSettingHelper.GetLevelActionName(CurLevelName);
            Transform actionTf = levelTf.Find(LevelActionNodeName);

            //LevelAction节点下已有同名子物体时复用, 并关闭其余LevelAction_xxx
            if (actionTf && ActivateLevelAction(actionTf, actionName))
            {
                Debug.Log($"--- use map LevelAction {actionName}");
                yield break;
            }

            string path = $"{LevelActionDir}/{actionName}.prefab";

            if (!ResMgr.Loader.IsLocationValid(path))
            {
                Debug.Log($"--- no LevelAction {path}, use map setting");
                yield break;
            }

            if (!actionTf)
            {
                Debuger.LogError($"--- no {LevelActionNodeName} node in map {levelTf.name}");
                yield break;
            }

            CheckMapActionEmpty(actionTf, actionName);

            AssetHandle handle = ResMgr.LoadPrefabAsyncHandle(path);
            yield return handle;
            GameObject actionObj = handle.InstantiateSync(new InstantiateOptions(true, actionTf, false));
            actionObj.name = actionName;
            Debug.Log($"--- load LevelAction {actionName}");
        }

        /// <summary>
        /// LevelAction节点下可以有多个LevelAction_xxx子物体, 只激活配置对应的那个, 其余关闭
        /// 返回是否找到了同名子物体
        /// </summary>
        private bool ActivateLevelAction(Transform actionTf, string actionName)
        {
            bool hasMatch = false;

            for (int i = 0; i < actionTf.childCount; i++)
            {
                Transform child = actionTf.GetChild(i);
                if (!child.name.StartsWith(LevelActionPrefix))
                {
                    continue;
                }

                //激活前Unity不会执行未激活子物体的Awake, 所以不会误刷怪
                bool isMatch = child.name == actionName;
                child.gameObject.SetActive(isMatch);
                hasMatch |= isMatch;
            }

            return hasMatch;
        }

        /// <summary>
        /// 检查地图自带的LevelAction内容, 已存在Group时警告, 避免与加载的LevelAction重复刷怪
        /// </summary>
        private void CheckMapActionEmpty(Transform actionTf, string actionName)
        {
            for (int i = 0; i < actionTf.childCount; i++)
            {
                if (actionTf.GetChild(i).name.StartsWith("Group"))
                {
                    Debuger.LogWarning($"--- map {actionTf.parent?.name} 自带Group, 会与 {actionName} 叠加, 建议清空地图上的LevelAction");
                    return;
                }
            }
        }

        private void GetSetting()
        {
            //主城不加载关卡地图, 也不参与关卡调试覆盖(否则调试关卡名会把关卡地图塞进主城场景)
            if (GameDataCommon.Current.gameStage == EGameStage.Home)
            {
                return;
            }

#if UNITY_EDITOR
            //当处于启动界面时,设置测试场景
            bool isTestLoad = GameAllData.CommonData.firstSceneName == SceneManager.GetActiveScene().name;
            if (isTestLoad && !string.IsNullOrEmpty(testLevelName) && !dontLoadMap)
            {
                CurLevelName = testLevelName;
                testLevelName = "";
                return;
            }
#endif
            if (DebugSetting.IsDebug)
            {
                var cfg = ConfigMgr.Inst.MainCfg;
                string getLevelBranch = cfg.GetValue("Setting", "LevelBranch", "");
                if (!string.IsNullOrEmpty(getLevelBranch))
                {
                    LevelData.LevelBranch = getLevelBranch;
                }

                string levelName = cfg.GetValue("Setting", "DebugLevelName", "");
                if (!string.IsNullOrEmpty(levelName))
                {
                    CurLevelName = levelName;
                }
            }
        }

        public void SetPlayerStartPos()
        {
            if (GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.isBodyCreated)
            {
                RoleMovement movement = GameDataCommon.LocalPlayer.Movement;
                movement.MoveToImmediate(GetStartPos());

                //初始朝向跟随起点: 有起始点就面朝它的 forward, 没有则保持当前朝向
                Transform startTf = GetStartPoint();
                if (startTf)
                {
                    movement.LookToDir(startTf.forward);
                }
            }
            else
            {
                Debug.LogError("---  no player creat");
            }
        }

        /// <summary>
        /// 获取当前地图的起始点(主城走HomeMapControl, 关卡走LevelControl), 找不到返回 null
        /// </summary>
        private Transform GetStartPoint()
        {
            if (GameDataCommon.Current.gameStage == EGameStage.Home)
            {
                var home = transform.GetComponentInChildren<HomeMapControl>();
                return home ? home.GetStartPoint() : null;
            }

            return LevelControl ? LevelControl.GetStartPoint() : null;
        }

        private Vector3 GetStartPos()
        {
            if (GameDataCommon.Current.gameStage == EGameStage.Home)
            {
                var home = transform.GetComponentInChildren<HomeMapControl>();
                return home.GetStartPos();
            }

            if (!LevelControl)
            {
                return Vector3.zero;
            }


            return LevelControl.GetStartPos();
        }

        public void HideOtherLevel()
        {
            foreach (Transform child in transform)
            {
                if (child.name != CurLevelName && child.name.StartsWith("level"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public Vector3 GetEndPos()
        {
            if (!LevelControl)
            {
                return GameDataCommon.LocalPlayer.transform.position;
            }

            return LevelControl.GetEndPos();
        }
    }
}