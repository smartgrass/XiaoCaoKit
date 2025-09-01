using UnityEngine;

public class MarkObject : MonoBehaviour
{
    public string key;
    
    private void Awake()
    {
        MarkObjectMgr.Add(key, gameObject);
    }

    private void OnDestroy()
    {
        MarkObjectMgr.Remove(key);
    }
}