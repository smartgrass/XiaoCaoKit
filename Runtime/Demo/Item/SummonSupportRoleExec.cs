using XiaoCao;

/// <summary>
/// 执行时召唤支援角色。
/// </summary>
public class SummonSupportRoleExec : GameStartMonoExec
{
    /// <summary>
    /// 触发支援角色的召唤逻辑。
    /// </summary>
    public override void Execute()
    {
        BattleExtraItemHelper.TryUse(BattleExtraItemType.SupportRole);
    }
}
