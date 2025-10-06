using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace XiaoCao
{
    //与CameraController类似是在Scene里面预配置的
    public class MapMgr : MonoSingleton<MapMgr>, IMgr
    {
        public bool dontLoadMap;

        public string testLevelName = "";

        public string MapName
        {
            get => GameDataCommon.Current.mapName;
            set => GameDataCommon.Current.mapName = value;
        }

        public LevelData LevelData { get => BattleData.Current.levelData; }

        private LevelControl levelControl;
 
        private void Awake()
        {
            _instance = this;
        }


        public IEnumerator LoadLevelObject()
        {
            GetSetting();
#if UNITY_EDITOR
            if (dontLoadMap)
            {
                yield break;
            }
#endif

            if (string.IsNullOrEmpty(MapName))
            {
                yield break;
            }
            HideOtherLevel();

            Transform tf = transform.Find(MapName);
            if (tf != null)
            {
                levelControl = tf.GetComponent<LevelControl>();
                tf.gameObject.SetActive(true);
                yield break;
            }
            Debug.Log($"--- load map {MapName}");
            string path = $"Assets/_Res/Map/{MapName}.prefab";
            AssetHandle handle = ResMgr.LoadPrefabAsyncHandle(path);
            yield return handle;
            GameObject obj = handle.InstantiateSync(transform);
            obj.name = MapName;
            levelControl = obj.GetComponent<LevelControl>();
        }

        private void GetSetting()
        {

            if (DebugSetting.IsSkillEditor || DebugSetting.IsDebug)
            {
                var cfg = ConfigMgr.MainCfg;
                string getLevelBranch = cfg.GetValue("Setting", "LevelBranch", "");
                if (!string.IsNullOrEmpty(getLevelBranch))
                {
                    LevelData.LevelBranch = getLevelBranch;
                }

                string levelName = cfg.GetValue("Setting", "DebugLevelName", "");
                if (!string.IsNullOrEmpty(levelName)) {
                    MapName = levelName;
                }

                if (Application.isEditor && !string.IsNullOrEmpty(testLevelName))
                {
                    MapName = testLevelName;
                }
            }
        }

        public void SetPlayerStartPos()
        {
            if (levelControl != null)
            {
                if (GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.isBodyCreated)
                {
                    GameDataCommon.LocalPlayer.Movement.MoveToImmediate(levelControl.GetStartPos());
                }
                else
                {
                    Debug.LogError("---  no player creat");
                }
            }
        }

        public void HideOtherLevel()
        {
            foreach (Transform child in transform)
            {
                if (child.name != MapName && child.name.StartsWith("level"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

    }
}
