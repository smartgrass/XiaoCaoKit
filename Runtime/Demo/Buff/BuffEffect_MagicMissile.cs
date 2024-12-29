using Fantasy.Pool;
using System;
using System.Collections;
using UnityEngine;

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

        private int _triggerIndex;

        private bool hasInit;

        //自动召唤魔法导弹,cd{5}s
        public override void ApplyEffect(string key, BuffInfo buff, int targetId)
        {
            Key = key;
            this.TargetId = targetId;
            player = TargetId.GetPlayerById();
            player.UpdateEvent += Update;
            atkTimers = player.component.atkTimers;
            atkTimers.AddKey(key, buff.addInfo[0]);
            loopTimer = new LoopTimer(0.5f);

            if (!hasInit)
            {
                hasInit = true;
                bulletPool = PoolMgr.Inst.GetOrCreatPool(BulletPath);
            }
        }

        public override void RemoveEffect()
        {
            var player = TargetId.GetPlayerById();
            player.UpdateEvent += Update;
        }

        public override void Update()
        {
            //存在Target时启动
            if (atkTimers.IsSkillReady(Key))
            {
                player.FindEnemy(out Role findRole, angle: 180);
                if (findRole != null && !findRole.IsDie)
                {
                    //触发
                    atkTimers.SetSkillEnterCD(Key);
                    loopTimer.Reset();
                    _triggerIndex = 0;
                }
            }
            if (_triggerIndex < 3)
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
            go.SetActive(true);

            var bullet = go.GetComponent<Bullet_MagicMissile>();

            bullet.ackInfo = GetAtkInfo();
            bullet.InitWithPlayer(player);

            //位置 & 朝向
            _triggerIndex++;
        }

        private AtkInfo GetAtkInfo()
        {
            PlayerAttr attr = player.PlayerAttr;
            bool isCrit = MathTool.IsInRandom(attr.Crit / 100f);
            int baseAtk = attr.Atk;
            var info = new AtkInfo()
            {
                team = player.team,
                skillId = Buff.ToString(),
                baseAtk = baseAtk,
                atk = baseAtk,
                isCrit = isCrit,
                atker = player.id,
            }; 
            return info;
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
