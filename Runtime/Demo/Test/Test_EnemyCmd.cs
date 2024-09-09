using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_EnemyCmd : MonoBehaviour
{
    public bool isAI;

    private Enemy0 enemy;

    private void Start()
    {
        enemy = GetComponent<IdRole>().GetEntity() as Enemy0;
    }

    [Button]

    void SetAI()
    {
        enemy.IsAiOn = isAI;
    }
}