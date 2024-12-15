using NaughtyAttributes;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using TEngine;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using XiaoCao;
using Debug = UnityEngine.Debug;
namespace XiaoCao
{
    ///<see cref="GameDataCommon"/>
    public class GameMgr : MonoSingleton<GameMgr>, IMgr
    {
        #region AllMgr
        public GameDataCommon GameData => GameDataCommon.Current;
        public BattleData BattleData => BattleData.Current;
        public Player0 LocalPlayer => GameDataCommon.GetPlayer();
        public EntityMgr entityMgr;
        public SoundMgr soundMgr;
        public CameraMgr cameraMgr;
        public TimeStopMgr timeStopMgr;
        public LevelControl levelControl;

        //静态
        public SaveMgr saveMgr;
        public ConfigMgr configMgr;

        //Helper
        ///<see cref="PlayerHelper"/>
        ///<see cref="RewardHelper"/>
        ///<see cref="LevelSettingHelper"/>


        #endregion

        [Button]
        void DebuggameData()
        {
#if UNITY_EDITOR
            Debug.Log($"--- DebuggameData");
            var dataView = gameObject.AddComponent<Test_GameDataView>();
            dataView.GetCurrentData();
#endif

        }

        public override void Init()
        {
            base.Init();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SetGameState(GameState gameState)
        {
            var oldState = GameDataCommon.Current.gameState;
            GameDataCommon.Current.gameState = gameState;
            GameEvent.Send<GameState, GameState>(EGameEvent.GameStateChange.Int(), oldState, gameState);
        }

        #region Scene


        private string curScene;


        private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            //curScene = scene.buildIndex;
            curScene = scene.name;
            Debug.Log($"--- curScene {curScene}");
        }

        public void FinishLevel(int nextScene)
        {
            //结算->显示结算页

        }

        public void ReloadScene()
        {
            SetGameState(GameState.Exit);
            curScene = SceneManager.GetActiveScene().name;
            LoadScene(curScene);
        }

        public void LoadScene(string sceneName)
        {
            SetGameState(GameState.Loading);
            GameDataCommon.Current.NextSceneName = curScene;
            SceneManager.LoadScene(SceneNames.Loading);
            StartCoroutine(LoadSceneInBackground(sceneName));
        }

        public void UnloadActiveScene()
        {
            //Debug.Log($"--- UnloadSceneAsync {curScene}");
            //return SceneManager.UnloadSceneAsync(curScene);
        }

        private IEnumerator LoadSceneInBackground(string NextScene)
        {

            curScene = NextScene;
            AsyncOperation loadingScene = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Single);
            Debug.Log($"--- LoadSceneInBackground {NextScene}");
            while (!loadingScene.isDone)
            {
                float progress = loadingScene.progress;

                Debug.Log($"Loading: {progress * 100:F2}%");

                yield return new WaitForSeconds(0.25f);
            }


            // SceneManager.GetSceneByBuildIndex(sceneBuildIndex)
            //SceneManager.SetActiveScene(SceneManager.GetSceneByName(curScene));
            SetGameState(GameState.Running);
        }
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


    }

    public class SceneIndex
    {
        public const int InitScene = 0;
        public const int MainScene = 1;
        public const int LoadingScene = 2;
    }


    ///直接使用<see cref="GameMgr"/>
    public class SceneMgr { }

    public static class SceneNames
    {
        public static string Level = "Level";
        public static string Loading = "Loading";
    }



}