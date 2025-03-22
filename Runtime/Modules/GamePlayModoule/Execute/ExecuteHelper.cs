using UnityEngine;

public class ExecuteHelper
{
    public static void DoExecute(Transform transform)
    {
        var executes = transform.GetComponents<IExecute>();
        foreach (var execute in executes)
        {
            execute.Execute();
        }
    }


    public static void DoExecuteInChildren(Transform transform)
    {
        var executes = transform.GetComponentsInChildren<IExecute>();
        foreach (var execute in executes)
        {
            execute.Execute();
        }
    }

    public static void DoExecute(GameObject go)
    {
        var executes = go.GetComponents<IExecute>();
        foreach (var execute in executes)
        {
            execute.Execute();
        }
    }
}
