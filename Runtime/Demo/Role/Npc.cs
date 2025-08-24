using NaughtyAttributes;
using System;
using UnityEngine;

namespace XiaoCao
{
    public class Npc : GameStartMono
    {
        [MiniBtn(nameof(DoLoadSkin))]
        public string bodyName;

        public RuntimeAnimatorController runtimeAnimator;

        public GameObject body;


        public bool hideWeapon;

        public string startAnimName;

        //"===可选项=="
        [MiniBtn(nameof(SetAnimClip))]
        public AnimationClip animClip;

        [ReadOnly]
        public Transform weaponTf;

        [ReadOnly]
        public string markName;

        public Vector3 MoveVec { get; set; }

        public Animator Animator { get; set; }

        public CharacterController Cc { get; set; }

        private void Awake()
        {
            Cc = GetComponent<CharacterController>();
        }


        public override void OnGameStart()
        {
            base.OnGameStart();
            markName = gameObject.name;
            MarkObject.MarkDic[gameObject.name] = gameObject;
            DoLoadSkin();
        }

        void DoLoadSkin()
        {
            ChangeSkin(bodyName);
        }

        // 添加一个公共方法用于更换皮肤
        public void ChangeSkin(string bodyName)
        {
            if (string.IsNullOrEmpty(bodyName))
            {
                return;
            }
            // 创建新的身体
            CreateRoleBody(bodyName);

            if (!string.IsNullOrEmpty(startAnimName))
            {
                Animator.Play(startAnimName);
            }
            else
            {
                if (animClip)
                {
                    SetAnimClip();
                }
            }

            if (hideWeapon)
            {
                SetWeapon(false);
            }
        }

        private void SetWeapon(bool show)
        {
            weaponTf = WeaponHelper.FindWeapon(Animator);
            weaponTf.gameObject.SetActive(show);
        }


        // 重写CreateRoleBody方法，实现根据bodyName加载皮肤
        protected void CreateRoleBody(string bodyName)
        {
            if (string.IsNullOrEmpty(bodyName))
            {
                return;
            }

            // 如果body节点不存在，则创建一个空物体作为body节点
            if (body == null)
            {
                body = new GameObject("Body");
                body.transform.SetParent(transform, false);
                body.transform.localPosition = Vector3.zero;
            }

            // 删除body中的所有子物体（清理旧的身体）
            foreach (Transform child in body.transform)
            {
                Destroy(child.gameObject);
            }

            // 加载新的身体预制体
            GameObject newBody = Role.LoadModelByKey(bodyName);

            // 将新身体的所有子物体移动到body节点下
            int childCount = newBody.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = newBody.transform.GetChild(0);
                child.SetParent(body.transform, false);
            }


            // 设置body的名字
            body.name = bodyName;

            // 获取动画控制器
            Animator = body.GetComponentInChildren<Animator>();
            if (Animator != null)
            {
                Animator.runtimeAnimatorController = runtimeAnimator;
                Animator.avatar = newBody.GetComponent<Animator>().avatar;
            }
            // 销毁空的newBody对象
            Destroy(newBody);
        }



        private void FixedUpdate()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
            {
                return;
            }
            Cc.Move(MoveVec + Vector3.down * XCTime.fixedDeltaTime);
        }

        public void SetAnimClip()
        {
            AnimationUtils.AddAndPlayClip(Animator, animClip);
        }
    }
}
