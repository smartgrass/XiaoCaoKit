using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using NaughtyAttributes;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using XiaoCao;
using XiaoCao.UI;
using Debug = UnityEngine.Debug;

namespace XiaoCao
{
    ///<see cref="GameDataCommon"/>
    public class GameMgr : MonoSingleton<GameMgr>, IMgr, IMapMsgReceiver
    {
        #region AllMgr

        private GameDataCommon GameData => GameDataCommon.Current;
        private BattleData BattleData => BattleData.Current;

        private Player0 LocalPlayer => GameDataCommon.LocalPlayer;

        public EntityMgr entityMgr;
        public SoundMgr soundMgr;
        public CameraMgr cameraMgr;
        public TimeStopMgr timeStopMgr;
        public LevelControl levelControl;
        public PoolMgr poolMgr;

        public SaveMgr saveMgr;
        public ConfigMgr configMgr;

        //Helper
        ///<see cref="PlayerHelper"/>
        ///<see cref="RewardHelper"/>
        ///<see cref="LevelSettingHelper"/>

        #endregion

        [Button]
        void DebugGameData()
        {
#if UNITY_EDITOR
            Debug.Log($"--- DebuggameData");
            var dataView = gameObject.AddComponent<Test_GameDataView>();
            dataView.GetCurrentData();
            poolMgr = PoolMgr.Inst;
#endif
        }

        public override void Init()
        {
            base.Init();
            SceneManager.sceneLoaded += OnSceneLoaded;

            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.Int(), OnReceiveMsg);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.Int(), OnReceiveMsg);
        }

        public void SetGameState(GameState gameState)
        {
            var oldState = GameDataCommon.Current.gameState;
            GameDataCommon.Current.gameState = gameState;
            GameEvent.Send<GameState, GameState>(EGameEvent.GameStateChange.Int(), oldState, gameState);
        }

      public  void OnReceiveMsg(string msg)
        {
            if (msg == ShowActKeys.LevelFinish.ToString())
            {
                LevelFinish();
            }
        }


        #region Scene

        private string curScene;

        public static string firstScene;


        private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            curScene = scene.name;
            Debug.Log($"--- OnSceneLoaded {curScene}");
            if (curScene == SceneNames.Level)
            {
                BlackScreen.ShowBlack();
            }
        }

        public void BackHome()
        {
            SetGameState(GameState.Exit);
            SceneManager.LoadScene(SceneNames.Home);
        }

        //完成
        public void LevelFinish()
        {
            LevelData.Current.finishLevelTime = Time.time;
            GameEvent.Send<int>(EGameEvent.LevelEnd.Int(), 1);
            BattleData.Current.levelData.levelResult = ELevelResult.Success;
            UIMgr.PopToastKey(LocalizeKey.LevelFinish);
            //写入存档
            LevelInfo levelInfo = GameDataCommon.Current.GetLevelInfo;
            Debug.Log($"-- pass level {levelInfo.chapter}_{levelInfo.index}");
            PlayerSaveData.LocalSavaData.levelPassData.SetPassState(levelInfo.chapter, levelInfo.index);
            PlayerSaveData.SavaData();
            CreatePortalLevelEnd();
        }

        public void ShowLevelResultUI()
        {
            UIMgr.Inst.levelResultPanel.ShowUI();
        }
        

        void CreatePortalLevelEnd()
        {
            string path = "Assets/_Res/Item/PortalLevelEnd.prefab";
            GameObject portal = GameObject.Instantiate(ResMgr.LoadAseet<GameObject>(path));
            portal.transform.position = MapMgr.Inst.GetEndPos();
        }

        public DialogPanel ShowLevelEndDialog()
        {
            string title = "";
            string content = LocalizeKey.IsExitLevel.ToLocalizeStr();
            return DialogManager.ShowDialog(title, content, GameMgr.Inst.ShowLevelResultUI);
        }


        public void ReloadScene()
        {
            SetGameState(GameState.Exit);
            curScene = SceneManager.GetActiveScene().name;
            LoadScene(curScene);
            SoundMgr.Inst.OnReloadScene();
        }

        public void LoadScene(string sceneName)
        {
            SetGameState(GameState.Loading);
            GameDataCommon.Current.NextSceneName = sceneName;
            SceneManager.LoadScene(SceneNames.Loading);
            ///<see cref="SceneLoader"/>
            //StartCoroutine(LoadSceneInBackground(sceneName));
        }


        /// <see cref="MapNames"/>
        public void LoadLevelScene(string levelId)
        {
            MapMgr.CurLevelName = levelId;

            GameMgr.Inst.LoadScene(SceneNames.Level);
        }


        public void UnloadActiveScene()
        {
            //Debug.Log($"--- UnloadSceneAsync {curScene}");
            //return SceneManager.UnloadSceneAsync(curScene);
        }

        // [Obsolete]
        // private IEnumerator LoadSceneInBackground(string NextScene)
        // {
        //     curScene = NextScene;
        //     AsyncOperation loadingScene = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Single);
        //     Debug.Log($"--- LoadSceneInBackground {NextScene}");
        //     while (!loadingScene.isDone)
        //     {
        //         float progress = loadingScene.progress;
        //
        //         Debug.Log($"Loading: {progress * 100:F2}%");
        //
        //         yield return new WaitForSeconds(0.25f);
        //     }
        //     Debug.Log($"Loading: finish {NextScene}");
        //
        //     // SceneManager.GetSceneByBuildIndex(sceneBuildIndex)
        //     //SceneManager.SetActiveScene(SceneManager.GetSceneByName(curScene));
        //     SetGameState(GameState.Running);
        // }

        #endregion


        public void StarTestProfiler(Action act)
        {
            string tag = "TestFun";
            Stopwatch sw = new Stopwatch();
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample(tag);
            //act.Invoke();
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            UnityEngine.Debug.Log(string.Format($"-- TestFun {tag}: {0} ms", sw.ElapsedMilliseconds));
        }

        internal static void ClearSceneData()
        {
            Debug.Log($"--- ClearSceneData ");
            PoolMgr.Inst.ClearAllPool(true);
            TimerManager.ClearSelf();
            GameAllData.battleData = new BattleData();
        }
    }

    public class SceneIndex
    {
        public const int InitScene = 0;
        public const int MainScene = 1;
        public const int LoadingScene = 2;
    }


    public class SceneMgr
    {
    }

    public static class SceneNames
    {
        public static readonly string Level = "Level";
        public static readonly string Loading = "Loading";
        public static readonly string Home = "Home";
    }

    public static class MapNames
    {
        //story与level拆开,方便跳过剧情
        //如果是在游戏中 则不是无法跳过,或者做其他定制化的处理
        public static readonly string Level1 = "level_0_1";

        public static string GetLevelKey(int chapter, int index)
        {
            return $"level_{chapter}_{index}";
        }

        public static LevelInfo GetLevelInfoByName(string levelName)
        {
            var array = levelName.Split('_');

            return new LevelInfo()
            {
                chapter = int.Parse(array[1]),
                index = int.Parse(array[2])
            };
        }
    }

    public struct LevelInfo
    {
        public int chapter;
        public int index;

        public string GetLevelName()
        {
            return LocalizeKey.GetLevelName(chapter, index);
        }
    }
}