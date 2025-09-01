using System;

public class EnableExec : MonoExecute
{
    public MonoExecute enableExecute;

    public override void Execute()
    {
        enableExecute?.Execute();
    }

    private void OnEnable()
    {
        Execute();
    }
}