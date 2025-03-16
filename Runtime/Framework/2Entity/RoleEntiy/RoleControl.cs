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
        public RoleControl(T _owner) : base(_owner) { AddListener(); }

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
        public void OnBreak()
        {
            int len = runnerList.Count;
            for (int i = len - 1; i > 0; i--)
            {
                runnerList[i].SetBreak();
            }
        }

        //主动打断和被动打断是不一样...需要区分
        public void SetNoBusy()
        {
            OnBreak();
        }

        public void OnDeadUpdate()
        {
            if (owner.lifeState != BehaviorLifeState.WillDestroy
                && Data_R.breakData.UpdateDeadEnd())
            {
                owner.lifeState = BehaviorLifeState.WillDestroy;
                owner.Enable = false;
                owner.DestroySelf();
            };
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
                        Debug.Log($"--- RemoveAt {i}");
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
                Debug.Log($"---  OnSkillFinish ");
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

        public virtual void TryPlaySkill(string skillId)
        {
            if (!IsRoleCanPlaySkill())
            {
                return;
            }

            RcpPlaySkill(skillId);
        }

        public virtual void RcpPlaySkill(string skillId)
        {
            PreSkillStart(skillId);
            Data_R.curSkillId = skillId;
            Transform selfTf = owner.transform;
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

            if (IsNorAtkId(skillId))
            {
                taskInfo.speedMult = 1 + owner.PlayerAttr.GetValue(EAttr.NorAtkSpeedAdd);
            }

            //指定技能目录,
            int skillDirId = owner.raceId;

            var task = XCTaskRunner.CreatNew(skillId, skillDirId, taskInfo);
            if (task == null)
            {
                Debug.LogError($"--- task null {skillId} ");
                return;
            }
            runnerList.Add(task);
            task.onMainEndEvent.AddListener(OnMainTaskEnd);
            task.onAllTaskEndEvent.AddListener(OnAllTaskEnd);
            Data_R.skillState.SetValue(ESkillState.Skill);
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

        public virtual void DefaultAutoDirect()
        {

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
    }

    public interface IRoleControl
    {
        public void TryPlaySkill(string skillId);

        public bool IsBusy(int level = 0);
        public void SetNoBusy();

        public void DefaultAutoDirect();
        void StopTimeSpeed(bool isOn);
    }

}