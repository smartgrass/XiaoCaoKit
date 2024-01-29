using UnityEngine;

namespace XiaoCao
{
    public class Enemy0 : EnemyBase, IMsgReceiver
    {
        public bool isAiRuning;

        internal EnemyData0 enemyData = new EnemyData0();

        public EnemyShareData0 component = new EnemyShareData0();

        public EnemyControl enemyControl;


        protected override void Awake()
        {

        }

        public void Init()
        {
            component.control = new EnemyControl(this);

            RoleIn();
        }

        /// <summary>
        /// TODO 
        /// 行为: 移动,攻击,受击
        /// AI功能: 扫描目标
        /// AI配置哪里填? 拖拽&编辑器自动查找
        /// </summary>

        protected override void OnUpdate()
        {
            component.control.Update();

        }

        protected override void OnFixedUpdate()
        {
            component.control.FixedUpdate();



        }

        protected override void OnDestroy()
        {
            RoleOut();
        }


        public void EnableAI(bool isOn)
        {

        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            base.ReceiveMsg(type, fromId, msg);

            if (type == EntityMsgType.StartSkill)
            {
                int msgInt = (int)msg;
                //playerComponent.control.TryPlaySkill(msgInt);
            }
            else if (type == EntityMsgType.MoveToPos)
            {
                MoveMsg moveMsg = (MoveMsg)msg;
                AIMoveTo(moveMsg.pos, moveMsg.speedFactor);

            }

        }



        void AIMoveTo(Vector3 pos, float speedFactor = 1)
        {
            //
            var dir = (pos - gameObject.transform.position).normalized;

            //TODO moveSetting

            //Vector3 moveDelta = moveDir * Data.moveSetting.baseMoveSpeed * RoleData.roleState.MoveMultFinal * XCTime.fixedDeltaTime;

            //TODO 重力
            //v = v + gt
            float v = 0;
            //v = v + Data.moveSetting.g * XCTime.fixedDeltaTime;
            //moveDelta.y += v * XCTime.fixedDeltaTime;
        }

    }


    public class EnemyData0 : IData
    {

    }


    public class EnemyControl : RoleControl<Enemy0>
    {
        public EnemyControl(Enemy0 _owner) : base(_owner) { }



        public override void Update()
        {
            if (owner.isAiRuning)
            {

            }
        }

    }
    public class EnemyShareData0 : IShareData
    {
        public EnemyControl control;
    }

    public class EnemyComponent : RoleComponent
    {
        public EnemyComponent(Enemy0 owner)
        {
            this.owner = owner;
        }

        public Enemy0 owner;
        public override RoleData Data_R => owner.roleData;
        public EnemyData0 Data_E => owner.enemyData;
    }


}
