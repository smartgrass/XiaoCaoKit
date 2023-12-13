using System;
using UnityEngine;

namespace XiaoCao
{
    public class Player0 : Player
    {
        public PlayerData0 playerData;

        public PlayerShareData0 playerShareData;

        public override IData data => playerData;

        public override IShareData componentData => playerShareData;

        public void Init(PlayerData0 playerData)
        {
            prefabID = playerData.prefabID;
            this.CreateGameObject();

            playerShareData.rigidbody = gameObject.GetComponent<Rigidbody>();
            playerShareData.input = new PlayerInput(this);
        }

        protected override void OnUpdate()
        {
            playerShareData.input.Update();
        }

        protected override void OnFixedUpdate()
        {
            playerShareData.input.FixedUpdate();
        }

    }


    public class PlayerInputData
    {
        public float horizontal;
        public float vertical;
        public bool[] inputs;
        public int skillInput;

        public KeyCode[] CheckKeyCode = new KeyCode[] { KeyCode.Alpha0,KeyCode.Alpha1};

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

    public class PlayerInput : PlayerComponent<Player0>
    {
        public PlayerInput(Player0 owner) : base(owner){ }

        public PlayerInputData data = new PlayerInputData();

        internal void FixedUpdate()
        {

            data.Reset();
        }

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
                    data.skillInput = i;
                }
            }
            owner.playerData.inputData.Copy(data);
        }
    }

    public class PlayerComponent<T> where T : Role
    {
        public T owner;

        public PlayerComponent(T owner)
        {
            this.owner = owner;
        }
    }


    public class PlayerData0 : IData
    {
        public int prefabID = 0;
        public int actState;
        public PlayerInputData inputData; //方向,ack 1,2 ,skill,空格
    }

    public class PlayerShareData0 : IShareData
    {
        public Rigidbody rigidbody;
        public PlayerInput input;
    }

}
