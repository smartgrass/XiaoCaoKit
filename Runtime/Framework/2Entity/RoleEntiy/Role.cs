
namespace XiaoCao
{

    public abstract class Role: HealthBehavior
    {
        ///<see cref="RoleTypeCode"/>
        public abstract int RoleType { get; }
        public virtual IComponentData data { get; }
        public virtual ISharedComponentData componentData { get; }

    }

    /// <summary>
    /// 比较枚举,使用int的好处,序列化方便
    /// </summary>
    public static class RoleTypeCode {
        public static int Enemy = 0;
        public static int Player = 1;
    }


    public class Player : Role
    {
        public override int RoleType => RoleTypeCode.Player;
    }


    public class Enemy : Role
    {
        public override int RoleType => RoleTypeCode.Enemy;
    }


}