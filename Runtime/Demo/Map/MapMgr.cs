using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace XiaoCao
{
    //与CameraController类似是在Scene里面预配置的
    public class MapMgr : MonoSingleton<MapMgr>, IMgr
    {
        public string MapName
        {
            get => GameDataCommon.Current.MapName;
            set => GameDataCommon.Current.MapName = value;
        }

        public LevelData LevelData { get => BattleData.Current.levelData; }

        private LevelControl levelControl;

        private void Awake()
        {
            _instance = this;
        }


        public IEnumerator LoadLevelObject()
        {
            GetEditorSetting();

            if (string.IsNullOrEmpty(MapName))
            {
                yield break;
            }
            HideOtherLevel();

            Transform tf = transform.Find(MapName);
            if (tf != null)
            {
                levelControl = tf.GetComponent<LevelControl>();
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

        private void GetEditorSetting()
        {
#if UNITY_EDITOR
            var cfg = ConfigMgr.MainCfg;
            string getLevelName = cfg.GetValue("Setting", "LevelName", "");
            if (!string.IsNullOrEmpty(getLevelName))
            {
                MapName = getLevelName;
            }
            string getLevelBranch = cfg.GetValue("Setting", "LevelBranch", "");
            if (!string.IsNullOrEmpty(getLevelBranch))
            {
                LevelData.LevelBranch = getLevelBranch;
            }
#endif
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
