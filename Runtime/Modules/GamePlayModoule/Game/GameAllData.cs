using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XiaoCao;

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

        [RuntimeInitializeOnLoadMethod]
        public static void Init()
        {
            commonData = new GameDataCommon();
            battleData = new BattleData();
        }

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

    public class BattleData
    {
        public BattleData()
        {
            CanPlayerControl.AddListener(OnControlChange);
        }

        public static BattleData Current => GameAllData.battleData;

        public LevelRewardData levelRewardData = new LevelRewardData();
        public static bool IsTimeStop { get => Current._isTimeStop; set => Current._isTimeStop = value; }

        private bool _isTimeStop;

        //每帧都会清空
        public HashSet<string> frameFlag = new HashSet<string>();

        public HashSet<string> map = new HashSet<string>();

        public Dictionary<string, int> tempIntDic = new Dictionary<string, int>();

        public DataListener<bool> CanPlayerControl = new DataListener<bool>(true);

        public bool UIEnter;


        ///<see cref="BattleFlagNames"/>
        public bool HasFlag(string flag)
        {
            return map.Contains(flag);
        }

        public void OnControlChange(bool v)
        {
            Debug.Log($"--- OnControlChange {v}");
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

    }

    public static class GameSetting
    {
        //目前技能
        public const int SkillCountOnBar = 3;
        ///<see cref="EQuality"/>
        public const int MaxBuffLevel = 5;//从0~5

        //根据阵营分层级
        public static int GetTeamLayer(int team)
        {
            if (team == 1)
            {
                return Layers.PLAYER;
            }
            return Layers.ENEMY;
        }

        public static int GetTeamAtkLayer(int team)
        {
            if (team == 1)
            {
                return Layers.PLAYER_ATK;
            }
            return Layers.ENEMY_ATK;
        }


        public static int GetTeamGroundCheckMash(int team)
        {
            if (team == 1)
            {
                return Layers.ENEMY_MASK;
            }
            return Layers.PLAYER_MASK;
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

        PlayerEvent = 100, //分界线
        PlayerPlaySkill = 101,
        TimeSpeedStop = 102,
        LocalPlayerChangeNowAttr = 103,
        PlayerCreatNorAtk = 104,

        //Enemy
        EnemyDeadEvent = 200,
        EnemyGroupEndEvent = 201,
    }

    public static class BattleFlagNames
    {

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


