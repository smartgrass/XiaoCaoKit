

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
    /// 比较枚举,使用int的好处,序列化方便
    /// 而使用odin序列化则没啥问题
    /// </summary>
    public enum RoleType
    {
        Enemy = 0,
        Player = 1,
    }
    public enum MeshType
    {
        Box = 0,
        Sphere = 1,
        [InspectorName("扇形柱 (半径,高,角度)")]
        Sector = 2,
    }

}