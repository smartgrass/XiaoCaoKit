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
        private static MapMgr _instance = null;

        public static MapMgr Inst => _instance;

        public bool dontLoadMap;

        public string testLevelName = "";


        private bool testLoad;

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
#if UNITY_EDITOR
            if (testLoad && dontLoadMap)
            {
                yield break;
            }
#endif

            if (string.IsNullOrEmpty(CurLevelName))
            {
                yield break;
            }

            HideOtherLevel();

            Transform tf = transform.Find(CurLevelName);
            if (tf != null)
            {
                LevelControl = tf.GetOrAddComponent<LevelControl>();
                tf.gameObject.SetActive(true);
                yield break;
            }

            Debug.Log($"--- load map {CurLevelName}");
            string path = $"Assets/_Res/Map/{CurLevelName}.prefab";
            AssetHandle handle = ResMgr.LoadPrefabAsyncHandle(path);
            yield return handle;
            GameObject obj = handle.InstantiateSync(transform);
            obj.name = CurLevelName;
            LevelControl = obj.GetOrAddComponent<LevelControl>();
        }

        private void GetSetting()
        {
#if UNITY_EDITOR
            //当处于启动界面时,设置测试场景
            testLoad = GameAllData.CommonData.firstSceneName == SceneManager.GetActiveScene().name;
            if (testLoad && !string.IsNullOrEmpty(testLevelName) && !dontLoadMap)
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
            if (LevelControl != null)
            {
                if (GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.isBodyCreated)
                {
                    GameDataCommon.LocalPlayer.Movement.MoveToImmediate(GetStartPos());
                }
                else
                {
                    Debug.LogError("---  no player creat");
                }
            }
        }

        private Vector3 GetStartPos()
        {
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