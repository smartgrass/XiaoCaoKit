using UnityEngine;
using UnityEngine.EventSystems;

namespace XiaoCao
{
    /// <summary>
    /// UI导航处理接口，实现标准的UI导航操作
    /// 包括Move, Select, UnSelect, Submit等操作
    /// </summary>
    public class UINavigationHandler : MonoBehaviour, IMoveHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // 事件定义
        public System.Action<AxisEventData> OnMoveAction;
        public System.Action<BaseEventData> OnSelectAction;
        public System.Action<BaseEventData> OnDeselectAction;
        public System.Action<BaseEventData> OnSubmitAction;

        /// <summary>
        /// 当用户通过方向键或摇杆导航时触发
        /// </summary>
        /// <param name="eventData">轴事件数据</param>
        public void OnMove(AxisEventData eventData)
        {
            OnMoveAction?.Invoke(eventData);
            Debug.Log($"[{gameObject.name}] Move: {eventData.moveVector}");
        }

        /// <summary>
        /// 当对象被选中时触发（获得焦点）
        /// </summary>
        /// <param name="eventData">基础事件数据</param>
        public void OnSelect(BaseEventData eventData)
        {
            OnSelectAction?.Invoke(eventData);
            Debug.Log($"[{gameObject.name}] Selected");
        }

        /// <summary>
        /// 当对象失去选中状态时触发（失去焦点）
        /// </summary>
        /// <param name="eventData">基础事件数据</param>
        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectAction?.Invoke(eventData);
            Debug.Log($"[{gameObject.name}] Deselected");
        }

        /// <summary>
        /// 当用户提交当前选中对象时触发（如按下回车键）
        /// </summary>
        /// <param name="eventData">基础事件数据</param>
        public void OnSubmit(BaseEventData eventData)
        {
            OnSubmitAction?.Invoke(eventData);
            Debug.Log($"[{gameObject.name}] Submitted");
        }
    }
}