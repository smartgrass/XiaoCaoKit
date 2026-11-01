using cfg;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台飞行物基类: 复用 <see cref="Atker"/> 的碰撞命中判定,
    /// 命中一次(或超时)后回收进对象池; 具体飞行方式由子类实现。
    /// 预制体要求: 与脚本同一个 GameObject 上挂一个 isTrigger 的碰撞体。
    /// </summary>
    public abstract class TurretProjectile : Atker
    {
        [Header("通用")]
        [Tooltip("最长存活时间(秒), 超时自动回收")]
        public float lifeTime = 4f;

        [Tooltip("打在墙/地面上时播放的击中特效名(Assets/_Res/SkillPrefab/Hit 下); 留空则用 skillId 对应 SkillSetting 的 HitEffect")]
        public string sceneHitEffect = "";

        [Header("发射音效")]
        [Tooltip("发射音效 id(Assets/_Res/Audio 下, 不带后缀); 留空则用炮台攻击配置里的 fireSfx 兜底")]
        public string launchSfx = "";

        [Range(0f, 1f)]
        [Tooltip("发射音效音量")]
        public float launchSfxVolume = 1f;

        protected Turret turret;
        protected Role ownerRole;
        protected AssetPool pool;

        /// <summary>发射时锁定的目标, 可为空</summary>
        protected Role targetRole;

        /// <summary>准心落点, 没有锁定目标时用它当作落点</summary>
        protected Vector3 aimPoint;

        protected float timer;

        /// <summary>出生位置, 用来判断是否飞到了准星落点</summary>
        private Vector3 spawnPos;

        /// <summary>从炮口到准星落点的距离, 飞到这里就算打在落点上</summary>
        private float travelLimit;

        /// <summary>这次射击的落点是否真的打在场景上(没打中任何东西时只是射程尽头, 不播特效)</summary>
        private bool hitScene;

        /// <summary>已经命中过角色, 不要再做落点判定</summary>
        private bool consumed;

        /// <summary>由炮台在发射时调用: 初始化伤害信息、归位到炮口并开始飞行</summary>
        public virtual void Launch(Turret turret, Role ownerRole, string skillId, float range,
            Vector3 aimPoint, Vector3 fireDir, AssetPool pool, Role target, bool hitScene = false)
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
            travelLimit = Vector3.Distance(spawnPos, aimPoint);
            this.hitScene = hitScene;
            consumed = false;

            PlayLaunchSfx();

            OnLaunched(range);
        }

        /// <summary>
        /// 发射瞬间播一次音效。留在飞行物上而不是炮台上, 是为了让不同弹道各配各的声音;
        /// 没配时由炮台用攻击配置的 fireSfx 兜底(见 Turret.LaunchProjectile)
        /// </summary>
        protected virtual void PlayLaunchSfx()
        {
            if (string.IsNullOrEmpty(launchSfx))
            {
                return;
            }

            SoundMgr.Inst.PlayClip(launchSfx, launchSfxVolume);
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

            OnMove(XCTime.deltaTime);

            //飞满到准星落点的距离还没撞到角色: 说明打在墙/地面上, 在这里炸开
            CheckAimPointReached();
        }

        /// <summary>飞到准星落点就收尾: 命中场景时播击中特效, 打空则直接消失</summary>
        private void CheckAimPointReached()
        {
            if (consumed || travelLimit <= 0.01f)
            {
                return;
            }

            if ((transform.position - spawnPos).sqrMagnitude < travelLimit * travelLimit)
            {
                return;
            }

            if (hitScene)
            {
                PlayHitEffect(aimPoint, -transform.forward);
            }

            Recycle();
        }

        /// <summary>
        /// 在世界坐标播一次击中特效(从公共对象池取, 池子负责延时回收)。
        /// 名字优先用 <see cref="sceneHitEffect"/>, 留空时回退到 skillId 对应的 SkillSetting.HitEffect,
        /// 与打中角色时 <see cref="HitHelper"/> 播的是同一个特效
        /// </summary>
        protected void PlayHitEffect(Vector3 pos, Vector3 forward)
        {
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
            pool.Release(gameObject);
        }
    }
}
