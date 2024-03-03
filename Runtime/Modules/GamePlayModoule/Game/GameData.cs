using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace XiaoCao
{
    public class GameData
    {
        //不会清空
        public static GameDataCommon commonData = new GameDataCommon();
        //定时请空
        public static BattleData battleData = new BattleData();

        public static PlayerSaveData playerSaveData = new PlayerSaveData();
    }

    public class GameDataCommon
    {
        public static GameDataCommon Current => GameData.commonData;

        public PlayMode playMode;

        public GameState gameState;

        public Player0 player0;

        public bool loadMod = false;

    }

    public class BattleData
    {
        public static BattleData Current => GameData.battleData;

        public HashSet<string> map = new HashSet<string>();

        public Dictionary<string, int> tempIntDic = new Dictionary<string, int>();


    }



    public static class GameSetting
    {
        public const int SkillCountOnBar = 6;

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

    public enum EventType : uint
    {
        None = 0,
        GameStateChange = 1,
        GameStartFinsh = 2,
        CameraChange = 3,
        RoleChange = 4,

        PlayerEvent = 100, //分界线
        AckingNorAck = 101,

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
        public static int Int(this EventType t)
        {
            return (int)t;
        }
    }

}


