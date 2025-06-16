using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XiaoCao;

public class Test_EnemyCmd : MonoBehaviour
{
#if UNITY_EDITOR
    [OnValueChanged(nameof(SetAI))]
    public bool isAI;
    [MiniBtn(nameof(SetAct))]
    public string actIndexCmd = "0";

    [MiniBtn(nameof(SetActCombol))]
    public string actCombolCmd = "0,1";

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
        enemy.AIMsg(ActMsgType.Skill, actIndexCmd);
    }

    void SetActCombol()
    {
        string[] array = actCombolCmd.Split(",");
        StartCoroutine(IEActCombol(array));
    }

    IEnumerator IEActCombol(string[] cmdList)
    {
        foreach (string cmd in cmdList)
        {
            enemy.AIMsg(ActMsgType.Skill, cmd);
            yield return null;
            yield return new WaitUntil(NoBusy);
        }
    }

    private bool NoBusy()
    {
        return !enemy.roleData.IsBusy;
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
#endif
}