using System;
using System.Collections.Generic;
using RotaryHeart.Lib.PhysicsExtension;
using UnityEngine;
using Physics = RotaryHeart.Lib.PhysicsExtension.Physics;

namespace XiaoCao.Render
{
    //防角色重合碰撞
    public class BodyCrash : MonoBehaviour
    {
        public Vector3 boxSize = Vector3.one;

        public Vector3 boxOffset;
        public float pushForce = 5.0f;
        
        private Collider[] _results = new Collider[10];
        private Transform selfTf;

        private bool selfInit;

        // 添加缓存字典以避免重复的GetComponent调用
        private Dictionary<GameObject, CharacterController> _characterCache =
            new Dictionary<GameObject, CharacterController>();


        private void FixedUpdate()
        {
            //每帧检测, 若检测到角色重合, 则将角色位置往远程推
            CheckAndResolveOverlap();
        }

        private void CheckAndResolveOverlap()
        {
            // 使用盒状区域检测周围的碰撞体，基于自身Transform的旋转和缩放
            // 应用偏移量到检测中心位置
            Vector3 offsetPosition = transform.position + transform.TransformDirection(boxOffset);
            // 移除预览参数以提高性能
            int count = Physics.OverlapBoxNonAlloc(offsetPosition,
                boxSize,
                _results,
                transform.rotation, Layers.BODY_PHYSICS_MASK, PreviewCondition.Editor);

            for (int i = 0; i < count; i++)
            {
                Collider other = _results[i];

                // 避免检测到自己
                if (other.gameObject == gameObject)
                    continue;

                if (selfInit)
                {
                    if (other.transform == selfTf)
                    {
                        continue;
                    }
                }
                else
                {
                    //判断是否是自身父节点,是则跳过
                    if (transform.IsChildOf(other.transform))
                    {
                        selfTf = other.transform;
                        selfInit = true;
                        continue;
                    }
                }

                // 检查是否是角色对象（通过标签或组件）
                if (other.CompareTag(Tags.ENEMY) || other.CompareTag(Tags.PLAYER))
                {
                    // 计算推开方向（从当前对象指向其他对象）
                    Vector3 direction = other.transform.position - transform.position;

                    // 如果两个对象位置完全重合，则选择一个默认方向
                    if (direction.sqrMagnitude < 0.01f)
                    {
                        direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f))
                            .normalized;
                    }
                    else
                    {
                        direction.y = 0; // 只在水平面上推开
                        direction.Normalize();
                    }

                    // 将其他对象推开（使用缓存避免重复GetComponent）
                    CharacterController cc;
                    if (!_characterCache.TryGetValue(other.gameObject, out cc))
                    {
                        cc = other.GetComponent<CharacterController>();
                        _characterCache.Add(other.gameObject, cc);
                    }

                    if (cc)
                    {
                        cc.Move(direction * (pushForce * Time.fixedDeltaTime));
                    }
                }
            }
        }

        // 当对象被销毁时清理缓存
        private void OnDestroy()
        {
            _characterCache.Clear();
        }
    }
}