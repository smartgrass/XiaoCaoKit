using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 功能件注册表: 功能件自己在 OnEnable/OnDisable 里注册与注销, 这里负责"找周围能用的功能件"。
    /// 只返回距离最近、并且当前能被该角色占用的一个。
    /// </summary>
    public static class FunctionItemMgr
    {
        private static readonly List<IFunctionItem> items = new List<IFunctionItem>();

        public static void Register(IFunctionItem item)
        {
            if (item == null || items.Contains(item))
            {
                return;
            }

            items.Add(item);
        }

        public static void Unregister(IFunctionItem item)
        {
            if (item == null)
            {
                return;
            }

            items.Remove(item);
        }

        /// <summary>
        /// 找 position 周围 radius 米内能被 role 占用的功能件; 多个时取最近的, 没有则返回 null。
        /// 被别的角色占着的会被跳过, 所以"周围有功能件但全被占用"时也返回 null,
        /// 需要区分这两种情况的用 <see cref="FindNearestAny"/>。
        /// </summary>
        public static IFunctionItem FindNearest(Vector3 position, float radius, Role role)
        {
            return FindNearestInternal(position, radius, role, false);
        }

        /// <summary>
        /// 找 position 周围 radius 米内最近的功能件, **不管它当前能不能被占用**。
        /// 只用来判断"附近到底有没有功能件、是不是被别人占着", 真要占用请用 <see cref="FindNearest"/>。
        /// </summary>
        public static IFunctionItem FindNearestAny(Vector3 position, float radius)
        {
            return FindNearestInternal(position, radius, null, true);
        }

        /// <summary>
        /// 该角色当前是否正在操作某个功能件。
        /// 操控期间受击不该把 AI 状态/操控锁打乱(操控要等 break 才断开), 供受击回调判断
        /// </summary>
        public static bool IsOperating(Role role)
        {
            if (role == null)
            {
                return false;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                IFunctionItem item = items[i];

                if (item is UnityEngine.Object unityObj && unityObj == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                if (item.Operator == role)
                {
                    return true;
                }
            }

            return false;
        }

        private static IFunctionItem FindNearestInternal(Vector3 position, float radius, Role role, bool ignoreUsable)
        {
            IFunctionItem nearest = null;
            float nearestSqr = radius * radius;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                IFunctionItem item = items[i];

                //接口引用拿不到 Unity 的 == 重载, 所以绕到 UnityEngine.Object 上判空
                if (item is UnityEngine.Object unityObj && unityObj == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                if (item.ItemTransform == null)
                {
                    continue;
                }

                if (!ignoreUsable && !item.CanBeOperatedBy(role))
                {
                    continue;
                }

                float sqr = (item.ItemTransform.position - position).sqrMagnitude;
                if (sqr > nearestSqr)
                {
                    continue;
                }

                nearestSqr = sqr;
                nearest = item;
            }

            return nearest;
        }
    }
}
