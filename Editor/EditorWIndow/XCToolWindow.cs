#if UNITY_EDITOR
//using AssetEditor.Editor.Window;

// ReSharper disable once CheckNamespace
using NaughtyAttributes;
using NUnit.Framework;
using System;
using System.Collections;
using TEngine;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace AssetEditor.Editor
{
    public class XCToolWindow : XiaoCaoWindow
    {
        [MenuItem(XCEditorTools.XCToolWindow, priority = 1)]
        public static XCToolWindow Open()
        {
            return OpenWindow<XCToolWindow>("XiaoCao调试面板");
        }

        public const int Line1 = 1;
        public const int Line2 = 2;
        public const int Line99 = 99;

        [HorLayout(true)]
        public bool IsKaiLe = false;
        [HorLayout(false)]
        [OnValueChanged(nameof(CheckDebugGo))]
        public bool IsShowDebug = true;
        [OnValueChanged(nameof(OnLevelChange))]
        public int playerLevel = 5;


        public override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            GameEvent.AddEventListener<GameState, GameState>(EGameEvent.GameStateChange.Int(), GameStateChange);
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            GameEvent.RemoveEventListener<GameState, GameState>(EGameEvent.GameStateChange.Int(), GameStateChange);
        }

        private void GameStateChange(GameState state1, GameState state2)
        {
            if (state2 == GameState.Running)
            {
                if (IsKaiLe)
                {
                    "IsKaiLe".SetKeyBool(true);
                    OnTimeScale();
                    GetBuffs();
                }
            }
        }

        private void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                IsShowDebug = DebugSetting.DebugGUI_IsShow.GetKeyBool();
            }
        }


        private void CheckDebugGo()
        {
            DebugSetting.DebugGUI_IsShow.SetKeyBool(IsShowDebug);
            if (MarkObject.TryGet("debugGo", out GameObject debugGo))
            {
                if (debugGo)
                {
                    debugGo.gameObject.SetActive(IsShowDebug);
                }

            }
        }

        [Button("选中角色", Line1)]
        void SelectPlayer()
        {

            if (!Application.isPlaying)
            {
                Selection.activeObject = GameObject.Find("Editor/Player").transform.GetChild(0);
                return;
            }

            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                Selection.activeGameObject = GameDataCommon.LocalPlayer.gameObject;
            }
        }

        [Button("选中敌人", Line1)]
        void SelectEnemy()
        {

            if (!Application.isPlaying)
            {
                return;
            }

            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                foreach (var player in RoleMgr.Inst.roleDic.Values)
                {
                    if (!player.IsPlayer)
                    {
                        Selection.activeObject = player.gameObject;
                        return;
                    }
                }

            }
        }

        [Button("生成敌人", Line1, enabledMode: EButtonEnableMode.Playmode)]
        void GenEnemy()
        {
            var testRole = GameObject.FindAnyObjectByType<TestRole>();
            if (testRole)
            {
                testRole.Gen();
            }
        }


        [Button("打开替换面板", Line1)]
        void OpenRepalceTool()
        {
            XCRepalceToolWin.Open();
        }


        [Button("打开配表位置", Line2)]
        void OpenExcelPath()
        {
            XCToolBarMenu.OpenPath_Excel();
        }

        [Button("打开生成配置位置", Line2)]
        void OpenGenConfigPath()
        {
            XCToolBarMenu.OpenGenConfigPath();
        }

        [Button("导入配置+代码", Line2)]
        void GenConfig()
        {
            //XCToolBarMenu.OpenPath_Excel();
            SaveXCTask.LoadLubanExcelWithCode();
        }


        private void OnLevelChange()
        {
            PlayerPrefs.SetInt("playerLv", playerLevel);
            if (!Application.isPlaying)
            {
                return;
            }

            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                GameDataCommon.LocalPlayer.PlayerAttr.lv = playerLevel;
            }

        }

        [Range(0, 10)]
        [OnValueChanged(nameof(OnTimeScale))]
        public float timeScale = 1;

        void OnTimeScale()
        {
            Debug.Log($"--- OnTimeScale {timeScale}");
            XCTime.timeScale = timeScale;
        }

        //[Button("Stop Time 5s", Line2)]
        void StopTime()
        {
            //TestRole testRole  = GameObject.FindObjectOfType<TestRole>();

        }
        [Button("清空对象池", Line99)]
        void ClearPool()
        {
            PoolMgr.Inst.ClearAllPool(false);
        }

        [Button("Buff + 10", Line99)]
        void GetBuffs()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            for (int i = 0; i < 8; i++)
            {
                RewardHelper.RewardBuffFromPool(0, "0", 0);
            }


            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (item.GetBuffType() == EBuffType.Other)
                {
                    var buffItem = BuffHelper.CreatBuffItem(item);
                    PlayerHelper.AddBuff(0, buffItem);
                }
            }
        }

        [Button("Test Config", Line99)]
        void TestConfig()
        {
            LocalizeMgr.ClearCache();
            LocalizeMgr t = LocalizeMgr.Inst;
            LocalizeMgr.Localize("Key").LogStr();
        }

    }
}
#endif