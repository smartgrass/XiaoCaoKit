using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 同名 buff 的时间处理方式
    /// </summary>
    public enum EBuffTimeMode
    {
        /// <summary> 取剩余时间的较大值 </summary>
        SetMax,

        /// <summary> 叠加时间 </summary>
        Add,
    }

    /// <summary>
    /// 玩家 buff 倒计时 UI 管理器: 实例化 BuffStateUI, 逐帧刷新填充进度, 时间结束后自动移除。
    /// </summary>
    public class BuffStateMgr
    {
        //单个 buff 的倒计时数据
        private class BuffStateData
        {
            public string buffName;
            public float totalTime;   //最大剩余时间, 作为进度分母
            public float remainTime;  //剩余时间
            public BuffStateUI ui;
        }

        private readonly Transform parent;
        private readonly BuffStateUI template;
        private readonly List<BuffStateData> stateList = new List<BuffStateData>();

        /// <summary>
        /// 传入 buff 图标容器, 容器下的第一个子物体作为格子模板并隐藏。
        /// </summary>
        public BuffStateMgr(Transform parent)
        {
            this.parent = parent;
            template = parent.GetChild(0).GetComponent<BuffStateUI>();
            template.gameObject.SetActive(false);
        }

        /// <summary>
        /// 添加 buff 的倒计时显示; 同名 buff 按 mode 处理: SetMax 取较大时间, Add 叠加时间。
        /// </summary>
        public void AddBuffUI(string buffName, float buffTime, EBuffTimeMode mode)
        {
            BuffStateData data = FindState(buffName);
            if (data == null)
            {
                BuffStateUI ui = Object.Instantiate(template, parent);
                ui.gameObject.SetActive(true);
                ui.SetBuffIcon(buffName);
                data = new BuffStateData { buffName = buffName, ui = ui };
                stateList.Add(data);
            }

            if (mode == EBuffTimeMode.Add)
            {
                data.remainTime += buffTime;
            }
            else
            {
                data.remainTime = Mathf.Max(data.remainTime, buffTime);
            }

            data.totalTime = Mathf.Max(data.totalTime, data.remainTime);
            data.ui.SetRemainRatio(data.remainTime / data.totalTime);
        }

        /// <summary>
        /// 逐帧刷新剩余时间与填充进度, 时间结束的 buff 会被移除。
        /// </summary>
        public void Update()
        {
            for (int i = stateList.Count - 1; i >= 0; i--)
            {
                BuffStateData data = stateList[i];
                data.remainTime -= Time.deltaTime;
                if (data.remainTime <= 0)
                {
                    Object.Destroy(data.ui.gameObject);
                    stateList.RemoveAt(i);
                    continue;
                }

                data.ui.SetRemainRatio(data.remainTime / data.totalTime);
            }
        }

        /// <summary>
        /// 查找同名 buff 的倒计时数据, 没有则返回 null。
        /// </summary>
        private BuffStateData FindState(string buffName)
        {
            foreach (var data in stateList)
            {
                if (data.buffName == buffName)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
