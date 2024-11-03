using UnityEngine;


public abstract class MonoExecute : MonoBehaviour, IExecute
{
    public abstract void Execute();
}

public interface IExecute
{
    public void Execute()
    {

    }
}
