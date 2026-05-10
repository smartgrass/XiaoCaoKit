
public abstract class GameStartMonoExec : GameStartMono, IExecute
{
    public bool isExecuteOnStart = true;

    //MonoExecute
    public override void OnGameStart()
    {
        base.OnGameStart();
        if (isExecuteOnStart)
        {
            Execute();
        }
    }

    public virtual void Execute()
    {
    }
}
