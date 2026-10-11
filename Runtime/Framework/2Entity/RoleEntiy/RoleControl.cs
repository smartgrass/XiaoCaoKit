using System;
using System.Collections;
using cfg;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static XiaoCao.BehaviorEntity;

namespace XiaoCao
{
    //技能通用体
    public abstract class RoleControl<T> : RoleComponent<T>, IRoleControl where T : Role
    {
        public RoleControl(T _owner) : base(_owner)
        {
            AddListener();
        }

        public List<XCTaskRunner> runnerList = new List<XCTaskRunner>();
        public CharacterController cc => owner.idRole.cc;

        private void AddListener()
        {
            Data_R.skillState.AddListener(OnStateChange);
        }

        public bool IsBusy(int level = 0)
        {
            foreach (var item in runnerList)
            {
                if (item.IsBusy)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsHighLevelSkill(string skillId)
        {
            //读表 读配置
            return false;
        }

        //OnMainTaskEnd 角色恢复控制
        //OnAllTaskEnd 所有序列任务结束
        //如飞行剑气释放完后, 角色恢复控制, 但剑气还在运动
        public virtual void OnAllTaskEnd(XCTaskRunner runner)
        {
            //
            //curTaskData.Remove(runner);
        }

        public void BreakAllBusy()
        {
            int len = runnerList.Count;
            for (int i = len - 1; i > 0; i--)
            {
                runnerList[i].SetBreak();
            }
        }

        //主动打断和被动打断是不一样...需要区分
        public virtual void SetNoBusy(int runnerId)
        {
            //OnBreak();
            Debug.Log($"--- SetNoBusy");
        }

        public void OnDeadUpdate()
        {
            if (owner.lifeState != BehaviorLifeState.WillDestroy
                && Data_R.breakData.UpdateDeadEnd())
            {
                //敌人自动销毁, 友军等复活
                if (owner.RoleIdentityType == RoleIdentityType.Enemy)
                {
                    owner.lifeState = BehaviorLifeState.WillDestroy;
                    owner.Enable = false;
                    owner.DestroySelf();
                }
            }

            ;
        }

        public void OnTaskUpdate()
        {
            bool hasStop = false;
            int firstLen = runnerList.Count;
            for (int i = 0; i < firstLen; i++)
            {
                if (!runnerList[i].IsAllStop)
                {
                    runnerList[i].OnUpdate();
                }
                else
                {
                    hasStop = true;
                }
            }


            //遍历结束后, 才移除结束任务
            if (hasStop)
            {
                int len = runnerList.Count;
                for (int i = len - 1; i > 0; i--)
                {
                    var runner = runnerList[i];
                    if (runner.IsAllStop)
                    {
                        //资源回收
                        XCTaskRunner.AllEnd2(runner);
                        runnerList.RemoveAt(i);
                    }
                }
            }
        }

        public void OnMainTaskEnd(XCTaskRunner runner)
        {
            if (!runner.IsBreak)
            {
                Data_R.skillState.SetValue(ESkillState.SkillEnd);
                Debug.Log($"---  OnSkillFinish {runner.debugSkillId}");
            }
            else
            {
                Data_R.skillState.SetValue(ESkillState.Idle);
            }
        }

        private void OnStateChange(ESkillState state)
        {
            if (state == ESkillState.Idle)
            {
                SetAnimSpeed(1);
            }
        }

        private void SetAnimSpeed(float speed)
        {
            if (owner.Anim.speed != 0)
            {
                _lastAnimSpeed = owner.Anim.speed;
            }

            owner.Anim.speed = speed;
        }


        private UniTask animSpeedTask;
        CancellationTokenSource cts;

        //动画顿帧
        private void AddAnimHitStop(float stopTime = 0.5f)
        {
            SetAnimSpeed(0);
            if (cts != null && animSpeedTask.Status == UniTaskStatus.Pending)
            {
                cts.Cancel();
                cts.Dispose();
                Debuger.Log("--- cancellationTokenSource");
            }

            cts = new CancellationTokenSource();
            animSpeedTask = XCTime.DelayRun(stopTime, () => { SetAnimSpeed(1); }, cts);
        }

        //基本技能条件
        public bool IsRoleCanPlaySkill()
        {
            //条件判断, 耗蓝等等
            if (!Data_R.IsStateFree || IsBusy())
                return false;

            return true;
        }

        public virtual TaskInfo TryPlaySkill(string skillId, string callFrom = "", int callCount = 0)
        {
            if (!IsRoleCanPlaySkill())
            {
                return null;
            }

            return RcpPlaySkill(skillId, callFrom, callCount);
        }

        public virtual TaskInfo RcpPlaySkill(string skillId, string callFrom = "", int callCount = 0)
        {
            PreSkillStart(skillId);
            Data_R.tempCurSkillId = skillId;
            Transform selfTf = owner.transform;
            var taskInfo = GetTaskInfo(skillId, callCount, selfTf);

            //来源标记:1 技能接下一段技能时从第 8 帧开始
            if (callFrom == SkillCallFrom.PlayNextSkill_1)
            {
                taskInfo.startFrame = 8;
            }

            //指定技能目录,
            int skillDirId = owner.raceId;

            var task = XCTaskRunner.CreatNew(skillId, skillDirId, taskInfo);
            if (task == null)
            {
                Debug.LogError($"--- task null {skillId} ");
                return null;
            }

            runnerList.Add(task);
            task.onMainEndEvent.AddListener(OnMainTaskEnd);
            task.onAllTaskEndEvent.AddListener(OnAllTaskEnd);
            Data_R.skillState.SetValue(ESkillState.Skill);

            return taskInfo;
        }

        private TaskInfo GetTaskInfo(string skillId, int callCount, Transform selfTf)
        {
            TaskInfo taskInfo = new TaskInfo()
            {
                role = owner,
                skillId = skillId,
                entityId = owner.id,
                playerTF = selfTf,
                castEuler = selfTf.eulerAngles,
                castPos = selfTf.position,
                playerAnimator = owner.Anim,
                speedMult = 1,
            };

            taskInfo.callCount = callCount;

            if (IsNorAtkId(skillId))
            {
                taskInfo.speedMult = 1 + owner.RoleAttr.GetValue(EAttr.NorAtkSpeedAdd);
            }

            //技能3:整体缩放随技能等级变化
            if (skillId == Skill3Id)
            {
                AddSkill3ScaleModify(taskInfo);
            }

            return taskInfo;
        }

        /// <summary>技能3 的 id</summary>
        private const string Skill3Id = "3";

        /// <summary>技能3 随等级缩放的子物体名</summary>
        private const string Skill3ScaleTarget = "s3_ring_2";

        /// <summary>
        /// 技能3:按当前等级读取 SkillUpgradeSetting.CostomValues 里的缩放加成,
        /// 以 ModifyTaskInfo(key = Scale) 的形式写入 ModifyDic,最终倍数 = 1 + 加成
        /// </summary>
        private void AddSkill3ScaleModify(TaskInfo taskInfo)
        {
            var setting = LubanTables.GetSkillUpgradeSetting(Skill3Id);
            if (setting.CostomValues == null || setting.CostomValues.Count == 0)
            {
                Debug.LogError($"--- {Skill3Id} 技能的 CostomValues 未配置缩放加成");
                return;
            }

            int level = PlayerSaveData.LocalSavaData.GetSkillLevel(Skill3Id);
            //等级超出配置长度时取最后一条
            int index = Mathf.Clamp(level - 1, 0, setting.CostomValues.Count - 1);
            //表里配的是加成比例,如 0.05 表示整体放大 1.05 倍
            float num = setting.CostomValues[index];

            taskInfo.ModifyDic[TaskModifyRule.ScaleKey] = new ModifyTaskInfo()
            {
                target = Skill3ScaleTarget,
                num = num,
            };
        }

        private bool IsNorAtkId(string id)
        {
            var setting = LubanTables.GetSkillSetting(id, 0);
            return setting.ActType == cfg.Skill.EActType.NorAtk;
        }


        //技能开始前根据输入调整方向 等数据
        protected virtual void PreSkillStart(string skillId)
        {
        }

        private float defaultSeeAngle = 30;
        private float defaultSeeRadous = 6f;


        public virtual void DefaultAutoDirect(float lerp = 0.4f)
        {
            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, defaultSeeRadous, defaultSeeAngle,
                out float maxScore, owner.team);
            if (findRole != null)
            {
                owner.transform.RotaToPos(findRole.transform.position, lerp);
                Debug.Log($"--- findRole RotaToPos {findRole.gameObject}");
            }
            else
            {
                Debug.Log($"--- findRole no");
            }
        }

        /// <summary>
        /// 索敌转向:朝搜索范围内最近的敌人转向,找不到敌人时保持原朝向。
        /// seeAngle 传 180 表示不限角度(全向),只按距离搜索
        /// </summary>
        /// <returns>是否找到敌人</returns>
        public virtual bool AutoDirectToEnemy(float lerp = 1, float seeR = 6f, float seeAngle = 180f)
        {
            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, seeR, seeAngle, out float maxScore, owner.team);
            if (findRole == null)
            {
                return false;
            }

            owner.transform.RotaToPos(findRole.transform.position, lerp);
            return true;
        }

        private float _lastAnimSpeed = 1;

        public void StopTimeSpeed(bool isOn)
        {
            if (isOn)
            {
                SetAnimSpeed(0);
                foreach (var item in runnerList)
                {
                    item.StopTimeSpeed(isOn);
                }
            }
            else
            {
                SetAnimSpeed(_lastAnimSpeed);
                foreach (var item in runnerList)
                {
                    item.StopTimeSpeed(isOn);
                }
            }
        }


        public void DoActCombo(string[] cmdList, Action onFinish = null)
        {
            owner.idRole.StartCoroutine(IEActCombo(cmdList, onFinish));
        }

        //开启协程
        public IEnumerator IEWaitActCombo(string[] cmdList, Action onFinish = null)
        {
            yield return owner.idRole.StartCoroutine(IEActCombo(cmdList, onFinish));
        }


        private IEnumerator IEActCombo(string[] cmdList, Action onFinish)
        {
            foreach (string cmd in cmdList)
            {
                TryPlaySkill(cmd);
                yield return null;
                yield return new WaitUntil(NoBusy);
            }

            onFinish?.Invoke();
        }

        private bool NoBusy()
        {
            return !owner.data_R.IsBusy;
        }
    }

    public interface IRoleControl
    {
        public TaskInfo TryPlaySkill(string skillId, string callFrom = "", int callCount = 0);

        public bool IsBusy(int level = 0);

        //占用当前
        public void BreakAllBusy();

        public void DefaultAutoDirect(float lerp = 0.4f);
        void StopTimeSpeed(bool isOn);
        public IEnumerator IEWaitActCombo(string[] cmdList, Action onFinish = null);
    }

    /// <summary>
    /// 技能触发来源标记,用于区分同一技能的不同来源(例如 1 技能接下一段技能)
    /// </summary>
    public static class SkillCallFrom
    {
        /// <summary>PlayNextSkill 来源的前缀,完整写法为 前缀 + 触发方技能 id</summary>
        public const string PlayNextSkillPrefix = "PlayNextSkill_";

        /// <summary>1 技能接下一段技能(该来源下起始帧为 8)</summary>
        public const string PlayNextSkill_1 = "PlayNextSkill_1";
    }

    /// <summary>
    /// NextSkill 链条规则:调用次数上限等
    /// </summary>
    public static class SkillNextRule
    {
        /// <summary>
        /// 取技能的 NextSkill 最大触发次数:按技能等级取 SkillUpgradeSetting.CostomValues,
        /// 1 级对应第 0 个数据,等级超出数组长度时取最后一个
        /// </summary>
        public static int GetMaxCallCount(string skillId)
        {
            SkillUpgradeSetting setting = LubanTables.GetSkillUpgradeSetting(skillId);
            if (setting.CostomValues == null || setting.CostomValues.Count == 0)
            {
                return 0;
            }

            int level = PlayerSaveData.LocalSavaData.GetSkillLevel(skillId);
            int index = Mathf.Clamp(level - 1, 0, setting.CostomValues.Count - 1);
            //连冲次数是整数,表里已改成 float 列表,这里取整
            return (int)setting.CostomValues[index];
        }
    }
}
