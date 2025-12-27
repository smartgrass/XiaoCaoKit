using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;
using static XiaoCao.NpcShowAction;

namespace XiaoCao
{
    /// <see cref="ShowActKeys"/>>
    /// <see cref="EnemyShowAction"/>>
    public abstract class BaseShowAction : GameStartMono
    {
        [Multiline(12)] public string taskLines;

        public virtual CharacterController Cc => GameDataCommon.LocalPlayer.idRole.cc;

        public Animator Animator
        {
            get
            {
                if (!animator)
                {
                    animator = Cc.GetComponentInChildren<Animator>();
                }

                return animator;
            }
        }

        private Animator animator;

        private string _reciveMsg;
        private string _waitMsg;
        private bool _msgReceived;

        protected bool isOverrideRunAct;

        public override void Start()
        {
            base.Start();
            GameEvent.AddEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReciveMsg);
        }

        private void OnReciveMsg(string str)
        {
            if (!string.IsNullOrEmpty(_waitMsg) && str == _waitMsg)
            {
                _msgReceived = true;
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            GameEvent.RemoveEventListener<string>(EGameEvent.MapMsg.ToInt(), OnReciveMsg);
        }


        public IEnumerator IETaskRun()
        {
            OnTaskStart();

            //等待Npc启动完毕
            yield return null;

            string[] list = taskLines.Split('\n');

            foreach (string line in list)
            {
                yield return StartCoroutine(RunLine(line));
            }

            OnTaskEnd();
        }

        public virtual void OnTaskEnd()
        {
        }

        public virtual void OnTaskStart()
        {
        }

        public IEnumerator RunLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                yield break;
            }

            // 使用TaskDataHelper解析任务数据
            ShowActData showActData = ShowActHelper.ParseFromString(line);

            yield return RunActData(showActData);
        }

        public virtual IEnumerator RunActData(ShowActData showActData)
        {
            // 根据actName执行不同的动作
            switch (showActData.actName)
            {
                case ShowActKeys.Wait:
                    // 等待指定时间
                    if (float.TryParse(showActData.content, out float waitTime))
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid wait time: {showActData.content}");
                    }

                    break;
                case ShowActKeys.WaitMsg:
                    // 等待消息
                    _msgReceived = false;
                    if (string.IsNullOrEmpty(showActData.content))
                    {
                        Debug.LogWarning("WaitMsg action requires a message content to wait for");
                        break;
                    }

                    _waitMsg = showActData.content;

                    // 等待消息接收
                    while (!_msgReceived)
                    {
                        yield return null;
                    }

                    _waitMsg = null;

                    break;
                case ShowActKeys.Anim:
                    Debug.Log($"--- taskData {showActData}");
                    // 播放动画行为
                    Animator.Play(showActData.content);
                    // 等待一帧确保动画开始播放
                    yield return null;
                    break;

                case ShowActKeys.EnterTalk:
                    TalkMgr.Inst.EnterTalk();
                    break;
                case ShowActKeys.Talk:
                    // 说话行为
                    if (!string.IsNullOrEmpty(showActData.content))
                    {
                        // 这里可以集成对话系统
                        TalkMgr.Inst.StartTalk(showActData.content);
                        while (TalkMgr.Inst.isTalking)
                        {
                            yield return null;
                        }
                    }

                    Debug.Log($"-- Talk End");
                    break;

                case ShowActKeys.MoveLocal:
                    // 移动行为 - 这里只是一个示例实现
                    if (!string.IsNullOrEmpty(showActData.content))
                    {
                        ShowActMoveData moveData = ShowActMoveData.ParseMoveData(showActData.content);
                        yield return StartCoroutine(MoveToLocalPosition(moveData));
                    }

                    break;
                case ShowActKeys.SetTargetMove:
                    Debug.Log($"--- SetTargetMove TODO");
                    break;
                case ShowActKeys.Active:
                    // 激活/禁用对象
                    HandleActiveAction(showActData.content);
                    yield return null;
                    break;

                case ShowActKeys.Event:
                    Debug.Log($"-- Event {showActData.content}");
                    // 发送事件
                    if (!string.IsNullOrEmpty(showActData.content))
                    {
                        // 这里可以根据需要发送不同类型的事件
                        //GameEvent.Send<string>(EGameEvent.CustomEvent.Int(), taskData.content);
                        Debug.Log($"Send event: {showActData.content}");
                        var array = showActData.content.Split(",");
                        if (array.Length == 0)
                        {
                            break;
                        }

                        if (array[0] == "DoSkill")
                        {
                            string skillId = array[1];
                            string[] cmdList = skillId.Split("|");
                            yield return GameDataCommon.LocalPlayer.component.control.IEWaitActCombol(cmdList, null);
                        }
                    }

                    yield return null;
                    break;

                case ShowActKeys.Loop:
                    // 循环控制暂时不在此方法中处理
                    Debug.LogWarning("Loop action should be handled at a higher level");
                    yield return null;
                    break;

                case ShowActKeys.EndLoop:
                    // 循环结束控制
                    Debug.LogWarning("EndLoop action should be handled at a higher level");
                    yield return null;
                    break;
                case ShowActKeys.LevelFinish:
                    GameMgr.Inst.LevelFinish();
                    break;

                case ShowActKeys.KillEnemy:
                    RoleMgr.Inst.KillEnemy();
                    break;
                default:
                    Debug.LogError($"Unknown action: {showActData.actName}");
                    yield return null;
                    break;
            }
        }


        // 执行本地坐标移动 - 使用CharacterController
        private IEnumerator MoveToLocalPosition(ShowActMoveData moveData)
        {
            // 计算目标世界位置
            Vector3 startPosition = Cc.transform.position;
            Vector3 targetPosition = Cc.transform.TransformPoint(moveData.moveLocal);

            // 计算移动方向和距离
            Vector3 moveDirection = (targetPosition - startPosition).normalized;
            float distance = Vector3.Distance(startPosition, targetPosition);

            // 如果距离太小则直接设置位置
            if (distance < 0.01f)
            {
                yield break;
            }

            float elapsedTime = 0f;
            float moveSpeed = distance / moveData.moveTime;

            while (elapsedTime < moveData.moveTime)
            {
                if (!Cc)
                {
                    yield break;
                }

                // 计算当前帧的移动向量
                float deltaTime = Time.deltaTime;
                float moveDistance = moveSpeed * deltaTime;

                // 使用CharacterController移动
                Vector3 move = moveDirection * moveDistance;
                // 添加重力影响
                move += Physics.gravity * deltaTime;

                // 执行移动
                Cc.Move(move);

                elapsedTime += deltaTime;
                yield return null;
            }

            // 确保最终位置准确 - 使用额外的小步长调整
            if (Cc)
            {
                Vector3 finalDirection = (targetPosition - Cc.transform.position).normalized;
                float finalDistance = Vector3.Distance(Cc.transform.position, targetPosition);
                float adjustSpeed = finalDistance / 0.1f; // 0.1秒内调整完成

                float adjustTimer = 0f;
                while (adjustTimer < 0.1f && finalDistance > 0.01f)
                {
                    float deltaTime = Time.deltaTime;
                    float moveDistance = adjustSpeed * deltaTime;

                    Vector3 move = finalDirection * moveDistance;
                    move += Physics.gravity * deltaTime;

                    Cc.Move(move);

                    finalDistance = Vector3.Distance(transform.position, targetPosition);
                    adjustTimer += deltaTime;
                    yield return null;
                }
            }
        }


        // 处理激活/禁用对象的辅助方法
        private void HandleActiveAction(string content)
        {
            string[] parts = content.Split(',');
            if (parts.Length >= 1)
            {
                string objectName = parts[0].Trim();
                bool activeState = true;
                if (parts.Length >= 2)
                {
                    activeState = parts[1].Trim().ToLower() == "true";
                }

                GameObject targetObject = null;
                if (MarkObjectMgr.TryGet(objectName, out var game))
                {
                    targetObject = game;
                }
                else
                {
                    //在当前层级查找
                    Transform find = transform.FindChildEx(objectName);
                    if (find)
                    {
                        targetObject = find.gameObject;
                    }
                    else if (transform.parent)
                    {
                        find = transform.parent.FindChildEx(objectName);
                        if (find)
                        {
                            targetObject = find.gameObject;
                        }
                    }
                    else
                    {
                        Debug.Log($"-- GameObject Find :{objectName}");
                        targetObject = GameObject.Find(objectName);
                    }
                }

                if (targetObject)
                {
                    targetObject.SetActive(activeState);
                }
                else
                {
                    Debug.LogError($"Object not found: {objectName}");
                }
            }
        }

        // 消息接收处理方法
        private void OnWaitMsgReceived(string receivedMsg, string expectedMsg)
        {
            if (receivedMsg == expectedMsg)
            {
                _msgReceived = true;
            }
        }
    }


    //格式: "actName:content"
    /// <summary>
    /// <see cref="ShowActKeys"/>
    /// </summary>
    public struct ShowActData
    {
        public string actName;
        public string content;

        public string[] GetContentArray()
        {
            return content.Split(',');
        }

        /// <summary>
        /// 将ShowActData转换为字符串表示
        /// 格式: "actName:content"
        /// 子符号: , /
        /// </summary>
        /// <returns>格式化的字符串</returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(actName))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(content))
            {
                return actName;
            }

            return $"{actName}:{content}";
        }

        /// <summary>
        /// 从字符串解析ShowActData
        /// 格式: "actName:content"
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>解析后的ShowActData结构</returns>
        public static ShowActData ParseFromString(string input)
        {
            ShowActData data = new ShowActData();

            if (string.IsNullOrEmpty(input))
            {
                return data;
            }

            // 移除首尾空格并按冒号分割
            string[] parts = input.Trim().Split(new char[] { ':' }, 2); // 只分割第一个冒号
            data.actName = parts[0].Trim();

            if (parts.Length > 1)
            {
                data.content = parts[1].Trim();
            }

            return data;
        }
    }

    //示例: moveLocal,1,0,2,2.5  
    public struct ShowActMoveData
    {
        public Vector3 moveLocal;
        public float moveTime;

        // 解析移动数据
        public static ShowActMoveData ParseMoveData(string content)
        {
            ShowActMoveData moveData = new ShowActMoveData();

            // 默认移动时间
            moveData.moveTime = 1.0f;

            if (string.IsNullOrEmpty(content))
                return moveData;

            string[] parts = content.Split(',');

            if (float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                moveData.moveLocal = new Vector3(x, y, z);
            }

            if (float.TryParse(parts[3], out float time))
            {
                moveData.moveTime = time;
            }

            return moveData;
        }
    }

    public class ShowActHelper
    {
        /// <summary>
        /// 将字符串按逗号分隔转换为TaskData
        /// 格式: "actName:content"
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>解析后的TaskData结构</returns>
        public static ShowActData ParseFromString(string input)
        {
            ShowActData taskData = new ShowActData();

            if (string.IsNullOrEmpty(input))
            {
                return taskData;
            }

            string[] parts = input.Trim().Split(':');

            taskData.actName = parts[0].Trim();
            if (parts.Length > 1)
            {
                taskData.content = parts[1].Trim();
            }

            return taskData;
        }

        /// <summary>
        /// 将TaskData转换为字符串
        /// </summary>
        /// <param name="taskData">TaskData结构</param>
        /// <returns>格式化后的字符串</returns>
        public static string ToString(ShowActData taskData)
        {
            return $"{taskData.actName}:{taskData.content}";
        }


        public static float[] GetNumArray(string content)
        {
            string[] parts = content.Split(',');
            return parts.Select(float.Parse).ToArray();
        }
    }

    public static class ShowActKeys
    {
        /// <summary>
        /// 等待指定时间
        /// 用法: wait:2.5
        /// </summary>
        public const string Wait = "Wait";

        public const string WaitMsg = "WaitMsg";

        /// <summary>
        /// 播放动画
        /// 用法: anim:attack
        /// </summary>
        public const string Anim = "Anim";

        /// <summary>
        /// 移动到指定位置
        /// 用法: move:position1
        /// </summary>
        public const string MoveLocal = "MoveLocal";

        public const string SetHp = "SetHp";

        /// <summary>
        /// (moveType,content)
        /// Transform targetName speed stopDis
        /// Point,vec3,speed stopDis
        /// Stop
        /// Default
        /// vec3: x/y/z
        /// </summary>
        public const string SetTargetMove = "SetTargetMove";

        /// <summary>
        /// 激活/禁用游戏对象
        /// 用法: Active:objectName,true
        /// </summary>
        public const string Active = "Active";

        public const string EnterTalk = "EnterTalk";

        public const string Talk = "Talk";

        /// <summary>
        /// 发送事件消息
        /// 用法: event:CustomEventName
        /// </summary>
        public const string Event = "Event";

        /// <summary>
        /// 循环执行指定次数
        /// 用法: loop:3 (开始循环3次)
        ///     endloop (结束循环)
        /// </summary>
        public const string Loop = "Loop";

        public const string EndLoop = "EndLoop";

        public const string FollowPlayer = "FollowPlayer";

        public const string LevelFinish = "LevelFinish";
        
        public const string KillEnemy = "KillEnemy";

        public static List<string> GetShortKeys()
        {
            //使用反射获取所有const字段
            List<string> keys = new List<string>();
            var fields = typeof(ShowActKeys).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly);
            foreach (var field in fields)
            {
                keys.Add(field.Name);
            }
            return keys;

        }
    }
}