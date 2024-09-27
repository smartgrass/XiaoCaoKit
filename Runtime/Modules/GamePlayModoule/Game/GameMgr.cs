using System;
using System.Collections;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
namespace XiaoCao
{
    ///<see cref="GameDataCommon"/>
    public class GameMgr : MonoSingleton<GameMgr>, IMgr
    {
        #region AllMgr
        public GameDataCommon GameData => GameDataCommon.Current;
        public BattleData BattleData => BattleData.Current;
        public Player0 Player => GameData.player0;
        public EntityMgr entityMgr;
        public SoundMgr soundMgr;
        public CameraMgr cameraMgr;
        public TimeStopMgr timeStopMgr;

        #endregion
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
            GameEvent.Send<GameState, GameState>(EventType.GameStateChange.Int(), oldState, gameState);
        }

        #region Scene


        private int curScene;


        private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            curScene = scene.buildIndex;
            Debug.Log($"--- curScene {curScene}");
        }

        public void FinishLevel(int nextScene)
        {
            //结算->显示结算页

        }

        public void LoadScene(int sceneBuildIndex)
        {
            StartCoroutine(LoadSceneInBackground(sceneBuildIndex));
        }

        public void UnloadActiveScene()
        {
            if (curScene > 0)
            {
                SceneManager.UnloadSceneAsync(curScene);
                curScene = 0;
            }
        }

        private IEnumerator LoadSceneInBackground(int sceneBuildIndex)
        {
            SetGameState(GameState.Loading);
            UnloadActiveScene();

            curScene = sceneBuildIndex;
            AsyncOperation loadingScene = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
            while (!loadingScene.isDone)
                yield return new WaitForSeconds(0.25f);

            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
            SetGameState(GameState.Running);
        }
        #endregion
    }

    public class SceneIndex
    {
        public const int InitScene = 0;
        public const int MainScene = 1;
        public const int LoadingScene = 2;
    }


    ///使用<see cref="GameMgr"/>
    public class SceneMgr { }

    


}