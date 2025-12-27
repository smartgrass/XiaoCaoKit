using Fantasy.Pool;
using System;
using System.Collections;
using TEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao.Buff
{


    public class BuffEffect_MagicMissile : BaseBuffEffect
    {
        public override EBuff Buff => EBuff.MagicMissile;
        public override bool HasLife => true;

        public const string BulletPath = "Assets/_Res/SkillPrefab/Buff/B_MagicMissile.prefab";
        public const string EffectPath = "Assets/_Res/SkillPrefab/Buff/B_MagicMissile_Effect.prefab";

        public string Key { get; set; }

        public AssetPool bulletPool;

        public AssetPool effectPool;

        public LoopTimer loopTimer;

        public PlayerAtkTimer atkTimers;

        public Player0 player;

        private int _triggerTime;

        //自动召唤魔法导弹,cd{5}s
        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            Key = key;
            this.TargetId = targetId;
            player = TargetId.GetPlayerById();
            atkTimers = player.component.atkTimer;
            atkTimers.AddKey(key, buff.addInfo[0]);
            loopTimer = new LoopTimer(0.3f);
            bulletPool = PoolMgr.Inst.GetOrCreatPool(BulletPath);

            GameEvent.AddEventListener<int, string>(EGameEvent.PlayerPlaySkill.ToInt(), OnPlaySkill);
        }
        public override void RemoveEffect()
        {
            var player = TargetId.GetPlayerById(); ;
            GameEvent.RemoveEventListener<int, string>(EGameEvent.PlayerPlaySkill.ToInt(), OnPlaySkill);
        }

        private void OnPlaySkill(int id, string skillId)
        {
            if (id != TargetId)
            {
                return;
            }
            if (!atkTimers.IsSkillReady(Key))
            {
                return;
            }

            if (player.FindEnemy(out Role findRole, angle: 180) && !findRole.IsDie)
            {
                //触发
                atkTimers.SetSkillEnterCD(Key);
                loopTimer.Reset();
                _triggerTime = 3;
            }
        }

        public override void Update()
        {
            if (_triggerTime > 0)
            {
                //间隔 0.5s 生成一个共3个, 分别发射导弹
                loopTimer.TickPeriodic(XCTime.deltaTime, out var executePeriodicTick);
                if (executePeriodicTick)
                {
                    CreatInstant();
                }
            }
        }

        //创建导弹
        private void CreatInstant()
        {
            var go = bulletPool.Get();
            var bullet = go.GetComponent<Bullet_MagicMissile>();
            bullet.InitWithPlayer(player);

            //位置 & 朝向
            _triggerTime--;
        }
    }


    public enum EBulletState
    {
        Stop,
        Start,
        Running,
        Finish,
    }

}
