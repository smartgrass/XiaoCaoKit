using System;
using System.Collections.Generic;

namespace XiaoCao
{
    /// <summary>
    /// 任务系统存档数据: 记录每个任务的累计进度与领奖状态, 由 <see cref="TaskMgr"/> 解读
    /// </summary>
    [Serializable]
    public class TaskData
    {
        //任务进度记录, key = 任务配置id
        public Dictionary<string, TaskRecord> records = new Dictionary<string, TaskRecord>();

        public void CheckNull()
        {
            records ??= new Dictionary<string, TaskRecord>();
        }

        /// <summary>
        /// 获取任务记录, 不存在时创建
        /// </summary>
        public TaskRecord GetOrAddRecord(string taskId)
        {
            if (!records.TryGetValue(taskId, out TaskRecord record))
            {
                record = new TaskRecord();
                records[taskId] = record;
            }
            return record;
        }

        /// <summary>
        /// 累加任务的存储进度(仅累计型任务使用), 超出部分不做限制, 读取时按目标值收敛
        /// </summary>
        public void AddProgress(string taskId, int delta)
        {
            GetOrAddRecord(taskId).progress += delta;
        }

        /// <summary>
        /// 清空全部任务记录(调试用)
        /// </summary>
        public void ClearAll()
        {
            records.Clear();
        }
    }

    [Serializable]
    public class TaskRecord
    {
        //累计型任务的已累计次数
        public int progress;

        //奖励是否已领取
        public bool claimed;
    }
}
