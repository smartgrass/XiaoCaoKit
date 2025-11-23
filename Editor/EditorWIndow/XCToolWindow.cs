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
        public const int Line3 = 3;
        public const int Line99 = 99;

        [HorLayout(true)] public bool IsKaiLe = false;

        [HorLayout(false)] [OnValueChanged(nameof(CheckDebugGo))]
        public bool IsShowDebug = true;

        [HorLayout(true)] [OnValueChanged(nameof(OnLevelChange))]
        public int playerLevel = 5;

        [OnValueChanged(nameof(OnLevelChange))]
        public bool isHiding;

        [HorLayout(false)] [Range(0, 10)] [OnValueChanged(nameof(OnTimeScale))]
        public float timeScale = 1;

        [MiniBtn(nameof(SetFrameRate))] public int frameRate = 30;

        [HorLayout(true)] [MiniBtn(nameof(AddBuff))]
        public EBuff buff;

        [HorLayout(false)] public bool autoAddBuff;


        public override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorApplication.pauseStateChanged += PauseStateChanged;
            GameEvent.AddEventListener<GameState, GameState>(EGameEvent.GameStateChange.Int(), GameStateChange);
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.pauseStateChanged -= PauseStateChanged;
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
                    OnHidingChange();
                }

                if (autoAddBuff)
                {
                    AddBuff();
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

        private void PauseStateChanged(PauseState state)
        {
        }

        private void CheckDebugGo()
        {
            DebugSetting.DebugGUI_IsShow.SetKeyBool(IsShowDebug);
            if (MarkObjectMgr.TryGet("debugGo", out GameObject debugGo))
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

        [Button("生成队友", Line1, enabledMode: EButtonEnableMode.Playmode)]
        void GenFriend()
        {
            var playerTf = GameDataCommon.LocalPlayer.transform;
            Vector3 pos = playerTf.position + playerTf.forward * 3;
            string roleKey = $"Role_{ConfigMgr.Inst.LocalRoleSetting.GetFriendRoleIndex()}";
            Enemy0 enemy0 = EnemyCreator.GenFriend(roleKey, pos, GameDataCommon.LocalPlayer.Level);
        }


        [Button("生成敌人", Line1, enabledMode: EButtonEnableMode.Playmode)]
        void GenEnemy()
        {
            var testRole = GameObject.FindAnyObjectByType<EnemyCreator>();
            if (testRole)
            {
                testRole.Gen();
            }
        }

        [Button("击杀敌人", Line1, enabledMode: EButtonEnableMode.Playmode)]
        void KillEnemy()
        {
            foreach (var role in RoleMgr.Inst.roleDic.Values)
            {
                if (!role.IsPlayer && role.Enable && !role.IsDie && role.IsAiOn)
                {
                    role.OnDie(new AtkInfo());
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

        [Button("玩家存档面板", Line3)]
        void OpenSavaDataWin()
        {
            PlayerSaveDataWindow.Open();
        }

        [Button("LevelEnd", Line3, enabledMode: EButtonEnableMode.Playmode)]
        void LevelEnd()
        {
            var endPos = MapMgr.Inst.GetEndPos();
            Vector3 offset = (endPos - GameDataCommon.LocalPlayer.transform.position).ToY0().normalized * -3;
            GameDataCommon.LocalPlayer.Movement.MoveToImmediate(endPos + offset + Vector3.up);
            GameEvent.Send<string>(EGameEvent.MapMsg.Int(), "LevelFinish");
            GameMgr.Inst.LevelFinish();
        }

        private void OnHidingChange()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                GameDataCommon.LocalPlayer.IsHiding = isHiding;
            }
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
                GameDataCommon.LocalPlayer.data_R.playerAttr.lv = playerLevel;
                GameDataCommon.LocalPlayer.InitRoleData();
                HotFlags.PlayerAttrChange = true;
            }
        }


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

        [Button("Buff + 圣器", Line99)]
        void GetBuffs()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Item item = RewardHelper.GetItemWithPool("0", 0);
                RewardHelper.RewardItem(item);
            }


            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (item.GetBuffType() == EBuffType.Ex)
                {
                    var buffItem = BuffHelper.CreatBuffItem(item);
                    PlayerHelper.AddBuff(0, buffItem);
                }
            }
        }

        [Button("Test Config", Line99)]
        void TestConfig()
        {
            Debug.Log($"{Application.dataPath}" +
                      $"{Application.consoleLogPath} {PathTool.GetProjectPath()}");
        }

        private void SetFrameRate()
        {
            Application.targetFrameRate = frameRate;
        }

        void AddBuff()
        {
            if (buff == EBuff.None)
            {
                return;
            }

            var buffItem = BuffHelper.CreatBuffItem(buff);
            PlayerHelper.AddBuff(0, buffItem);
        }

        [Button("LogAllLocalize", Line99)]
        void LogAllLocalize()
        {
            foreach (var section in LocalizeMgr.Inst.localizeData.SectionList)
            {
                Debug.Log($"-- section {section.SectionName}");
                foreach (var dic in section.Dic)
                {
                    Debug.Log($"-- dic {dic.Key} {dic.Value}");
                }
            }
        }
    }
}
#endif