using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 召唤一个支援角色, 并派它去操作附近的功能件(炮台)。
/// <para>1. 在自身位置/朝向上召唤支援角色; 场上已经有一只时直接复用它, 不会重复生成</para>
/// <para>2. 关掉它"离玩家太远就自动回到/传送回玩家身边"的功能(见 <see cref="AIControl.FriendKeepWithPlayer"/>)</para>
/// <para>3. 给它加上 <see cref="RoleTagCommon.KeepOperateFunction"/>: 只要 Tag 还在, 被踢下炮台(被打出 break / 被别人接手)
/// 就隔 1 秒重新回去操作; Tag 移除后也就不再回来了</para>
/// <para>4. 执行 <see cref="ShowActKeys.FindFunction"/>, 让它自己走过去接管最近的功能件</para>
/// </summary>
public class SummonSupportRoleFunctionExec : GameStartMonoExec
{
    [Tooltip("关闭支援角色「离玩家太远自动回到玩家身边」(含 20 米外的瞬移); 不关会被友军跟随逻辑从功能件旁边拽走")]
    public bool disableFriendKeepWithPlayer = true;

    [Tooltip("给它加「一直守着功能件」的 Tag: 被踢下炮台后 1 秒自动回去操作; 关掉则被打断一次就不再回来")]
    public bool keepOperateFunction = true;

    [Tooltip("查找功能件的半径(米); 0 = 用 ShowAct 的默认值 20 米")]
    [MinValue(0f)]
    public float findFunctionRadius = 20f;

    public override void Execute()
    {
        Enemy0 role = SummonSupportRole();
        if (role == null)
        {
            Debug.LogWarning($"--- {name} 召唤支援角色失败: 场上没有可用的玩家");
            return;
        }

        if (disableFriendKeepWithPlayer)
        {
            //不关这个的话, 支援角色走出 6 米就开始往玩家身上贴, 超过 20 米直接被瞬移回去
            role.AiControl.FriendKeepWithPlayer = false;
        }

        if (keepOperateFunction)
        {
            //FindFunction 里靠这个 Tag 判断"被踢下炮台后要不要再回去", 要加在派活之前
            role.AddTag(RoleTagCommon.KeepOperateFunction);
        }

        RunFindFunction(role);
    }

    /// <summary>在编辑器里手动跑一次(仅 Play 模式), 方便调功能件位置和半径</summary>
    [Button("召唤并派去操作功能件", enabledMode: EButtonEnableMode.Playmode)]
    private void ExecuteByButton()
    {
        Execute();
    }

    /// <summary>让场上这只支援角色不再守着炮台: 当前这次操控结束后就不回去了</summary>
    [Button("解除守炮台", enabledMode: EButtonEnableMode.Playmode)]
    private void ReleaseKeepOperate()
    {
        Enemy0 role = BattleData.Current.supportRoleId.GetRoleById() as Enemy0;
        if (role == null)
        {
            return;
        }

        role.RemoveTag(RoleTagCommon.KeepOperateFunction);
    }

    /// <summary>召唤支援角色; 已经召唤过时复用场上那一只</summary>
    private Enemy0 SummonSupportRole()
    {
        //已召唤过会返回 false 且弹提示, 但 supportRoleId 仍指向场上那只, 后面照常派它去干活
        BattleExtraItemHelper.TrySummonSupportRole(transform.position, transform.forward);
        return BattleData.Current.supportRoleId.GetRoleById() as Enemy0;
    }

    /// <summary>执行 ShowActKeys.FindFunction: 让支援角色自己走过去接管最近的功能件</summary>
    private void RunFindFunction(Enemy0 role)
    {
        IdRole idRole = role.idRole;
        if (idRole == null)
        {
            Debug.LogWarning($"--- {name} 支援角色 {role.id} 还没有角色本体, 无法执行 FindFunction");
            return;
        }

        //支援角色是运行时生成的, 身上没有演出组件; FindFunction 由它驱动
        EnemyShowAction showAction = idRole.GetComponent<EnemyShowAction>();
        if (showAction == null)
        {
            showAction = idRole.gameObject.AddComponent<EnemyShowAction>();
        }

        //ShowAct 文本格式: "按键:内容", 内容就是搜索半径
        string lines = findFunctionRadius > 0f
            ? $"{ShowActKeys.FindFunction}:{findFunctionRadius.ToString("0.##")}"
            : ShowActKeys.FindFunction;

        showAction.RunShowAction(role, lines);
    }
}
