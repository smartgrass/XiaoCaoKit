using System;
using System.Collections.Generic;

namespace XiaoCao
{
    /// <summary>
    /// 技能运行时修改项,播放技能前由调用方写入 <see cref="TaskInfo.ModifyDic"/>
    /// </summary>
    [Serializable]
    public class ModifyTaskInfo
    {
        /// <summary>目标子物体名,如 s3_ring_2(也可直接写完整资源路径)</summary>
        public string target;

        /// <summary>按 key 解释的字符串参数</summary>
        public string strMsg;

        /// <summary>按 key 解释的数值参数,如 Scale 时为缩放增量,最终倍数 = 1 + num</summary>
        public float num;
    }

    /// <summary>
    /// <see cref="TaskInfo.ModifyDic"/> 的匹配规则:按 key 找到修改项并作用到目标子物体
    /// </summary>
    public static class TaskModifyRule
    {
        /// <summary>整体缩放:命中后目标子物体整体 scale 变为 (1 + num) 倍</summary>
        public const string ScaleKey = "Scale";

        /// <summary>
        /// 取子物体的整体缩放倍数:遍历 ModifyDic 中 key 为 Scale 的项,命中多条时相乘
        /// </summary>
        /// <param name="info">技能运行信息</param>
        /// <param name="objectData">子物体数据,主时间轴上没有子物体时传 null</param>
        /// <returns>缩放倍数,没有命中项时为 1</returns>
        public static float GetScaleFactor(TaskInfo info, ObjectData objectData)
        {
            if (objectData == null || info.ModifyDic.Count == 0)
            {
                return 1f;
            }

            float factor = 1f;
            bool hasModify = false;
            foreach (KeyValuePair<string, ModifyTaskInfo> kv in info.ModifyDic)
            {
                if (kv.Key != ScaleKey || !IsTarget(kv.Value.target, objectData.ObjectPath))
                {
                    continue;
                }

                factor *= 1f + kv.Value.num;
                hasModify = true;
            }

            return hasModify ? factor : 1f;
        }

        /// <summary>
        /// 目标匹配:target 与资源路径的文件名(去掉扩展名)相同即命中,也支持直接写完整路径
        /// </summary>
        private static bool IsTarget(string target, string objectPath)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(objectPath))
            {
                return false;
            }

            if (target == objectPath)
            {
                return true;
            }

            //"xxx/s3_ring_2.prefab" 与 "s3_ring_2" 视为同一个目标,这里手工取文件名避免每帧产生字符串
            int nameStart = objectPath.LastIndexOf('/') + 1;
            int nameEnd = objectPath.LastIndexOf('.');
            if (nameEnd <= nameStart)
            {
                nameEnd = objectPath.Length;
            }

            int nameLength = nameEnd - nameStart;
            return target.Length == nameLength && string.CompareOrdinal(target, 0, objectPath, nameStart, nameLength) == 0;
        }
    }
}
