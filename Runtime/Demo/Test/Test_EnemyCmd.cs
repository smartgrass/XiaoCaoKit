using NaughtyAttributes;
using System.Text;
using UnityEngine;
using XiaoCao;

public class Test_EnemyCmd : MonoBehaviour
{
    [OnValueChanged(nameof(SetAI))]
    public bool isAI;
    [MiniBtn(nameof(SetAct))]
    public string actCmd = "0";

    [Header("DebugView")]
    public AIControl control;

    private Enemy0 enemy;

    private void Start()
    {
        enemy = GetComponent<IdRole>().GetEntity() as Enemy0;
        isAI = enemy.IsAiOn;
        control = enemy.component.aiControl;
    }



    void SetAI()
    {
        enemy.IsAiOn = isAI;
    }


    void SetAct()
    {
        enemy.AIMsg(ActMsgType.Skill, actCmd);
    }

    private void Update()
    {
        if (DebugGUI.IsShowing)
        {
            ActPoolFSM poolFSM = control.mainDataFSM as ActPoolFSM;

            if (poolFSM.CurState != null)
            {
                DebugGUI.Log("AIState", GetStatePath(poolFSM.CurState));
            }
        }
    }

    private string GetStatePath(AIFSMBase state)
    {
        return state.GetStatePath();
    }

}