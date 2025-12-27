using System.Collections;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using XiaoCao;
using XiaoCao.Buff;

namespace XiaoCaoKit.Runtime.Demo.Buff
{
    //攻击敌人时触发闪电传递,造成{0:P0}的伤害,冷缩{1:F2}s
    [XCBuff]
    public class BuffEffect_ThunderExplode : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.ThunderExplode;
        private const string ThunderPrefabPath = "Assets/_Res/SkillPrefab/Buff/B_ThunderLine.prefab";
        private const string HitPrefabPath = "Assets/_Res/SkillPrefab/Buff/B_ThunderHit.prefab";
        private float cdTime;
        private float lastTriggerTime;
        private float damageRate;
        public float radius = 8f;
        private const int MaxChainTargets = 5; // 最大连锁目标数
        private Role ownerRole;
        private readonly Vector3 offsetY = Vector3.up;

        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            this.TargetId = targetId;
            damageRate = buff.addInfo[0];
            ownerRole = TargetId.GetRoleById();

            // 解析参数，第一个是冷却时间
            cdTime = buff.addInfo[1];
            lastTriggerTime = -cdTime; // 确保第一次触发不会受冷却限制


            // 注册暴击事件回调
            GameEvent.AddEventListener<int, bool, AtkInfo>(EGameEvent.RoleHurt.ToInt(), OnRoleHurt);
        }

        public override void RemoveEffect()
        {
            GameEvent.RemoveEventListener<int, bool, AtkInfo>(EGameEvent.RoleHurt.ToInt(), OnRoleHurt);
            base.RemoveEffect();
        }

        private void OnRoleHurt(int entityId, bool isPlayer, AtkInfo info)
        {
            // 检查是否是目标实体暴击
            if (info.atker != TargetId)
                return;

            // 检查冷却时间
            if (Time.time - lastTriggerTime < cdTime)
                return;

            // 触发闪电传递效果
            TriggerThunderExplode(info.hitPos);
        }

        private void TriggerThunderExplode(Vector3 pos)
        {
            // 更新最后触发时间
            lastTriggerTime = Time.time;
            var enemyList = RoleMgr.Inst.SearchEnemyInRadius(pos, radius, ownerRole.team);

            if (enemyList.Count == 0)
                return;


            // 创建闪电效果并连锁攻击敌人
            ChainLightningAttack(pos, enemyList);

            Debug.Log($"Thunder explode triggered! Attacked {Mathf.Min(enemyList.Count, MaxChainTargets)} targets.");
        }
        
        private void ChainLightningAttack(Vector3 startPos, List<Role> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return;

            // 限制连锁目标数量
            int targetCount = Mathf.Min(enemies.Count, MaxChainTargets);
            AtkInfo atkInfo = AtkInfoHelper.CreatInfo(ownerRole, Buff.ToString());
            int damage = Mathf.RoundToInt(ownerRole.PlayerAttr.Atk * damageRate);
            atkInfo.atk = damage;
            AtkInfoHelper.ProcessAtkInfo(atkInfo);
            SoundMgr.Inst.PlayHitAudio("Ele");

            // 获取第一个目标（最近的敌人）
            Role from = GetClosestEnemy(startPos, enemies);
            DamageRole(from, atkInfo);
            // 连锁攻击其他目标
            for (int i = 1; i < targetCount; i++)
            {
                // 从当前目标寻找下一个最近的目标
                Role to = GetClosestEnemy(from.transform.position, enemies);
                if (to == null)
                    break;

                // 对下一个目标造成伤害
                AttackTarget(from, to, atkInfo);
                from = to;
            }
        }

        private Role GetClosestEnemy(Vector3 position, List<Role> enemies)
        {
            Role closest = null;
            float closestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDie)
                    continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            // 从列表中移除已选中的敌人，避免重复选择
            if (closest != null)
            {
                enemies.Remove(closest);
            }

            return closest;
        }

        private void AttackTarget(Role from, Role to, AtkInfo atkInfo)
        {
            if (to == null || to.IsDie)
                return;

            // 计算伤害（基于攻击者攻击力和伤害倍率）
            atkInfo.hitPos = to.transform.position;

            DamageRole(to, atkInfo);


            var thunder = PoolMgr.Inst.Get(ThunderPrefabPath);
            LineRenderer lineRenderer = thunder.GetComponent<LineRenderer>();


            // 启动协程来处理闪电线的动态效果
            ownerRole.idRole.StartCoroutine(ThunderEffectEffect(lineRenderer, from.transform, to.transform));
        }

        private void DamageRole(Role to, AtkInfo atkInfo)
        {
            // 对目标造成伤害
            to.OnDamage(atkInfo);
            var hit = PoolMgr.Inst.Get(HitPrefabPath, 0.8f);
            atkInfo.hitPos = to.transform.position + offsetY;
            hit.transform.position = to.transform.position + offsetY;
        }

        private IEnumerator ThunderEffectEffect(LineRenderer lineRenderer, Transform from,
            Transform to)
        {
            // // 创建渐变颜色：从透明->不透明->透明
            // var orgColor = lineRenderer.startColor;
            // var endColor = lineRenderer.endColor;

            lineRenderer.positionCount = 2;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 更新线条位置
                lineRenderer.SetPosition(0, from.position + offsetY);
                lineRenderer.SetPosition(1, to.position + offsetY);

                // // 更新颜色透明度
                float t = elapsed / duration;
                lineRenderer.colorGradient.alphaKeys[1].alpha = Mathf.Lerp(1, 0, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 确保最终位置正确
            lineRenderer.SetPosition(0, from.position + offsetY);
            lineRenderer.SetPosition(1, to.position + offsetY);

            // 动画完成，回收对象
            PoolMgr.Inst.Release(ThunderPrefabPath, lineRenderer.gameObject);
        }
    }
}