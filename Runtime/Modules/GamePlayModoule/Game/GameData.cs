using System;
using System.Collections.Generic;

namespace XiaoCao
{
    public class GameData
    {
        //不会清空
        public static GameDataCommon commonData = new GameDataCommon();
        //定时请空
        public static BattleData battleData = new BattleData();
    }

    public class GameDataCommon
    {
        public static GameDataCommon Current => GameData.commonData;

        public PlayMode PlayMode;

        public GameState gameState;

        internal Player0 Player0;

        public bool loadMod = false;
    }

    public class BattleData
    {
        public static BattleData Current => GameData.battleData;

        public HashSet<string> map = new HashSet<string>();
        public Dictionary<string,int> tempIntDic = new Dictionary<string,int>();
    

    }

    public static class GameSetting
    {

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

        PlayerEvent  = 100, //分界线
        AckingNorAck = 101,

    }



    public static class EventTypeExtend
    {
        public static int Int(this EventType t) 
        {
            return (int)t;
        }
    }

}


