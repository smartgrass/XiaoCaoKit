

using UnityEngine;

namespace XiaoCao
{
    public enum TransfromType
    {
        [InspectorName("玩家坐标")]
        PlyerPos = 0, //释放时,跟随玩家坐标
        [InspectorName("跟随玩家")]
        PlayerTransfrom = 1, //一直跟随玩家坐标, 如buff
        [InspectorName("启动技能位置")]
        StartPos = 2, //技能启动时的玩家的参考系
        [InspectorName("特殊节点")]
        OtherTransfrom
    }


    /// <summary>
    /// 代码层类型判断
    /// </summary>
    public enum RoleType
    {
        Enemy = 0,
        Player = 1,
    }

    //身份类型
    public enum RoleIdentityType
    {
        Enemy = 0,
        Player = 1,
        [InspectorName("友军")]
        PlayerFriend = 2,
    }
    
    public enum MeshType
    {
        Box = 0,
        Sphere = 1,
        [InspectorName("扇形柱 (半径,高,角度)")]
        Sector = 2,
    }

}