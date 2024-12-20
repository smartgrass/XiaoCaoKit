﻿using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        //角色buff背包
        private PlayerBuffs playerBuffs = new PlayerBuffs();

        public static PlayerBuffs LocalPlayerBuffs => Current.playerBuffs;

        public static PlayerBuffs GetPlayerBuff(int id = -1)
        {
            if (id < 0)
            {
                return Current.playerBuffs;
            }
            //暂不区分角色
            return Current.playerBuffs;
        }


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
        public static bool IsLocalPlayerId(this int id)
        {
            var player = GameDataCommon.Current.player0;
            if (player == null) return false;
            return player.id == id;
        }

        public static void AddBuff(int id, BuffItem buff)
        {
            BattleData.GetPlayerBuff(id).AddBuff(buff);
        }

    }

    public class PlayerBuffs
    {
        public int MaxEquipped = 4;
        // 装备中buff
        public List<BuffItem> EquippedBuffs = new List<BuffItem>();
        // 未装备buff
        public List<BuffItem> UnequippedBuffs = new List<BuffItem>();
        public BuffItem GetValue(bool isEquipped, int index)
        {
            List<BuffItem> sourceList = isEquipped ? EquippedBuffs : UnequippedBuffs;
            if (sourceList.Count > index)
            {
                return sourceList[index];
            }
            return default;
        }

        public void AddBuff(BuffItem buff)
        {
            int findEmpty = -1;
            for (int i = 0; i < UnequippedBuffs.Count; i++)
            {
                if (UnequippedBuffs[i].GetBuffType == EBuffType.None)
                {
                    findEmpty = i;
                    UnequippedBuffs[findEmpty] = buff;
                    return;
                }
            }

            UnequippedBuffs.Add(buff);

        }

        // 合成: 移除buff-from, 将from第一个词条加在to
        public void SynthesisBuff(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> fromList = isFromEquipped ? EquippedBuffs : UnequippedBuffs;
            List<BuffItem> toList = isToEquipped ? EquippedBuffs : UnequippedBuffs;

            var toItem = toList[ToIndex];
            var costItem = fromList[FromIndex]; 

            toItem.UpGradeItem(costItem);
            toList[ToIndex] = toItem;
            costItem.Clear();
            fromList[FromIndex] = costItem;
        }

        // 移动buff: 将装备中或未装备中的buff移动到任意位置, 如果该位置已存在buff,则交换两buff位置
        public void MoveBuff(bool isFromEquipped, int FromIndex, bool isToEquipped, int ToIndex)
        {
            List<BuffItem> sourceList = isFromEquipped ? EquippedBuffs : UnequippedBuffs;
            List<BuffItem> baseList = isToEquipped ? EquippedBuffs : UnequippedBuffs;

            if (FromIndex < 0 || FromIndex >= sourceList.Count || ToIndex < 0)
            {
                Debug.LogError("索引无效！");
                return;
            }

            if (ToIndex >= baseList.Count)
            {
                // 如果目标索引超出目标列表长度，则扩展列表（这个逻辑可以根据实际需求调整）
                for (int i = baseList.Count; i <= ToIndex; i++)
                {
                    var empty = new BuffItem();
                    empty.Clear();
                    baseList.Add(empty); // 或者创建一个默认的BuffItem实例
                }
            }

            BuffItem temp = sourceList[FromIndex];
            BuffItem temp2 = baseList[ToIndex];

            sourceList[FromIndex] = temp2;
            baseList[ToIndex] = temp;
            Debug.Log($"--- 交换了");
        }
    }


    public static class GameSetting
    {
        //目前技能
        public const int SkillCountOnBar = 2;
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
        AckingNorAck = 101,
        TimeSpeedStop = 102,
        EnemyDeadEvent = 103,
        EnemyGroupEndEvent = 104,
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


