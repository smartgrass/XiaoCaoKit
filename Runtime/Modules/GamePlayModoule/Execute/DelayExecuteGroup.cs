public class DelayExecuteGroup : DelayMonoExecute
{
    public MonoExecute[] list;
    public override void Execute()
    {
        foreach (var item in list)
        {
            item.Execute();
        }
    }
}