using System;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{
    public class XCAnimEvent : XCEvent
    {
        #region private
        private Animator _animator = null;

        private AnimationClip Clip;

        #endregion
        public int clipHash => Animator.StringToHash(eName);

        public float blenderLength = 0;

        public float startOffset = 0;

        public float speed = 1f;

        //!hasExitTime 不等当前clip自动播完
        public bool isBackToIdle;

        void FindClip()
        {
            foreach (var item in _animator.runtimeAnimatorController.animationClips)
            {
                if (item.name == eName)
                {
                    Clip = item;
                    return;
                }
            }
        }

        public override void OnTrigger(float timeSinceTrigger)
        {
            if (_animator == null)
            {
                //this.track.data
                //if (skillOwner.isCustomObject)
                //{
                //    _animator = skillOwner.eventOwnerTF.GetComponentInChildren<Animator>(true);
                //}
                //else
                //{
                //    //默认获取玩家动画机
                //    _animator = skillOwner.attacker.animator;
                //}
            }
            if (_animator == null)
            {
                throw new Exception($"no _animator {eName}");
            }
            FindClip();

            if (Clip == null)
            {
                throw new Exception($"no clip {eName}");
            }

            _animator.speed = track.Info.speed;

            base.OnTrigger(timeSinceTrigger);

            //如果主技能被打断则不播放动画
            //if (SelfRunner.isMainSkill && SelfRunner.isBreak)
            //{
            //    return;
            //}

            _animator.CrossFade(clipHash, blenderLength / Clip.length, 0, startOffset / Clip.length);

            //修正偏差值
            if (timeSinceTrigger > 0)
            {
                _animator.Update(timeSinceTrigger - 0.001f);
            }
        }

        public override void OnFinish()
        {
            if (isBackToIdle)
            {
                _animator.CrossFade(AnimHash.Idle, 0.05f);
            }
            base.OnFinish();
        }
    }


    public static class AnimHash
    {
        public static readonly int Idle = AnimNames.Idle.AnimHash();
        public static readonly int Break = AnimNames.Break.AnimHash();//break
        public static readonly int Hit = AnimNames.Hit.AnimHash();//轻受击
        public static readonly int Dead = AnimNames.Dead.AnimHash();
        public static readonly int RollTree = AnimNames.RollTree.AnimHash();
    }
    public static class AnimNames
    {
        public const string Idle = "Idle";
        public const string Break = "Break";
        public const string Hit = "Hit";
        public const string Dead = "Dead";
        public const string RollTree = "RollTree";

        public static int AnimHash(this string name)
        {
            return Animator.StringToHash(name);
        }
    }

}