using System;
using System.Collections;
using UnityEngine;
using XiaoCao;
using static XiaoCao.AIControl;

public class EnemyShowAction : BaseShowAction
{
    public Enemy0 enemy0;

    public override CharacterController Cc => enemy0.idRole.cc;


    public void OnEnemyCreat(EnemyCreator enemyCreator, Enemy0 enemy)
    {
        enemy0 = enemy;
        string[] list = taskLines.Split('\n');
        StartCoroutine(IETaskRun());
    }

    public override void OnTaskStart()
    {
        enemy0.AiControl.IsOnShowAction = true;
    }

    public override void OnTaskEnd()
    {
        Debug.Log($"--- OnTaskEnd");
        enemy0.AiControl.IsOnShowAction = false;
    }


    public override IEnumerator RunActData(ShowActData showActData)
    {
        isOverrideRunAct = false;
        switch (showActData.actName)
        {
            case ShowActKeys.FollowPlayer:
                enemy0.AiControl.FollowPlayer();
                break;
            case ShowActKeys.SetTargetMove:
                isOverrideRunAct = true;
                yield return IEProcessTargetMoveAct(showActData);
                break;
            case ShowActKeys.SetHp:
                isOverrideRunAct = true;
                if (!string.IsNullOrEmpty(showActData.content))
                {
                    int hp = int.Parse(showActData.content);
                    enemy0.PlayerAttr.GetAttribute(EAttr.MaxHp).BaseValue = hp;
                    enemy0.PlayerAttr.hp = hp;
                }

                break;
        }

        if (!isOverrideRunAct)
        {
            yield return base.RunActData(showActData);
        }

        yield break;
    }

    private IEnumerator IEProcessTargetMoveAct(ShowActData showActData)
    {
        var aiControl = enemy0.AiControl;
        string[] parts = showActData.GetContentArray();
        aiControl.TargetPosTypeValue = Enum.Parse<TargetPosType>(parts[0]);
        if (parts[0] == TargetPosType.Point.ToString())
        {
            float speed = float.Parse(parts[2]);
            float stopDistance = float.Parse(parts[3]);
            aiControl.TargetStopDistance = stopDistance;
            aiControl.MoveSpeedShowAction = speed;

            string vecStr = parts[1];
            string[] vecParts = vecStr.Split('/');
            float x = float.Parse(vecParts[0]);
            float y = float.Parse(vecParts[1]);
            float z = float.Parse(vecParts[2]);
            Vector3 vec = new Vector3(x, y, z);

            // 设置目标点
            aiControl.TargetPos = vec;
        }
        else if (parts[0] == TargetPosType.Transform.ToString())
        {
            float speed = float.Parse(parts[2]);
            float stopDistance = float.Parse(parts[3]);
            aiControl.TargetStopDistance = stopDistance;
            aiControl.MoveSpeedShowAction = speed;
            GameObject target = MarkObjectMgr.GetById(parts[1]);
            SetTarget(target, parts[1]);
        }
        else
        {
            yield break;
        }

        while (enemy0.AiControl.TargetPosTypeValue != AIControl.TargetPosType.Stop)
        {
            yield return null;
        }
    }

    private void SetTarget(GameObject target, string id)
    {
        if (target)
        {
            enemy0.AiControl.TargetTf = target.transform;
        }
        else
        {
            Debug.LogError($"--- target {id} null");
        }
    }
}