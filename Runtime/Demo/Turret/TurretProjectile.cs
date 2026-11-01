using cfg;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台飞行物基类: 复用 <see cref="Atker"/> 的碰撞命中判定,
    /// 命中角色/场景, 或者飞满 lifeTime 后回收进对象池; 具体飞行方式由子类实现。
    /// 预制体要求: 与脚本同一个 GameObject 上挂一个 isTrigger 的碰撞体。
    /// </summary>
    public abstract class TurretProjectile : Atker
    {
        [Header("通用")]
        [Tooltip("最长存活时间(秒), 超时自动回收")]
        public float lifeTime = 4f;

        [Tooltip("打在墙/地面上时播放的击中特效名(Assets/_Res/SkillPrefab/Hit 下); 留空则用 skillId 对应 SkillSetting 的 HitEffect")]
        public string sceneHitEffect = "";

        [Tooltip("飞行途中检测的场景层; 勾 WallBlock(实体墙)、Ground(地面)、Default(未分层楼板)。" +
                 "Wall 是空气墙, 不挡子弹, 所以不要勾")]
        public LayerMask sceneCollisionMask = Layers.WALL_BLOCK_MASK | Layers.GROUND_MASK | Layers.DEFAULT_MASK;

        protected Turret turret;
        protected Role ownerRole;
        protected AssetPool pool;

        /// <summary>发射时锁定的目标, 可为空</summary>
        protected Role targetRole;

        /// <summary>准心落点, 没有锁定目标时用它当作落点</summary>
        protected Vector3 aimPoint;

        protected float timer;

        /// <summary>出生位置, 用来忽略"炮口卡在墙里"的刚出发命中</summary>
        private Vector3 spawnPos;

        /// <summary>已经命中过角色, 不要重复结算</summary>
        private bool consumed;

        /// <summary>命中场景时播的音效(发射时从炮台攻击配置取, 不在飞行物上重复配置)</summary>
        private AudioClip hitSfx;

        /// <summary>命中音效音量</summary>
        private float hitSfxVolume = 1f;

        /// <summary>已经回收过, 避免回收后继续做碰撞/落点判定</summary>
        private bool recycled;

        /// <summary>场景碰撞检测的复用缓冲, 避免每帧分配</summary>
        private static readonly RaycastHit[] sceneHits = new RaycastHit[8];

        /// <summary>离出生点这么近(米)的场景命中视为"起点被包住"(炮口卡在空气墙里), 忽略掉</summary>
        private const float SceneHitIgnoreNear = 0.3f;

        /// <summary>由炮台在发射时调用: 初始化伤害信息、归位到炮口并开始飞行</summary>
        public virtual void Launch(Turret turret, Role ownerRole, string skillId, float range,
            Vector3 aimPoint, Vector3 fireDir, AssetPool pool, Role target)
        {
            this.turret = turret;
            this.ownerRole = ownerRole;
            this.pool = pool;
            this.aimPoint = aimPoint;
            targetRole = target;
            timer = 0f;
            curTriggerTime = 0;

            //伤害倍率/暴击按操控者的属性算, 命中层级按阵营取反
            AtkInfo info = AtkInfoHelper.CreatInfo(ownerRole, skillId);
            InitAtkInfo(info);
            AddTriggerByCollider();

            //炮台飞行物命中一次即消耗
            maxTriggerTime = 1;

            //出生朝向用炮台算好的发射方向(炮口指向准星落点), 与准星命中的位置一致
            transform.SetPositionAndRotation(turret.muzzle.position, Quaternion.LookRotation(fireDir, Vector3.up));

            spawnPos = transform.position;
            consumed = false;
            recycled = false;

            //音效统一配在炮台的攻击配置上, 这里只取击中音效(发射音效由炮台在 Launch 前播)
            hitSfx = turret.attackConfig.hitSfx;
            hitSfxVolume = turret.attackConfig.hitSfxVolume;

            OnLaunched(range);
        }

        protected abstract void OnLaunched(float range);

        protected virtual void Update()
        {
            timer += XCTime.deltaTime;
            if (timer >= lifeTime)
            {
                Recycle();
                return;
            }

            Vector3 prevPos = transform.position;
            OnMove(XCTime.deltaTime);

            if (recycled)
            {
                return;
            }

            //撞到墙/地面就收尾; 没撞到任何东西就一路飞, 由 lifeTime 兜底回收
            CheckSceneHit(prevPos);
        }

        /// <summary>
        /// 沿这一帧的位移做扫掠检测, 撞到墙/地面就播击中特效+音效并回收。
        /// 准星射线不检测地面(否则准星会被台阶/楼板挡住), 所以地面命中只能靠这里补
        /// </summary>
        private bool CheckSceneHit(Vector3 fromPos)
        {
            if (consumed || sceneCollisionMask.value == 0)
            {
                return false;
            }

            Vector3 delta = transform.position - fromPos;
            float dist = delta.magnitude;
            if (dist <= 0.0001f)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(fromPos, delta / dist, sceneHits, dist,
                sceneCollisionMask.value, QueryTriggerInteraction.Collide);

            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = sceneHits[i];

                //炮台自身与操控者不算命中
                if (turret != null && turret.IsSelfCollider(hit.collider))
                {
                    continue;
                }

                //飞行物自己在 Default 层, 也在检测掩码内, 跳过自身节点才能避免"出门就撞自己"
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                //出生点被墙体包住时, 刚出发就命中的要忽略, 否则子弹一发射就炸
                if ((hit.point - spawnPos).sqrMagnitude < SceneHitIgnoreNear * SceneHitIgnoreNear)
                {
                    continue;
                }

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = i;
                }
            }

            if (best < 0)
            {
                return false;
            }

            consumed = true;
            PlayHitEffect(sceneHits[best].point, sceneHits[best].normal);
            Recycle();
            return true;
        }

        /// <summary>
        /// 在世界坐标播一次击中特效(从公共对象池取, 池子负责延时回收)。
        /// 名字优先用 <see cref="sceneHitEffect"/>, 留空时回退到 skillId 对应的 SkillSetting.HitEffect,
        /// 与打中角色时 <see cref="HitHelper"/> 播的是同一个特效
        /// </summary>
        protected void PlayHitEffect(Vector3 pos, Vector3 forward)
        {
            PlayHitSfx();

            string effectName = sceneHitEffect;

            if (string.IsNullOrEmpty(effectName) && ackInfo != null)
            {
                SkillSetting setting = ackInfo.GetSkillSetting;
                if (setting != null)
                {
                    effectName = setting.HitEffect;
                }
            }

            if (string.IsNullOrEmpty(effectName))
            {
                return;
            }

            GameObject effect = RunTimePoolMgr.Inst.GetHitEffect(effectName);
            if (effect == null)
            {
                return;
            }

            //墙上的特效不该跟着角色移动, 挂在场景根节点下
            effect.SetActive(true);
            effect.transform.SetParent(null, true);
            effect.transform.position = pos;
            effect.transform.forward = forward;
        }

        /// <summary>命中场景(墙/地面)的音效, 与击中特效一起播</summary>
        private void PlayHitSfx()
        {
            if (hitSfx != null)
            {
                SoundMgr.Inst.PlayClip(hitSfx, hitSfxVolume);
            }
        }

        protected abstract void OnMove(float dt);

        /// <summary>
        /// 命中判定: 只打不同阵营的角色, 击退方向用飞行物自身的朝向
        /// (基类 <see cref="Atker"/> 用的是父节点朝向, 对象池的父节点不是发射者, 这里改成自身朝向)
        /// </summary>
        public override void ReceiveTriggerEnter(Collider other)
        {
            //角色碰撞体挂在子节点上, IdRole 在刚体所在节点
            Rigidbody body = other.attachedRigidbody;
            if (body == null)
            {
                return;
            }

            if (!body.TryGetComponent(out IdRole idRole) || !(idRole.GetEntity() is Role role))
            {
                return;
            }

            if (role.team == ackInfo.team || role.IsDie)
            {
                return;
            }

            if (consumed)
            {
                return;
            }

            consumed = true;
            ackInfo.ackObjectPos = transform.position;
            ackInfo.hitPos = other.ClosestPointOnBounds(transform.position);
            ackInfo.hitDir = transform.forward.SetY(0f);

            OnHitRole(role);
        }

        public override void OnTriggerTimeOut()
        {
            base.OnTriggerTimeOut();
            Recycle();
        }

        /// <summary>回收进对象池(池子会顺带把物体设为未激活)</summary>
        protected virtual void Recycle()
        {
            recycled = true;
            pool.Release(gameObject);
        }
    }
}
