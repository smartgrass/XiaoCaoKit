using UnityEngine;
using Random = UnityEngine.Random;

namespace XiaoCao.Buff
{

    public class Bullet_MagicMissile : Atker
    {
        public float handleX = 0.2f;
        public float handleR = 4;
        public float handleY = 0.5f;
        public float offsetY = 0.8f;

        public float startY = 0.8f;
        public float startX = 0.5f;
        public float startR = 0.2f;
        public float scanR = 7;

        public float startDelay = 0f;
        public float maxMoveTime = 4;
        public float moveSpeed = 4;
        public int effectIndex = 0;

        private Transform target;
        private EBulletState state;
        private float targetTime;
        private Vector3 _tempEndPos;
        private Vector3 _startPos;
        private Vector3 _handle;
        public Vector3 GetEndPos
        {
            get
            {
                if (target)
                {
                    _tempEndPos = target.position + Vector3.up * offsetY;
                }
                return _tempEndPos;
            }
        }

        private float timer;

        public void InitWithPlayer(Player0 player)
        {
            InitAtkInfo(AtkInfoHelper.CreatInfo(player, EBuff.MagicMissile.ToString()));
            TriggerByCollider();
            //random
            Vector3 offset = GetRandomStartPoint(startR);
            Init(player.transform.TransformPoint(offset));
            maxMoveTime = 1;
        }

        public void Init(Vector3 startPos)
        {
            target = null;
            _startPos = startPos;
            transform.position = _startPos;
            EnterState(EBulletState.Start);
        }


        private void Update()
        {
            timer += XCTime.deltaTime;
            switch (state)
            {
                case EBulletState.Start:
                    //等待0.5f;
                    if (timer > startDelay)
                    {
                        EnterState(EBulletState.Running);
                    }
                    break;
                case EBulletState.Running:
                    float t = Mathf.Clamp01(timer / targetTime);
                    Vector3 pos = MathTool.GetBezierPoint2(_startPos, GetEndPos, _handle, t);
                    MoveAndLookTo(pos);
                    if (timer >= maxMoveTime)
                    {
                        EnterState(EBulletState.Finish);
                    }
                    break;
                default:
                    break;
            }

        }

        void MoveAndLookTo(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            if (!dir.IsZore())
            {
                transform.forward = dir;
            }
            transform.position = pos;
        }


        public void EnterState(EBulletState newState)
        {
            state = newState;
            timer = 0;
            if (newState == EBulletState.Running)
            {
                FindEnemy();
                SetHandleValue();
            }
            else if (newState == EBulletState.Finish)
            {
                FinishEnd();
            }
        }


        void SetHandleValue()
        {
            var endPos = GetEndPos;
            float distance = Vector3.Distance(_startPos, endPos);
            Vector3 dir = (endPos - _startPos).normalized;
            Vector3 center = Vector3.Lerp(_startPos, endPos, handleX);
            _handle = center + GetRandomPoint(handleR) + Vector3.up * handleY;


            targetTime = distance / moveSpeed;
            targetTime = Mathf.Clamp(targetTime, 1, maxMoveTime);

        }

        Vector3 GetRandomPoint(float r)
        {
            return new Vector3(Random.Range(-r, r), Random.Range(0, r), Random.Range(-r, r));
        }

        Vector3 GetRandomStartPoint(float r)
        {
            //正负
            float sign = RandomHelper.GetRandom() ? 1 : -1;
            var randomPoint = Random.insideUnitSphere * r;
            return randomPoint + Vector3.up * startY + Vector3.left * startX * sign;
        }


        public void FinishEnd()
        {
            var effect = RunTimePoolMgr.Inst.GetHitEffect(effectIndex);
            effect.transform.position = transform.position;
            effect.transform.forward = transform.forward;
            OnRecycle();
            EnterState(EBulletState.Stop);
        }

        public override void OnTriggerTimeOut()
        {
            base.OnTriggerTimeOut();
            FinishEnd();
        }

        public void FindEnemy()
        {
            var player = id.GetPlayerById();
            if (player.FindEnemy(out Role findRole, scanR, angle: 180))
            {
                target = findRole.transform;
            }
            else
            {
                _tempEndPos = player.transform.position + Vector3.up * offsetY
                    + player.transform.forward.ToY0() * scanR;
            }
        }

        void OnRecycle()
        {
            var pool = PoolMgr.Inst.GetOrCreatPool(BuffEffect_MagicMissile.BulletPath);
            if (pool != null)
            {
                pool.Release(gameObject);
            }
            gameObject.SetActive(false);
        }

    }

}
