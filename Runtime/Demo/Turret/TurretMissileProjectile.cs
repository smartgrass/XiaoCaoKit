using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 抛物线导弹: 从炮口沿二次贝塞尔曲线飞向锁定目标,
    /// 没有锁定目标时飞向准心落点
    /// </summary>
    public class TurretMissileProjectile : TurretProjectile
    {
        [Tooltip("飞行速度(米/秒), 用于换算整段抛物线的时间")]
        public float speed = 18f;

        [Tooltip("抛物线最高点相对起终点连线的抬高量")]
        public float arcHeight = 3f;

        [Tooltip("目标身上的瞄准高度偏移")]
        public float targetOffsetY = 1f;

        private Vector3 startPos;
        private Vector3 endPos;
        private Vector3 handle;
        private Vector3 fallbackEndPos;
        private float moveTime;
        private float elapsed;

        protected override void OnLaunched(float range)
        {
            startPos = transform.position;
            //没有锁定目标时飞向准心落点, 发射瞬间定下, 之后飞行中朝向变化不影响
            fallbackEndPos = aimPoint;
            endPos = GetTargetPos();
            moveTime = Mathf.Max(0.2f, Vector3.Distance(startPos, endPos) / speed);
            handle = GetHandle();
        }

        protected override void OnMove(float dt)
        {
            elapsed += dt;

            //目标还活着就持续修正落点
            if (targetRole != null && !targetRole.IsDie)
            {
                endPos = GetTargetPos();
                handle = GetHandle();
            }

            float t = Mathf.Clamp01(elapsed / moveTime);
            Vector3 pos = MathTool.GetBezierPoint2(startPos, endPos, handle, t);

            Vector3 dir = pos - transform.position;
            if (!dir.IsZore())
            {
                transform.forward = dir;
            }

            transform.position = pos;

            if (t >= 1f)
            {
                Recycle();
            }
        }

        private Vector3 GetTargetPos()
        {
            if (targetRole != null && !targetRole.IsDie)
            {
                return targetRole.transform.position + Vector3.up * targetOffsetY;
            }

            return fallbackEndPos;
        }

        private Vector3 GetHandle()
        {
            Vector3 center = Vector3.Lerp(startPos, endPos, 0.5f);
            return center + Vector3.up * arcHeight;
        }
    }
}
