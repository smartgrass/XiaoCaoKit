using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao.Battle
{
    public class PullRole : MonoBehaviour
    {
        public float radius = 18;

        public float force = -5;

        public float triggerSpan = 0.2f;

        public int team;

        [CurveRange(0, 0, 1, 1)] public AnimationCurve forceCurve;

        private float lastTriggerTime = 0f;

        private List<Role> _list = new List<Role>();

        //检测敌人,将敌人拉向自己
        public void FixedUpdate()
        {
            if (BattleData.IsTimeStop)
            {
                return;
            }

            //触发间隔, 每triggerSpan触发一次
            if (!(Time.time - lastTriggerTime < triggerSpan))
            {
                _list = RoleMgr.Inst.SearchEnemyInRadius(transform.position, radius, team);
            }

            lastTriggerTime = Time.time;

            foreach (var role in _list)
            {
                if (!role.Enable)
                {
                    continue;
                }

                // Calculate
                Vector3 dir = role.transform.position - transform.position;
                float dis = dir.magnitude;
                dir.Normalize();
                float curForce = forceCurve.Evaluate(radius / radius + (dis * 1.5f));

                dir *= curForce * force * Time.fixedDeltaTime;

                // Apply
                role.Movement.cc.Move(dir);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}