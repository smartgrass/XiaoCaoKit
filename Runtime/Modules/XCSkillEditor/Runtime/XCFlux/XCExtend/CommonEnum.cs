

namespace XiaoCao
{
    public enum TransfromType
    {
        [EnumLabel("玩家坐标")]
        PlyerPos = 0, //释放时,跟随玩家坐标
        [EnumLabel("跟随玩家")]
        PlayerTransfrom = 1, //一直跟随玩家坐标, 如buff
        [EnumLabel("启动技能位置")]
        StartPos = 2, //技能启动时的玩家的参考系
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
        Sector = 2,  //扇形
        Other = 3,
    }

}