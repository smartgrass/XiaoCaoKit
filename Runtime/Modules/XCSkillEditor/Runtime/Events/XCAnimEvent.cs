using System;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    public class XCAnimEvent : XCEvent
    {
        #region private
        private Animator _animator = null;

        #endregion
        public int clipHash => Animator.StringToHash(eName);

        public float clipLen = 1;

        public float blenderLength = 0;

        public float startOffset = 0;

        public float speed = 1f;

        //回到Idle的过渡时间(秒), 大于0时生效; <=0 则不切回Idle
        public float backToIdleTime;

        /// <summary>
        /// 每个 Animator 上"最近一次动画切换"的序号。
        /// 用于 OnFinish 时判断自己是否还拥有该 Animator 的动画控制权:
        /// 技能末段动画事件的结束帧比主任务停止晚若干帧, 这期间玩家已经按出普攻/下一个技能,
        /// 若仍执行 backToIdle 的 CrossFadeInFixedTime(Idle), 就会把刚起手的新动作盖回 Idle 姿势
        /// (逻辑照常执行, 表现为"技能结束接普攻时角色停在 idle 姿势不动")。
        /// </summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Animator, AnimToken> s_animTokens
            = new System.Runtime.CompilerServices.ConditionalWeakTable<Animator, AnimToken>();

        private sealed class AnimToken
        {
            public int value;
        }

        //本事件触发时从所属 Animator 上取到的序号, 之后被别的动画事件顶掉则不再回 Idle
        [System.NonSerialized]
        private int _myAnimToken;


        public override void OnTrigger(float timeSinceTrigger)
        {
            if (_animator == null)
            {
                if (task.IsMainTask)
                {
                    _animator = Info.playerAnimator;
                }

                if (_animator == null)
                {
                    _animator = Tran.GetComponentInChildren<Animator>();
                }
            }

            if (_animator == null)
            {
                Debug.LogError($"no _animator {eName}");
                return;
            }

            _animator.speed = task.Info.GetAnimSpeed;

            base.OnTrigger(timeSinceTrigger);

            //标记"当前动画归本事件所有"; 之后任何在同一个 Animator 上播放的动画事件都会覆盖这个序号
            _myAnimToken = NextAnimToken(_animator);

            //如果主技能被打断则不播放动画
            //if (SelfRunner.isMainSkill && SelfRunner.isBreak)
            //{
            //    return;
            //}

            //TODO 
            _animator.CrossFade(clipHash, blenderLength / clipLen, 0, startOffset / clipLen);

            //修正偏差值
            if (timeSinceTrigger > 0)
            {
                _animator.Update(timeSinceTrigger - 0.001f);
            }
        }

        public override void OnFinish()
        {
            //只有在"本事件仍拥有动画控制权"时才回 Idle, 否则会把已经起手的新动作(普攻等)盖掉
            if (backToIdleTime > 0f && _animator != null && IsAnimOwner())
            {
                _animator.CrossFadeInFixedTime(AnimHash.Idle, backToIdleTime);
            }
            base.OnFinish();
        }

        /// <summary>
        /// 本事件是否仍拥有该 Animator 的动画控制权。
        /// 技能已被打断(task 不再 Running)、角色已被别的技能占用、或之后有别的动画事件用过
        /// 同一个 Animator(普攻/下一个技能)时返回 false。
        /// </summary>
        private bool IsAnimOwner()
        {
            //技能已被打断(主任务不再 Running)
            if (task != null && task.State != XCState.Running)
            {
                return false;
            }

            //角色已被别的技能占用(普攻/下一个技能)时不回 Idle, 否则会把刚起手的新动作盖掉
            if (IsBusyByOther())
            {
                return false;
            }

            //之后有别的动画事件在同一个 Animator 上播放过
            AnimToken token;
            if (s_animTokens.TryGetValue(_animator, out token))
            {
                return token.value == _myAnimToken;
            }
            return true;
        }

        /// <summary>
        /// 角色是否正被"别的"技能占用。
        /// 本技能自己仍占着角色时不算, 因为那是正常的收招流程。
        /// </summary>
        private bool IsBusyByOther()
        {
            if (task == null || task.Runner == null)
            {
                return false;
            }

            if (task.Runner.IsBusy)
            {
                return false;
            }

            var role = Info != null ? Info.role : null;
            var ctrl = role != null && role.data_R != null ? role.data_R.roleControl : null;
            return ctrl != null && ctrl.IsBusy();
        }

        private static int NextAnimToken(Animator animator)
        {
            AnimToken token;
            if (!s_animTokens.TryGetValue(animator, out token))
            {
                token = new AnimToken();
                s_animTokens.Add(animator, token);
            }
            token.value++;
            return token.value;
        }
    }


    public static class AnimHash
    {
        public static readonly int Idle = AnimNames.Idle.AnimHash();
        public static readonly int Break = AnimNames.Break.AnimHash();//break
        public static readonly int Hit = AnimNames.Hit.AnimHash();//轻受击
        public static readonly int HitIndex = AnimNames.HitIndex.AnimHash();//轻受击
        public static readonly int IsDead = AnimNames.IsDead.AnimHash();
        public static readonly int RollTree = AnimNames.RollTree.AnimHash();
        public static readonly int IsGround = AnimNames.IsGround.AnimHash();
        public static readonly int MoveSpeed = AnimNames.MoveSpeed.AnimHash();
        public static readonly int MoveDirect = AnimNames.MoveDirect.AnimHash();
    }
    public static class AnimNames
    {
        public const string Idle = "Idle";
        public const string IdleValue = "idleValue";
        public const string Break = "Break";
        public const string Hit = "Hit"; //Trigger
        public const string HitIndex = "HitIndex";
        public const string IsDead = "IsDead";
        public const string RollTree = "RollTree";
        public const string Roll = "Roll";
        public const string Jump = "Jump";

        public const string MoveSpeed = "MoveSpeed";
        public const string MoveDirect = "MoveDirect"; //左右1 -1

        public static string IsGround = "IsGround"; //bool

        public static string NorAck = "NorAck";

        public static int AnimHash(this string name)
        {
            return Animator.StringToHash(name);
        }
    }

}