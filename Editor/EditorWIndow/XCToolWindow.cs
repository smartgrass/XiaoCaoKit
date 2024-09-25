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
            }
        }

        [Button("选中角色", Line1)]
        void SelectPlayer()
        {
            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                Selection.activeGameObject = GameDataCommon.Current.player0.gameObject;
            }

            if (!Application.isPlaying)
            {
                Selection.activeObject = GameObject.Find("Editor/Player").transform.GetChild(0);
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

        [Button("导入配置+代码", Line2)]
        void GenConfig()
        {
            //XCToolBarMenu.OpenPath_Excel();
            SaveXCTask.LoadLubanExcelWithCode();
        }

        public bool IsKaiLe = false;
        [OnValueChanged(nameof(OnLevelChange))]
        public int playerLevel = 5;

        private void OnLevelChange()
        {
            PlayerPrefs.SetInt("playerLv", playerLevel);
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
    }
}
#endif