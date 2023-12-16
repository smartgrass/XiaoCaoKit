using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace XiaoCao
{
    public class Player0 : Player
    {
        //True:本地玩家控制;  False:网络同步控制, AI控制
        public bool IsControl;

        public PlayerData0 playerData;

        public PlayerShareData0 playerShareData;

        public override IData data => playerData;
        public override IShareData componentData => playerShareData;

        public void Init(PlayerData0 playerData)
        {
            this.playerData = playerData;
            prefabID = playerData.prefabID;

            this.CreateGameObject();

            playerShareData.rigidbody = gameObject.GetComponent<Rigidbody>();
            playerShareData.anim = gameObject.GetComponentInChildren<Animator>();
            playerShareData.input = new PlayerInput(this);
            playerShareData.cc = gameObject.GetComponent<CharacterController>();
        }

        protected override void OnUpdate()
        {
            playerShareData.input.Update();
            playerShareData.control.Update();
        }

        protected override void OnFixedUpdate()
        {
            playerShareData.control.FixedUpdate();
            playerShareData.input.FixedUpdate();
        }
    }


    public class PlayerInputData
    {
        public float horizontal;
        public float vertical;
        public bool[] inputs;
        public int skillInput;

        public KeyCode[] CheckKeyCode = new KeyCode[] { KeyCode.Alpha0, KeyCode.Alpha1 };

        public void Reset()
        {
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = false;
            skillInput = 0;
            horizontal = 0;
            vertical = 0;
        }

        public void Copy(PlayerInputData data)
        {
            this.horizontal = data.horizontal;
            this.vertical = data.vertical;
            this.inputs = data.inputs;
            skillInput = data.skillInput;
        }

    }

    public class PlayerInput : EntityComponent<Player0>
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => owner.playerData.inputData;
        internal void Update()
        {
            data.horizontal = Input.GetAxis("Horizontal");
            data.vertical = Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.Space))
                data.inputs[0] = true;
            if (Input.GetKey(KeyCode.LeftShift))
                data.inputs[1] = true;
            if (Input.GetKey(KeyCode.Mouse0))
                data.inputs[2] = true;

            for (int i = 0; i < data.CheckKeyCode.Length; i++)
            {
                if (Input.GetKey(data.CheckKeyCode[i]))
                {
                    data.skillInput = i; //TODO还需配置对应id,使用SO做默认配置吧
                }
            }
            owner.playerData.inputData.Copy(data);
        }
        internal void FixedUpdate()
        {
            data.Reset();
        }

    }

    public class PlayerControl : EntityComponent<Player0>
    {
        public PlayerControl(Player0 owner) : base(owner) { }

        public PlayerInputData inputData => owner.playerData.inputData;

        public PlayerData0 playerData => owner.playerData;
        public CharacterController cc => owner.playerShareData.cc;

        //TODO 可优化
        public Vector3 camForward => Camera.main.transform.forward;

        public void Update()
        {
            if (playerData.actState == EActState.Dead)
            {

                return;
            }

            if (playerData.isCanSkill && inputData.skillInput != 0)
            {
                TryPlaySkill(inputData.skillInput);
            }

        }
        public void FixedUpdate()
        {
            UpdateGravity();

            if (playerData.actState == EActState.Dead)
            {
                return;
            }

            MoveUpdate();
        }

        void MoveUpdate()
        {
            if (!playerData.isMoveLock && playerData.moveLockTime <= 0)
            {
                //还有其他buff条件 
                Vector3 forward = camForward;
                forward.y = 0;
                Vector3 hor = Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 vec = (inputData.vertical * forward.normalized + inputData.horizontal * hor).normalized;
                cc.Move(vec);
            }
            if (playerData.moveLockTime > 0)
            {
                playerData.moveLockTime -= XCTime.fixedDeltaTime;
            }
        }

        void UpdateGravity()
        {
            //暂时利用刚体吧
            //owner.playerShareData.rigidbody.useGravity
        }



        public void TryPlaySkill(int skillId)
        {
            //条件判断, 耗蓝等等



            PlaySkill(skillId);
        }

        public void PlaySkill(int skillId)
        {
            Debug.Log($"yns PlaySkill {skillId}");

        }


    }

    public class EntityComponent<T> where T : Role
    {
        public T owner;

        public EntityComponent(T owner)
        {
            this.owner = owner;
        }
    }


    public class PlayerData0 : IData
    {
        public int prefabID = 0;

        public EActState actState;
        public int curSkillId;
        public bool isCanSkill;
        public bool isNorAck;
        public bool isMoveLock;
        public float moveLockTime;

        public MoveSetting moveSetting;
        public float MoveSpeed => moveSetting.moveSpeed;
        public float moveSpeedFactor = 1;

        public PlayerAttr playerAttr = new PlayerAttr();

        public PlayerInputData inputData = new PlayerInputData(); //方向,ack 1,2 ,skill,空格
    }

    public class MoveSetting : ScriptableObject
    {
        public float moveSpeed = 4;
        public float angleSpeed = 40;

    }

    public class PlayerAttr
    {
        public int lv;
        public int exp;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int maxExp;
        public int atk;
        public int def;



        public void SetByLevel(int lv)
        {
            maxHp = hp = 100 + 10 * lv;
            maxMp = mp = 100 + 10 * lv;
            maxExp = 100 + 100 * (lv % 10);
            atk = lv * 5;
            def = lv * 1;
        }
    }

    public class PlayerShareData0 : IShareData
    {
        public Rigidbody rigidbody;
        public PlayerInput input;
        public PlayerControl control;
        internal Animator anim;
        internal CharacterController cc;
    }


    public enum EActState
    {
        Idle,
        //Jump,
        //Roll,
        Break, //被打断的中,眩晕中
        Dead  //最高优先级
    }

}
