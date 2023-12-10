

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

    public enum RoleType
    {
        Player,
        Enemy
    }

}