using XiaoCao;

/// <summary>
/// 执行时召唤支援角色到自身位置。
/// </summary>
public class SummonSupportRoleExec : GameStartMonoExec
{
    /// <summary>
    /// 在自身坐标和朝向上召唤支援角色。
    /// </summary>
    public override void Execute()
    {
        BattleExtraItemHelper.TrySummonSupportRole(transform.position, transform.forward);
    }
}
