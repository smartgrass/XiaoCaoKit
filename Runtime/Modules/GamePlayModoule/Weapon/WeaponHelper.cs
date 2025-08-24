using UnityEngine;

namespace XiaoCao
{
    public static class WeaponHelper
    {
        public static Transform FindWeapon(Animator animator)
        {
            var hand = WeaponHelper.FindHand(animator);
            var weapon = hand.Find(Role.WeaponPointName);
            return weapon;
        }

        public static Transform FindHand(Animator animator)
        {
            Transform rightHandBone = null;
            if (animator != null && animator.isHuman)
            {
                // 获取右手骨骼的Transform  
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (rightHandBone == null)
            {
                Debug.LogError("Animator组件不存在或不是Humanoid类型");
                return FindHand(animator.GetComponent<SkinnedMeshRenderer>());
            }
            return rightHandBone;
        }

        private static Transform FindHand(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer != null)
            {
                Transform[] bones = skinnedMeshRenderer.bones;

                foreach (Transform bone in bones)
                {
                    Debug.Log(bone.name);
                    if (bone.name.Contains("RightHand") || bone.name == "WeaponPoint")
                    {
                        return bone;
                    }
                }
            }
            return null;
        }
    }
}
