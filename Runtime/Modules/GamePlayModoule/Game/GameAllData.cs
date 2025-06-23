using cfg;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XiaoCao
{
    public class GameAllData
    {
        //不会清空
        public static GameDataCommon commonData = new GameDataCommon();
        //过关卡时请空
        public static BattleData battleData = new BattleData();

        //由玩家缓存数据读取 , 不需要初始值
        public static PlayerSaveData playerSaveData = null;

        //RuntimeInitializeOnLoadMethod
        public static void GameAllDataInit()
        {
            Debug.Log($"-- GameAllDataInit");
            commonData = new GameDataCommon();
            battleData = new BattleData();
        }

    }

    [XCHelper]
    public static class GameSetting
    {
        public static void GetGameVersion()
        {
            VersionType = ConfigMgr.StaticSettingSo.versionType;

        }
        public static GameVersionType VersionType;

        public static UserInputType UserInputType;

        //目前技能
        public const int SkillCountOnBar = 4;
        ///<see cref="EQuality"/>
        public const int MaxBuffLevel = 5;//从0~5

        ///<see cref="LevelSettingHelper"/>
        ///<see cref="XCSetting"/>
    }

    public enum UserInputType
    {
        Mouse,
        Touch,
    }


    public class TempData
    {

    }

    public class GameDataCommon
    {
        public static GameDataCommon Current => GameAllData.commonData;

        public PlayMode playMode;

        public GameState gameState;

        public Player0 player0;

        //需要保存
        public int LocalPlayerId;

        public string NextSceneName;

        public string MapName = "level0";

        public static Player0 LocalPlayer
        {
            get
            {
                return Current.player0;
            }

        }

        public static Player0 GetPlayer(int id = 0)
        {
            //封装, 方便多玩家时处理
            return Current.player0;

        }

        public bool loadMod = false;

    }

    /// <summary>
    /// bool
    /// </summary>
    public class HotFlags
    {
        public static bool PlayerAttrChange;
    }


    public class BattleData
    {
        public BattleData()
        {
            CanPlayerControl.AddListener(OnControlChange);
        }

        public static BattleData Current => GameAllData.battleData;

        public LevelData levelData = new LevelData();
        public static bool IsTimeStop { get => Current._isTimeStop; set => Current._isTimeStop = value; }

        private bool _isTimeStop;

        //每帧都会清空
        public HashSet<string> frameFlag = new HashSet<string>();

        public HashSet<string> map = new HashSet<string>();

        public Dictionary<string, float> tempNumDic = new Dictionary<string, float>();

        public DataListener<bool> CanPlayerControl = new DataListener<bool>(true);

        public bool UIEnter;


        ///<see cref="BattleFlagNames"/>
        ///使用Map可以更好地清空数据
        public bool HasFlag(string flag)
        {
            return map.Contains(flag);
        }

        public void OnControlChange(bool v)
        {
            Debug.Log($"--- OnControlChange {v}");
        }

        public float GetDamageFactor(int Team)
        {
            float mult = 0;
            if (Team == XCSetting.PlayerTeam)
            {
                tempNumDic.TryGetValue(BattleNumKeys.DamageMult_P, out mult);
                return 1 + mult;
            }
            else
            {
                tempNumDic.TryGetValue(BattleNumKeys.DamageMult_E, out mult);
                return 1 + mult;
            }
        }
    }

    [XCHelper]
    public static class PlayerHelper
    {

        public static Role GetRoleById(this int id)
        {
            EntityMgr.Inst.FindEntity<Role>(id, out Role role);
            return role;
        }

        public static Player0 GetPlayerById(this int id)
        {
            return GameDataCommon.GetPlayer(id);
        }

        public static Enemy0 GetEnemyById(this int id)
        {
            EntityMgr.Inst.FindEntity<Enemy0>(id, out Enemy0 enemy);
            return enemy;
        }

        public static bool IsLocalPlayerId(this int id)
        {
            var player = GameDataCommon.Current.player0;
            if (player == null) return false;
            return player.id == id;
        }

        public static void AddBuff(int id, BuffItem buff)
        {
            GetPlayerBuff(id).AddBuff(buff);
            GameEvent.Send<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.Int(), UIPanelType.PlayerPanel, true);
        }

        public static PlayerBuffs LocalPlayerBuffs
        {
            get
            {
                return GetPlayerBuff().playerBuffs;
            }
        }

        public static BuffControl GetPlayerBuff(int playerId = -1)
        {
            if (playerId < 0)
            {
                return GameDataCommon.LocalPlayer.component.buffControl;
            }

            var player = playerId.GetPlayerById();
            if (player != null)
            {
                return player.component.buffControl;
            }
            Debug.LogError($"--- no player {playerId}");
            return null;
        }

        public static int GetSkillLevel(string skillId)
        {
            var dic = PlayerSaveData.Current.skillUnlockDic;
            dic.TryGetValue(skillId, out int level);
            return level;
        }


    }

    public enum PlayMode
    {
        Nor
    }

    public enum GameState
    {
        Loading,
        Running,
        Pause,
        Finish,
        Exit
    }

    public enum EGameEvent : uint
    {
        None = 0,
        GameStateChange = 1,
        ///<see cref="GameStartMono"/>
        GameStartFinsh = 2,
        CameraChange = 3,
        RoleChange = 4,
        UIPanelBtnGlow = 5,

        PlayerEvent = 100, //分界线
        PlayerPlaySkill = 101,
        TimeSpeedStop = 102,
        LocalPlayerChangeNowAttr = 103,
        PlayerCreatNorAtk = 104,
        PlayerGetBuffItem = 105,


        //Enemy
        EnemyDeadEvent = 200,
        EnemyGroupEndEvent = 201,

        //Map 300
        MapMsg = 300,

    }

    public static class BattleFlagNames
    {

    }

    public static class BattleNumKeys
    {
        public const string DamageMult_P = "DamageMult_P"; //伤害倍率
        public const string DamageMult_E = "DamageMult_E";
    }

    public static class TeamTag
    {
        public const int Enemy = 0;

        public const int Player = 10;

        public const int Player2 = 11;
    }

    public enum RoleChangeType
    {
        Add,
        Remove,
    }

    public static class EventTypeExtend
    {
        public static int Int(this EGameEvent t)
        {
            return (int)t;
        }
    }

    /// <summary>
    /// 值类型监听
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class DataListener<T>
    {
        public DataListener(T initialValue = default(T))
        {
            _data = initialValue;
        }

        // 声明事件，当有数据变化时触发  
        public event Action<T> OnValueChanged;

        private T _data;

        // 公共的getter  
        public T Data
        {
            get { return _data; }
        }

        // 提供一个方法来设置数据并触发事件  
        public void SetValue(T value)
        {
            T oldValue = _data;
            _data = value;
            OnValueChanged?.Invoke(value);
        }
        // 不触发回调
        public void SetValueQuiet(T value)
        {
            _data = value;
        }


        // 示例：注册监听事件  
        public void AddListener(Action<T> listener)
        {
            OnValueChanged += listener;
        }

        // 示例：注销监听事件  
        public void RemoveListener(Action<T> listener)
        {
            OnValueChanged -= listener;
        }


        // 隐式转换到T  
        public static implicit operator T(DataListener<T> wrapper)
        {
            return wrapper._data;
        }

        //// 隐式转换到T  
        //public static implicit operator DataListener<T> (T wrapper)
        //{
        //    DataListener<T> v = new DataListener<T>();
        //    v.SetValueQuiet (wrapper);
        //    return v;
        //}
    }

}


