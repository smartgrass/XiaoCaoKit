using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_EnemyCmd : MonoBehaviour
{
    [OnValueChanged(nameof(SetAI))]
    public bool isAI;
    [MiniBtn(nameof(SetAct))]
    public string actCmd = "0";


    private Enemy0 enemy;

    private void Start()
    {
        enemy = GetComponent<IdRole>().GetEntity() as Enemy0;
        isAI = enemy.IsAiOn;
    }



    void SetAI()
    {
        enemy.IsAiOn = isAI;
    }


    void SetAct()
    {
        enemy.AIMsg(ActMsgType.Skill, actCmd);
    }

}