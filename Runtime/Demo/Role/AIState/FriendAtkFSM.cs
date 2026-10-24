using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 友方攻击行为调度器: 在技能与普攻之间做选择。
    /// 技能可用(CD好且未达连击上限)时使用 skillFSM, 否则切换到普攻行为 norAtkFSM(顺序FSM, 支持多段普攻)。
    /// 累计攻击 maxAtkTime 次后结束, 由上层行为池决定下一步(如重新接近目标)。
    /// </summary>
    [CreateAssetMenu(fileName = "FriendAtkFSM", menuName = "SO/AI/FriendAtkFSM", order = 1)]
    public class FriendAtkFSM : MainFSM
    {
        [XCLabel("普攻行为(顺序FSM)")]
        [Tooltip("普攻子行为, 可用 SequenceFSM 串联接近和多段普攻。")]
        public SequenceFSM norAtkFSM;

        [XCLabel("技能行为")]
        [Tooltip("技能子行为, 通常为 FriendAtkState。")]
        public FriendAtkState skillFSM;

        [XCLabel("本次最大攻击次数")]
        [Tooltip("技能+普攻累计执行次数达到该值后本状态结束; 小于等于 0 表示不限制。")]
        public int maxAtkTime = 3;

        private FriendAtkState _skillInst;
        private SequenceFSM _norInst;

        /// <summary>当前执行的子行为</summary>
        private AIFSMBase _cur;

        /// <summary>已执行的攻击次数</summary>
        private int _atkCount;

        public override void OnStart()
        {
            FristLoad();
            _cur = null;
            _atkCount = 0;
            State = FSMState.Update;
        }

        private void FristLoad()
        {
            if (IsLoaded)
            {
                return;
            }
            IsLoaded = true;

            //实例化子行为, 防止多个角色共用同一 SO 资产时互相污染状态
            _skillInst = InstantiateSub(skillFSM);
            _norInst = InstantiateSub(norAtkFSM);
        }

        private T InstantiateSub<T>(T proto) where T : AIFSMBase
        {
            if (proto == null)
            {
                return null;
            }
            var so = ScriptableObject.Instantiate(proto);
            so.name = proto.name;
            so.InitReset(control);
            return so;
        }

        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            if (_cur == null)
            {
                //达到攻击次数上限则结束
                if (maxAtkTime > 0 && _atkCount >= maxAtkTime)
                {
                    OnExit();
                    return;
                }

                _cur = PickNext();
                if (_cur == null)
                {
                    OnExit();
                    return;
                }

                //切换到普攻时重置技能连击计数
                if (_cur == _norInst)
                {
                    _skillInst.ResetCombo();
                }
                _cur.ResetFSM();
            }

            if (_cur.State != FSMState.Finish)
            {
                _cur.OnUpdate();
            }

            if (_cur.State == FSMState.Finish)
            {
                _atkCount++;
                _cur = null;
            }
        }

        /// <summary>
        /// 选择下一个子行为: 技能可用则优先技能, 否则普攻。
        /// </summary>
        private AIFSMBase PickNext()
        {
            if (_skillInst != null && _skillInst.CanUseSkill())
            {
                return _skillInst;
            }
            if (_norInst != null)
            {
                return _norInst;
            }
            return null;
        }

        public override void OnExit()
        {
            State = FSMState.Finish;
        }

        public override string GetStatePath()
        {
            return _cur == null ? name : $"{name}/{_cur.GetStatePath()}";
        }
    }
}
