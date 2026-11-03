using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 功能件: 场景里可以被角色占用的固定设施(目前只有炮台)。
    /// 角色通过 ShowAct 的 FindFunction 自动查找周围的功能件并开始操控。
    /// 想增加新类型(弩车/投石机之类)只要实现本接口、在 OnEnable 注册到 <see cref="FunctionItemMgr"/>,
    /// 查找与操控流程都不用改。
    /// </summary>
    public interface IFunctionItem
    {
        /// <summary>功能件自身节点, 按距离查找用; 节点销毁后用 Unity 判空即可认为失效</summary>
        Transform ItemTransform { get; }

        /// <summary>当前占用者, 为空表示空闲</summary>
        Role Operator { get; }

        /// <summary>角色要走到哪个位置才能操作这里(炮台是站位点); 没有专门站位点就返回 ItemTransform.position</summary>
        Vector3 OperatePosition { get; }

        /// <summary>离 <see cref="OperatePosition"/> 多近(米)才算"已靠近", 可以开始操作</summary>
        float OperateRange { get; }

        /// <summary>该角色现在能否占用: 空闲或已被自己占用, 且没有拒绝过这个角色</summary>
        bool CanBeOperatedBy(Role role);

        /// <summary>开始操控; 占不到时返回 false</summary>
        bool TryOperate(Role role);

        /// <summary>断开操控; 只有当前占用者是自己时才生效</summary>
        void StopOperate(Role role);
    }
}
