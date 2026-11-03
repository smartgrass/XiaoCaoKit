using System;
using System.Collections;
using UnityEngine;
using XiaoCao;
using static XiaoCao.AIControl;

public class EnemyShowAction : BaseShowAction
{
    /// <summary>FindFunction 没写半径时的默认搜索范围(米)</summary>
    private const float DefaultFindFunctionRadius = 20f;

    /// <summary>走向功能件的最长时间(秒), 超时放弃; 防止被地形卡住后一直僵着</summary>
    private const float ApproachFunctionTimeout = 20f;

    /// <summary>走向功能件时的移速倍率(相对 AI 基础移速)</summary>
    private const float ApproachFunctionMoveSpeed = 1f;

    /// <summary>走到离操作位多近(米)算站好了; 要贴近站位点本身, 否则接管时会对齐过去看到一段位移</summary>
    private const float ApproachArriveDistance = 0.35f;

    /// <summary>连续多久(秒)没有接近操作位就认为走不过去, 直接按当前位置接管</summary>
    private const float ApproachStallTime = 1.5f;

    /// <summary>被打断过就彻底放弃功能件, 不再执行 FindFunction</summary>
    private bool hasGivenUpFunction;

    public Enemy0 enemy0;

    public override CharacterController Cc => enemy0.idRole.cc;

    public override Role GetRole()
    {
        return enemy0;
    }

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
                isOverrideRunAct = true;
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
                    enemy0.RoleAttr.GetAttribute(EAttr.MaxHp).BaseValue = hp;
                    enemy0.RoleAttr.hp = hp;
                }

                break;
            case ShowActKeys.FindFunction:
                isOverrideRunAct = true;
                yield return IEProcessFindFunction(showActData);
                break;
        }

        if (!isOverrideRunAct)
        {
            yield return base.RunActData(showActData);
        }

        yield break;
    }

    /// <summary>
    /// 查找并操作周围的功能件(目前只有炮台): 内容填搜索半径(米), 留空用 DefaultFindFunctionRadius。
    /// 找到后一直占着, 直到被打断(受击/死亡/被别人接手), 之后这个 ShowAct 才算结束。
    /// 受击断开由功能件自己处理(见 Turret.OnOperatorDamaged), 同时会把该角色拉进拒绝名单,
    /// 所以之后即使再次执行 FindFunction 也不会重新占用同一台炮台。
    /// </summary>
    private IEnumerator IEProcessFindFunction(ShowActData showActData)
    {
        if (hasGivenUpFunction)
        {
            yield break;
        }

        float radius = DefaultFindFunctionRadius;
        if (!string.IsNullOrEmpty(showActData.content))
        {
            float.TryParse(showActData.content, out radius);
        }

        Vector3 selfPos = enemy0.transform.position;
        IFunctionItem item = FunctionItemMgr.FindNearest(selfPos, radius, enemy0);
        if (item == null)
        {
            //周围可能只有被别人占着的功能件: 这种情况直接放弃, 不等待也不去抢
            IFunctionItem occupied = FunctionItemMgr.FindNearestAny(selfPos, radius);
            if (occupied != null && occupied.Operator != null)
            {
                Debug.Log($"[FindFunction] {enemy0.transform.name} 放弃: 附近功能件 {occupied.ItemTransform.name} " +
                          $"已被 {occupied.Operator.transform.name} 占用");
            }
            else
            {
                Debug.Log($"[FindFunction] {enemy0.transform.name} 周围 {radius} 米内没有可用功能件");
            }

            yield break;
        }

        //先走过去靠近: 没到操作范围不接管, 中途被别人占了也直接放弃
        yield return IEApproachFunction(item);

        if (!IsNearFunction(item))
        {
            Debug.Log($"[FindFunction] {enemy0.transform.name} 放弃: 没能走到功能件 {item.ItemTransform.name} 旁边");
            yield break;
        }

        if (!item.CanBeOperatedBy(enemy0))
        {
            Role blocker = item.Operator;
            string blockerName = blocker != null ? blocker.transform.name : "未知角色";
            Debug.Log($"[FindFunction] {enemy0.transform.name} 放弃: 走到一半功能件 {item.ItemTransform.name} " +
                      $"被 {blockerName} 占了");
            yield break;
        }

        if (!item.TryOperate(enemy0))
        {
            //TryOperate 内部可能因为"刚好被别人占走"而失败, 同样放弃
            Role holder = item.Operator;
            string holderName = holder != null ? holder.transform.name : "未知角色";
            Debug.Log($"[FindFunction] {enemy0.transform.name} 放弃: 功能件 {item.ItemTransform.name} " +
                      $"已被 {holderName} 占用");
            yield break;
        }

        //功能件被拿走后 Operator 会变空, 敌人死亡时也要退出
        while (item.Operator == enemy0 && !enemy0.IsDie)
        {
            yield return null;
        }

        //死亡/被打断都把功能件交还, 否则炮台会被一个已经离开的敌人一直挂着
        item.StopOperate(enemy0);

        //非死亡退出就是被打断(受击/被抢), 之后这个敌人不再去找功能件
        if (!enemy0.IsDie)
        {
            hasGivenUpFunction = true;
        }
    }

    /// <summary>离功能件的操作位够近, 可以接管了(接管时会对齐到站位点)</summary>
    private bool IsNearFunction(IFunctionItem item)
    {
        if (item == null || item.ItemTransform == null)
        {
            return false;
        }

        return Vector3.Distance(enemy0.transform.position, item.OperatePosition) <= item.OperateRange;
    }

    /// <summary>已经站到功能件的操作位上, 接管时不需要再对齐</summary>
    private bool IsArrivedAtOperatePos(IFunctionItem item)
    {
        if (item == null || item.ItemTransform == null)
        {
            return false;
        }

        return Vector3.Distance(enemy0.transform.position, item.OperatePosition) <= ApproachArriveDistance;
    }

    /// <summary>
    /// 让 AI 一路走到功能件的操作位(站位点)上, 而不是只走到"可操作范围"边缘:
    /// 走到边缘就接管的话, 敌人会停在离站位点一两米的地方, 和炮台看起来没关系。
    /// 走不过去(被炮台/地形挡住)时按当前站位接管, 对齐交给功能件自己处理; 20 秒还走不到则放弃。
    /// 期间每帧确认功能件还空着, 被别人占了就提前结束, 交给调用方放弃。
    /// </summary>
    private IEnumerator IEApproachFunction(IFunctionItem item)
    {
        if (item == null || item.ItemTransform == null || enemy0.IsDie)
        {
            yield break;
        }

        AIControl ai = enemy0.AiControl;

        if (IsArrivedAtOperatePos(item))
        {
            ai.MoveStop();
            ai.TargetPosTypeValue = TargetPosType.Stop;
            yield break;
        }

        //沿用 ShowAct 那套移动: IsOnShowAction 期间由 ForceFollowTargetUpdate 驱动
        ai.TargetPosTypeValue = TargetPosType.Point;
        ai.TargetPos = item.OperatePosition;
        //直接停在站位点上, 不用"可操作范围"当停止距离, 否则接管时还要瞬移一段
        ai.TargetStopDistance = ApproachArriveDistance;
        ai.MoveSpeedShowAction = ApproachFunctionMoveSpeed;

        float timer = 0f;
        float lastRemain = Vector3.Distance(enemy0.transform.position, item.OperatePosition);
        float stallTimer = 0f;

        while (timer < ApproachFunctionTimeout)
        {
            if (enemy0.IsDie || !item.CanBeOperatedBy(enemy0) || item.ItemTransform == null)
            {
                break;
            }

            if (IsArrivedAtOperatePos(item))
            {
                break;
            }

            //长时间没靠近就当成"走不过去", 不再空等到超时
            float remain = Vector3.Distance(enemy0.transform.position, item.OperatePosition);
            stallTimer = remain < lastRemain - 0.005f ? 0f : stallTimer + Time.deltaTime;
            lastRemain = remain;
            if (stallTimer >= ApproachStallTime)
            {
                break;
            }

            //功能件可能被推着走, 每帧刷新目标点
            ai.TargetPos = item.OperatePosition;
            timer += Time.deltaTime;
            yield return null;
        }

        ai.TargetPosTypeValue = TargetPosType.Stop;
        ai.MoveStop();
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