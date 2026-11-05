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

    /// <summary>带"守着功能件"Tag 的角色被踢下功能件后, 隔多久(秒)重新去操作</summary>
    private const float ReoperateDelay = 1f;

    /// <summary>被打断过就彻底放弃功能件, 不再执行 FindFunction</summary>
    private bool hasGivenUpFunction;

    /// <summary>正在跑的演出协程, 重复触发时先停掉旧的</summary>
    private Coroutine showActCoroutine;

    /// <summary>当前占着的功能件; 重开演出时要先放开, 否则炮台会一直以为有人在操作</summary>
    private IFunctionItem operatedItem;

    public Enemy0 enemy0;

    public override CharacterController Cc => enemy0.idRole.cc;

    public override Role GetRole()
    {
        return enemy0;
    }

    public void OnEnemyCreat(EnemyCreator enemyCreator, Enemy0 enemy)
    {
        RunShowAction(enemy, taskLines);
    }

    /// <summary>
    /// 让一个已存在的角色直接跑一段 ShowAct 文本(不经过 <see cref="EnemyCreator"/>)。
    /// 运行时召唤出来的支援角色身上没有演出组件, 要让它自己去操作功能件就用这个:
    /// <c>RunShowAction(role, $"{ShowActKeys.FindFunction}:20")</c>
    /// </summary>
    public void RunShowAction(Enemy0 enemy, string lines)
    {
        if (string.IsNullOrEmpty(lines))
        {
            Debug.LogWarning($"--- {name} ShowAct 内容为空, 跳过");
            return;
        }

        enemy0 = enemy;
        taskLines = lines;
        //重新开始一段演出, 上一段的"放弃功能件"不该带到这一段来
        hasGivenUpFunction = false;

        if (showActCoroutine != null)
        {
            StopCoroutine(showActCoroutine);
            showActCoroutine = null;
        }

        //上一段可能正占着炮台, 先放开再重来
        ReleaseOperatedItem();

        showActCoroutine = StartCoroutine(IETaskRun());
    }

    public override void OnTaskStart()
    {
        enemy0.AiControl.IsOnShowAction = true;
        //每次演出重新开始,"跟随玩家"必须在本段里重新触发, 不能继承上一段的
        enemy0.AiControl.IsShowFollowPlayer = false;
    }

    public override void OnTaskEnd()
    {
        Debug.Log($"--- OnTaskEnd");
        enemy0.AiControl.IsOnShowAction = false;
        enemy0.AiControl.IsShowFollowPlayer = false;
    }


    public override IEnumerator RunActData(ShowActData showActData)
    {
        isOverrideRunAct = false;
        switch (showActData.actName)
        {
            case ShowActKeys.FollowPlayer:
                //切回玩家为目标, 同时进入寻路模式: 演出中与演出后都沿通道绕墙追
                enemy0.AiControl.FollowPlayer();
                isOverrideRunAct = true;
                //跟随是持续状态: 期间不进 AI 状态机(不会攻击), 追到目标距离/到点后交回 AI 开打
                yield return IEKeepFollowPlayer(showActData);
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
    /// 保持"跟随玩家"这段演出, 一直到进入目标距离。
    /// <para>跟随期间 <see cref="AIControl.IsOnShowAction"/> 一直为 true, AIControl.Update 直接走
    /// <see cref="AIControl"/> 的演出跟随分支, 不会更新 AI 状态机, 所以敌人只会寻路贴近玩家, 不会进入攻击循环。</para>
    /// <para>内容留空(推荐): 走到离玩家 <see cref="AIControl.TargetStopDistance"/> 以内就结束跟随, 交回 AI 状态机,
    /// 下一帧直接进 PreAtkState → AtkState 的攻击循环。想让"追到就开打"就把这一行放在最后。</para>
    /// <para>内容填数字(秒): 最多跟这么多秒, 到点也交回 AI(<see cref="BaseShowAction"/> 的秒数写法)。</para>
    /// <para>其它结束条件: 被受击打断(AIControl.OnDamageAct 会清标记) / 玩家死亡 / 敌人死亡。</para>
    /// </summary>
    private IEnumerator IEKeepFollowPlayer(ShowActData showActData)
    {
        AIControl ai = enemy0.AiControl;

        //留空 = 追到目标距离就收工; 填了秒数 = 只按时间结束, 不再看距离
        float limit = -1f;
        bool stopOnArrive = true;
        if (!string.IsNullOrEmpty(showActData.content))
        {
            if (float.TryParse(showActData.content, out float seconds))
            {
                limit = seconds;
                stopOnArrive = false;
            }
        }

        float timer = 0f;
        while (ai.IsShowFollowPlayer && !enemy0.IsDie)
        {
            if (ai.targetRole == null || ai.targetRole.IsDie)
            {
                break;
            }

            if (stopOnArrive && IsNearFollowTarget(ai))
            {
                break;
            }

            if (limit >= 0f)
            {
                timer += Time.deltaTime;
                if (timer >= limit)
                {
                    break;
                }
            }

            yield return null;
        }

        //清标记后 AI 状态机立刻接管: 已经在攻击距离内, PreAtkState 直接退出, 开始攻击循环
        ai.IsShowFollowPlayer = false;
    }

    /// <summary>
    /// 已经到了跟随的停止距离内(与 AIControl 停下移动用的是同一个距离), 跟随可以结束、交给 AI 开打。
    /// </summary>
    private bool IsNearFollowTarget(AIControl ai)
    {
        if (ai.targetRole == null)
        {
            return true;
        }

        return Vector3.Distance(enemy0.transform.position, ai.targetRole.transform.position) <= ai.TargetStopDistance;
    }

    /// <summary>
    /// 查找并操作周围的功能件(目前只有炮台): 内容填搜索半径(米), 留空用 DefaultFindFunctionRadius。
    /// 找到后一直占着, 直到被打出 break(破防)/死亡/被别人接手。普通受击不会断开操控,
    /// break 断开由功能件自己处理(见 Turret.OnOperatorDamaged)。
    /// 被踢下炮台之后看角色身上有没有 <see cref="RoleTagCommon.KeepOperateFunction"/>:
    /// 没有就彻底放弃(<see cref="hasGivenUpFunction"/>, 敌人都是这样);
    /// 有就一直守着, 隔 <see cref="ReoperateDelay"/> 秒重新找炮台接着操作(召唤出来的支援角色会带这个 Tag)。
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

        //带 Tag 的角色被踢下炮台后回到这里重来一轮; 其它退出原因都是直接结束
        while (true)
        {
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

            operatedItem = item;

            //功能件被拿走后 Operator 会变空, 角色死亡时也要退出
            while (item.Operator == enemy0 && !enemy0.IsDie)
            {
                yield return null;
            }

            //死亡/被打断都把功能件交还, 否则炮台会被一个已经离开的角色一直挂着
            ReleaseOperatedItem();

            if (enemy0.IsDie)
            {
                yield break;
            }

            //没带"守着功能件"的 Tag: 被踢下炮台就彻底放弃, 这段演出结束(敌人都是这样)
            if (!enemy0.HasTag(RoleTagCommon.KeepOperateFunction))
            {
                hasGivenUpFunction = true;
                yield break;
            }

            //带 Tag 的(召唤出来的支援角色): 只要 Tag 还在就一直守着炮台, 被踢下来就等一会儿重新操作。
            //等待期间先关 AI: 支援角色是 FriendState.Follow, AI 一开就会朝玩家走, 1 秒后再回炮台会来回折返
            enemy0.IsAiOn = false;
            enemy0.AiControl.MoveStop();

            yield return new WaitForSeconds(ReoperateDelay);

            if (enemy0.IsDie || !enemy0.HasTag(RoleTagCommon.KeepOperateFunction))
            {
                yield break;
            }

            enemy0.IsAiOn = true;
        }
    }

    /// <summary>放开当前占着的功能件(正常结束时由 FindFunction 自己放, 这里给"重开演出"兜底)</summary>
    private void ReleaseOperatedItem()
    {
        if (operatedItem == null)
        {
            return;
        }

        IFunctionItem item = operatedItem;
        operatedItem = null;

        //功能件可能已经被销毁, 接口引用拿不到 Unity 的 == 重载, 绕到 Object 上判空
        if (item is UnityEngine.Object unityObj && unityObj == null)
        {
            return;
        }

        item.StopOperate(enemy0);
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