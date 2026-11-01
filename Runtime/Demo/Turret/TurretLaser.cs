using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台激光: 瞬间命中(不生成飞行物), 用 LineRenderer 做短暂光束表现。
    /// 从炮口沿炮口朝向扫描, 命中第一个敌方角色即结算伤害, 撞墙则止于墙面。
    /// </summary>
    public class TurretLaser : MonoBehaviour
    {
        [Tooltip("光束线条, 没有命中时也要能显示, 建议用两端对齐的简单材质")]
        public LineRenderer line;

        [Tooltip("光束停留时间(秒)")]
        public float showTime = 0.12f;

        private float hideTimer;

        private void Awake()
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.enabled = false;
        }

        private void Update()
        {
            if (hideTimer <= 0f)
            {
                return;
            }

            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                line.enabled = false;
            }
        }

        /// <summary>
        /// 发射激光: 沿炮台算好的发射方向(炮口指向准星落点)射出, 命中敌方角色后结算伤害
        /// </summary>
        /// <param name="turret">所属炮台</param>
        /// <param name="ownerRole">操控者, 决定阵营与攻击力</param>
        /// <param name="skillId">伤害/表现配置</param>
        /// <param name="range">射程</param>
        /// <param name="dir">发射方向, 与子弹/导弹保持同一套准星落点算法</param>
        public void Fire(Turret turret, Role ownerRole, string skillId, float range, Vector3 dir)
        {
            Vector3 origin = turret.muzzle.position;
            Vector3 endPos = origin + dir * range;

            //角色碰撞体在 PLAYER/ENEMY 层, 阻挡层由炮台配置(空气墙不在其中)
            int mask = turret.AimRayMask;
            RaycastHit[] hits = Physics.RaycastAll(origin, dir, range, mask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                //炮台自身与操控者不挡光(视角在炮台后方时, 光束会先穿过自己)
                if (turret.IsSelfCollider(hit.collider))
                {
                    continue;
                }

                //角色碰撞体挂在子节点上, IdRole 在刚体所在节点
                Rigidbody body = hit.collider.attachedRigidbody;
                if (body == null || !body.TryGetComponent(out IdRole idRole) || !(idRole.GetEntity() is Role role))
                {
                    //先撞到的不是角色, 视为被场景挡住
                    endPos = hit.point;
                    break;
                }

                if (role.team == ownerRole.team || role.IsDie)
                {
                    //同阵营角色不挡光, 继续往后找
                    continue;
                }

                endPos = hit.point;
                ApplyDamage(ownerRole, skillId, role, dir, origin);
                break;
            }

            ShowBeam(origin, endPos);
        }

        /// <summary>按操控者属性结算一次激光伤害</summary>
        private void ApplyDamage(Role ownerRole, string skillId, Role target, Vector3 dir, Vector3 origin)
        {
            AtkInfo info = AtkInfoHelper.CreatInfo(ownerRole, skillId);
            info.ackObjectPos = origin;
            info.hitPos = target.transform.position;
            info.hitDir = dir.SetY(0f);

            AtkInfoHelper.ProcessAtkInfo(info);
            target.OnDamage(info);
        }

        private void ShowBeam(Vector3 start, Vector3 end)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.enabled = true;
            hideTimer = showTime;
        }
    }
}
