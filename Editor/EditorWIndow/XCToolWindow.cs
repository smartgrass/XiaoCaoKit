#if UNITY_EDITOR
//using AssetEditor.Editor.Window;

// ReSharper disable once CheckNamespace
using NaughtyAttributes;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace AssetEditor.Editor
{
    public class XCToolWindow : XiaoCaoWindow
    {
        [MenuItem(XCEditorTools.XCToolWindow)]
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
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        private void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                if (IsKaiLe)
                {
                    "IsKaiLe".SetKeyBool(true);
                    OnTimeScale();
                }
                IsShowDebug = DebugPanel.DebugGUI_IsShow.GetKeyBool();
            }
        }

        private void CheckDebugGo()
        {
            DebugPanel.DebugGUI_IsShow.SetKeyBool(IsShowDebug);
            if (MarkObject.TryGet("debugGo", out GameObject debugGo))
            {
                debugGo.gameObject.SetActive(IsShowDebug);
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
                Selection.activeGameObject = GameDataCommon.Current.player0.gameObject;
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
                GameMgr.Inst.Player.PlayerAttr.lv = playerLevel;
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


        [Button("Test Config", Line99)]
        void TestConfig()
        {
            var t = ConfigMgr.MainCfg;
        }

    }
}
#endif