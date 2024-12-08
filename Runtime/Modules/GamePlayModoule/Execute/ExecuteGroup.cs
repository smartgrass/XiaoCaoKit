public class ExecuteGroup : MonoExecute
{
    public MonoExecute[] list;
    public override void Execute()
    {
        foreach (var item in list) { 
            item.Execute();
        }
    }
}
