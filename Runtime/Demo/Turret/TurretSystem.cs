using System.Collections.Generic;

namespace XiaoCao
{
    /// <summary>
    /// 炮台注册表与玩家交互入口:
    /// 记录场景内所有炮台、玩家正在操控的炮台、以及玩家附近最近的可交互炮台。
    /// 玩家输入在 <see cref="TryConsumePlayerInput"/> 里被改道到炮台, 所以角色自身不会再做普攻/技能。
    /// </summary>
    public static class TurretSystem
    {
        private static readonly List<Turret> turrets = new List<Turret>();

        /// <summary>玩家正在操控的炮台</summary>
        public static Turret PlayerOperating { get; private set; }

        /// <summary>玩家附近可交互的炮台(多个重叠时取最近的一个)</summary>
        public static Turret Nearby { get; private set; }

        private static float nearbyDistance = float.MaxValue;

        public static void Register(Turret turret)
        {
            if (turrets.Contains(turret))
            {
                return;
            }

            turrets.Add(turret);
        }

        public static void Unregister(Turret turret)
        {
            turrets.Remove(turret);

            if (PlayerOperating == turret)
            {
                PlayerOperating = null;
            }

            if (Nearby == turret)
            {
                Nearby = null;
                nearbyDistance = float.MaxValue;
            }
        }

        public static void SetPlayerOperating(Turret turret)
        {
            PlayerOperating = turret;
        }

        /// <summary>退出玩家当前操控的炮台, 供 HUD 顶部的退出按钮与 Esc 键共用</summary>
        public static void ExitPlayerControl()
        {
            if (PlayerOperating != null)
            {
                PlayerOperating.ExitControl();
            }
        }

        /// <summary>炮台每帧上报与本地玩家的距离, 由这里挑出最近的一个作为可交互目标</summary>
        public static void ReportNearby(Turret turret, bool isNear, float distance = float.MaxValue)
        {
            if (!isNear)
            {
                if (Nearby == turret)
                {
                    Nearby = null;
                    nearbyDistance = float.MaxValue;
                }

                return;
            }

            if (Nearby != null && Nearby != turret && nearbyDistance <= distance)
            {
                return;
            }

            Nearby = turret;
            nearbyDistance = distance;
        }

        /// <summary>
        /// 玩家输入接管: 操控炮台时把全部输入交给炮台(旋转/发射/离开);
        /// 靠近炮台时把攻击键改为打开交互界面。
        /// 返回 true 表示输入已被炮台消费, 调用方应跳过角色自身的操作
        /// </summary>
        public static bool TryConsumePlayerInput(Player0 player)
        {
            if (PlayerOperating != null)
            {
                PlayerOperating.OnPlayerInput(player);
                return true;
            }

            if (Nearby == null || !Nearby.CanInteract(player))
            {
                return false;
            }

            if (!player.playerData.inputData.inputs[(int)InputKey.NorAck])
            {
                return false;
            }

            Nearby.OpenInteractionPanel();
            return true;
        }
    }
}
